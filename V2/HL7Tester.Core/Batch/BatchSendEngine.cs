using System.Diagnostics;
using System.Net.Sockets;
using HL7Tester.Core.Batch.Models;
using HL7Tester.Core.Fhir;
using HL7Tester.Core.Sending;
using Microsoft.Extensions.Logging;

namespace HL7Tester.Core.Batch;

/// <summary>
/// Orchestrates batch sending of HL7 v2 (MLLP) or FHIR (REST) messages.
/// All patients run in parallel; each patient follows its workflow steps sequentially.
/// A global throttle ensures a minimum delay between ALL messages (across all patients).
/// </summary>
public sealed class BatchSendEngine
{
    private readonly AdtMessageGenerator _generator;
    private readonly IMessageSender _sender;
    private readonly IFhirResourceGenerator _fhirGenerator;
    private readonly PatientDataRandomizer _randomizer;
    private readonly ILogger<BatchSendEngine> _logger;
    private readonly Random _random = new();

    public BatchSendEngine(
        AdtMessageGenerator generator,
        IMessageSender sender,
        IFhirResourceGenerator fhirGenerator,
        PatientDataRandomizer randomizer,
        ILogger<BatchSendEngine>? logger = null)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _fhirGenerator = fhirGenerator ?? throw new ArgumentNullException(nameof(fhirGenerator));
        _randomizer = randomizer ?? throw new ArgumentNullException(nameof(randomizer));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<BatchSendEngine>.Instance;
    }

    /// <summary>
    /// Runs a batch workflow: generates N patients, sends their workflow steps in parallel,
    /// with a global delay between all messages.
    /// </summary>
    /// <param name="workflow">The workflow configuration (steps, patient count, delay, location).</param>
    /// <param name="ipAddress">Target server IP address (MLLP mode).</param>
    /// <param name="port">Target server port (MLLP mode).</param>
    /// <param name="encodingName">Message encoding (null for UTF-8 default, MLLP mode).</param>
    /// <param name="cancellationToken">Token to cancel the batch run.</param>
    /// <param name="protocol">Transport protocol: "MLLP" (default) or "FHIR".</param>
    /// <param name="fhirProfile">Active FHIR connection profile (required when protocol is FHIR).</param>
    /// <returns>The final batch result with per-patient outcomes.</returns>
    public async Task<BatchSendResult> RunAsync(
        BatchWorkflow workflow,
        string ipAddress,
        int port,
        string? encodingName = null,
        CancellationToken cancellationToken = default,
        string protocol = "MLLP",
        FhirProfile? fhirProfile = null)
    {
        if (workflow is null) throw new ArgumentNullException(nameof(workflow));

        var isFhir = string.Equals(protocol, "FHIR", StringComparison.OrdinalIgnoreCase);

        if (!isFhir && string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address is required.", nameof(ipAddress));

        if (isFhir && fhirProfile is null)
            throw new InvalidOperationException("FHIR protocol requires an active FHIR profile.");

        if (!workflow.IsValid(out var validationError))
            throw new InvalidOperationException(validationError);

        var stopwatch = Stopwatch.StartNew();
        var result = new BatchSendResult();

        // Generate all patient contexts
        var patients = _randomizer.GeneratePatients(workflow.PatientCount);

        // Keep-alive TCP only makes sense for MLLP; ignored in FHIR mode.
        var keepConnection = !isFhir && workflow.KeepConnectionOpen;

        _logger.LogInformation("Batch started: {PatientCount} patients, {StepCount} steps, delay {DelayMs}ms, protocol {Protocol}{KeepConn}",
            workflow.PatientCount, workflow.Steps.Count, workflow.GlobalDelayMs,
            isFhir ? "FHIR" : "MLLP",
            keepConnection ? " [KeepConnection]" : string.Empty);

        // Global throttle: ensures at least GlobalDelayMs between any two sends
        var throttle = new SemaphoreSlim(1, 1);

        // If KeepConnectionOpen (MLLP only), create a single shared TCP connection
        TcpClient? sharedClient = null;
        if (keepConnection)
        {
            sharedClient = new TcpClient();
            await sharedClient.ConnectAsync(ipAddress, port, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Shared TCP connection established to {Ip}:{Port}", ipAddress, port);
        }

        try
        {
            // Launch all patients in parallel
            var tasks = patients.Select(patient => RunPatientAsync(
                patient, workflow, ipAddress, port, encodingName, throttle, sharedClient,
                isFhir, fhirProfile, cancellationToken)).ToList();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            // Collect results
            foreach (var task in tasks)
            {
                result.PatientResults.Add(task.Result);
            }

            // Check if cancellation was requested
            result.WasCancelled = cancellationToken.IsCancellationRequested;

            stopwatch.Stop();
            result.Elapsed = stopwatch.Elapsed;

            _logger.LogInformation("Batch completed: {Success}/{Total} patients OK, {Messages}/{Attempted} messages sent, elapsed {Elapsed}",
                result.SuccessfulPatients, result.TotalPatients,
                result.TotalMessagesSent, result.TotalMessagesAttempted,
                result.Elapsed.ToString(@"mm\:ss\.ff"));
        }
        finally
        {
            sharedClient?.Dispose();
        }

        return result;
    }

    private async Task<PatientBatchResult> RunPatientAsync(
        PatientContext patient,
        BatchWorkflow workflow,
        string ipAddress,
        int port,
        string? encodingName,
        SemaphoreSlim throttle,
        TcpClient? sharedClient,
        bool isFhir,
        FhirProfile? fhirProfile,
        CancellationToken cancellationToken)
    {
        var patientResult = new PatientBatchResult
        {
            Patient = patient,
            TotalSteps = workflow.Steps.Count,
            Protocol = isFhir ? "FHIR" : "MLLP"
        };

        // Resolve global location fields once per patient (supports CSV: "301A, 302B, 308A")
        var patientRoom = PickRandomValue(workflow.Room);
        var patientBed = PickRandomValue(workflow.Bed);
        var patientUnit = PickRandomValue(workflow.Unit);
        var patientFloor = PickRandomValue(workflow.Floor);

        // Resolved event dates per step (for "@N" reference resolution)
        var resolvedEventDates = new string[workflow.Steps.Count];

        try
        {
            for (int i = 0; i < workflow.Steps.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var step = workflow.Steps[i];

                // Resolve EventDateTime: explicit value, "@N" reference, or DateTime.Now
                resolvedEventDates[i] = ResolveEventDateTime(step.EventDateTime, resolvedEventDates, i);

                // Build the message request with patient data + location (step override or resolved global)
                var request = new AdtMessageRequest
                {
                    MessageTypeCode = step.MessageType,
                    PatientId = patient.PatientId,
                    PatientFamilyName = step.FamilyName ?? patient.FamilyName,
                    PatientGivenName = step.GivenName ?? patient.GivenName,
                    BirthDate = patient.BirthDate,
                    Sex = string.IsNullOrEmpty(workflow.Sex) ? patient.Sex : workflow.Sex,
                    AdmissionNumber = patient.AdmissionNumber,
                    Room = step.Room ?? patientRoom,
                    Bed = step.Bed ?? patientBed,
                    Unit = step.Unit ?? patientUnit,
                    Floor = step.Floor ?? patientFloor,
                    NewPatientId = step.NewPatientId,
                    EventDateTime = resolvedEventDates[i]
                };

                // Acquire the global throttle (ensures delay between ALL messages)
                await throttle.WaitAsync(cancellationToken).ConfigureAwait(false);
                MessageSendResult sendResult;
                try
                {
                    if (isFhir)
                    {
                        sendResult = await SendStepFhirAsync(request, fhirProfile, cancellationToken).ConfigureAwait(false);
                        patientResult.LastHttpStatusCode = sendResult.HttpStatusCode;
                    }
                    else
                    {
                        sendResult = await SendStepMllpAsync(request, ipAddress, port, encodingName, sharedClient, cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    throttle.Release();
                }

                if (!sendResult.Success)
                {
                    patientResult.FailedStepIndex = i + 1;
                    patientResult.Error = sendResult.ErrorMessage;
                    _logger.LogWarning("Patient {PatientId} step {Step} ({MsgType}) failed: {Error}",
                        patient.PatientId, i + 1, step.MessageType, sendResult.ErrorMessage);
                    break;
                }

                patientResult.SuccessfulSteps++;
                _logger.LogDebug("Patient {PatientId} step {Step}/{Total} ({MsgType}) sent OK",
                    patient.PatientId, i + 1, workflow.Steps.Count, step.MessageType);

                // Global delay between messages (except after the last step)
                if (i < workflow.Steps.Count - 1 && workflow.GlobalDelayMs > 0)
                {
                    await Task.Delay(workflow.GlobalDelayMs, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            patientResult.FailedStepIndex = patientResult.SuccessfulSteps + 1;
            patientResult.Error = "Cancelled";
            _logger.LogInformation("Patient {PatientId} cancelled at step {Step}",
                patient.PatientId, patientResult.SuccessfulSteps + 1);
        }
        catch (Exception ex)
        {
            patientResult.FailedStepIndex = patientResult.SuccessfulSteps + 1;
            patientResult.Error = ex.Message;
            _logger.LogError(ex, "Patient {PatientId} step {Step} exception",
                patient.PatientId, patientResult.SuccessfulSteps + 1);
        }

        return patientResult;
    }

    /// <summary>
    /// MLLP transport: generate the HL7 v2 message and send it over TCP/MLLP.
    /// The shared TCP client (keep-alive) is forwarded through the protocol-agnostic
    /// <see cref="SendRequest"/> so behavior is identical to the pre-FHIR implementation.
    /// </summary>
    private async Task<MessageSendResult> SendStepMllpAsync(
        AdtMessageRequest request,
        string ipAddress,
        int port,
        string? encodingName,
        TcpClient? sharedClient,
        CancellationToken cancellationToken)
    {
        var message = _generator.Generate(request);

        return await _sender.SendAsync(new SendRequest
        {
            Protocol = "MLLP",
            Payload = message,
            Ip = ipAddress,
            Port = port,
            EncodingName = encodingName,
            ExistingClient = sharedClient
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// FHIR transport: generate the resource from the same HL7 form model, serialize
    /// to JSON, and send it over HTTP with the verb/endpoint mapped from the step type.
    /// </summary>
    private async Task<MessageSendResult> SendStepFhirAsync(
        AdtMessageRequest request,
        FhirProfile? fhirProfile,
        CancellationToken cancellationToken)
    {
        var resource = _fhirGenerator.Generate(request);
        var json = _fhirGenerator.ToJson(resource);

        var (resourceType, httpMethod, resourceId) = MapStepToFhir(request.MessageTypeCode, request);

        return await _sender.SendAsync(new SendRequest
        {
            Protocol = "FHIR",
            Payload = json,
            Profile = fhirProfile,
            ResourceType = resourceType,
            ResourceId = resourceId,
            HttpMethod = httpMethod
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps an HL7 step message type to the FHIR REST interaction:
    /// endpoint resource type, HTTP verb, and optional resource id.
    /// Mirrors the resource kinds produced by <see cref="FhirR4Generator"/>.
    /// </summary>
    /// <remarks>
    /// | Step type(s)            | FHIR interaction                          |
    /// |-------------------------|-------------------------------------------|
    /// | A01, A04, A05, A28      | POST Bundle (transaction)                 |
    /// | A08, A31                | PUT Patient/{id}                          |
    /// | A03                     | PATCH Encounter (discharge)               |
    /// | A02, A09, A10, A21, A22| PATCH Encounter (location update)         |
    /// | A18, A40                | POST Patient/{id}/$merge                  |
    /// | O01                     | POST ServiceRequest                       |
    /// | S12                     | POST Appointment                          |
    /// | S13, S14                | PUT Appointment                           |
    /// | S15                     | PATCH Appointment                         |
    /// </remarks>
    private static (string ResourceType, FhirHttpMethod Method, string? ResourceId) MapStepToFhir(
        string messageType, AdtMessageRequest request)
    {
        var code = (messageType ?? string.Empty).ToUpperInvariant();

        // Admission-like events: atomic transaction Bundle (Patient + Encounter POST).
        if (ContainsAny(code, "ADT A01", "ADT A04", "ADT A05", "ADT A28"))
            return ("Bundle", FhirHttpMethod.Post, null);

        // Patient identity updates: full Patient for PUT.
        if (ContainsAny(code, "ADT A08", "ADT A31"))
            return ("Patient", FhirHttpMethod.Put, NullIfEmpty(request.PatientId));

        // Discharge: partial Encounter for PATCH with status finished.
        if (code.Contains("ADT A03", StringComparison.Ordinal))
            return ("Encounter", FhirHttpMethod.Patch, null);

        // Transfer/move-like events: Encounter status + location update.
        if (ContainsAny(code, "ADT A02", "ADT A09", "ADT A10", "ADT A21", "ADT A22"))
            return ("Encounter", FhirHttpMethod.Patch, null);

        // Patient merge: $merge operation on the source Patient.
        if (ContainsAny(code, "ADT A18", "ADT A40"))
        {
            var sourceId = NullIfEmpty(request.PatientId);
            var mergeId = sourceId is null ? null : $"{sourceId}/$merge";
            return ("Patient", FhirHttpMethod.Post, mergeId);
        }

        // Orders: ServiceRequest.
        if (code.Contains("ORM O01", StringComparison.Ordinal))
            return ("ServiceRequest", FhirHttpMethod.Post, null);

        // Appointments: create / reschedule / modify / cancel.
        if (code.Contains("SIU S12", StringComparison.Ordinal))
            return ("Appointment", FhirHttpMethod.Post, null);

        if (ContainsAny(code, "SIU S13", "SIU S14"))
            return ("Appointment", FhirHttpMethod.Put, null);

        if (code.Contains("SIU S15", StringComparison.Ordinal))
            return ("Appointment", FhirHttpMethod.Patch, null);

        // Unsupported types surface as NotSupportedException from the generator
        // before this mapping is reached; keep a safe fallback for robustness.
        return ("Bundle", FhirHttpMethod.Post, null);
    }

    private static bool ContainsAny(string haystack, params string[] needles)
    {
        foreach (var needle in needles)
        {
            if (haystack.Contains(needle, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    /// <summary>
    /// Picks a random value from a comma-separated list.
    /// If the input contains no comma, it is returned as-is (trimmed).
    /// </summary>
    private string PickRandomValue(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return string.Empty;

        var values = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (values.Length <= 1) return values[0];

        return values[_random.Next(values.Length)];
    }

    /// <summary>
    /// Resolves the EventDateTime for a step.
    /// - null/empty → DateTime.Now (yyyyMMddHHmm)
    /// - "@N" → references the resolved date of step N (1-based)
    /// - explicit value → used as-is
    /// </summary>
    private static string ResolveEventDateTime(string? raw, string[] resolvedDates, int currentIndex)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return DateTime.Now.ToString("yyyyMMddHHmm");

        var trimmed = raw.Trim();

        // Reference to another step: "@2" means use the resolved date of step 2
        if (trimmed.StartsWith('@'))
        {
            var refStr = trimmed[1..];
            if (int.TryParse(refStr, out var refIndex) && refIndex >= 1 && refIndex <= currentIndex)
            {
                return resolvedDates[refIndex - 1];
            }
            // Invalid reference → fall back to now
            return DateTime.Now.ToString("yyyyMMddHHmm");
        }

        // Explicit date value — use as-is
        return trimmed;
    }
}
