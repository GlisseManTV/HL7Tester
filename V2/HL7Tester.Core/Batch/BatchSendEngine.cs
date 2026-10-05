using System.Diagnostics;
using System.Net.Sockets;
using HL7Tester.Core.Batch.Models;
using Microsoft.Extensions.Logging;

namespace HL7Tester.Core.Batch;

/// <summary>
/// Orchestrates batch sending of HL7 messages.
/// All patients run in parallel; each patient follows its workflow steps sequentially.
/// A global throttle ensures a minimum delay between ALL messages (across all patients).
/// </summary>
public sealed class BatchSendEngine
{
    private readonly AdtMessageGenerator _generator;
    private readonly IHL7NetworkSender _sender;
    private readonly PatientDataRandomizer _randomizer;
    private readonly ILogger<BatchSendEngine> _logger;
    private readonly Random _random = new();

    public BatchSendEngine(
        AdtMessageGenerator generator,
        IHL7NetworkSender sender,
        PatientDataRandomizer randomizer,
        ILogger<BatchSendEngine>? logger = null)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _randomizer = randomizer ?? throw new ArgumentNullException(nameof(randomizer));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<BatchSendEngine>.Instance;
    }

    /// <summary>
    /// Runs a batch workflow: generates N patients, sends their workflow steps in parallel,
    /// with a global delay between all messages.
    /// </summary>
    /// <param name="workflow">The workflow configuration (steps, patient count, delay, location).</param>
    /// <param name="ipAddress">Target server IP address.</param>
    /// <param name="port">Target server port.</param>
    /// <param name="encodingName">Message encoding (null for UTF-8 default).</param>
    /// <param name="cancellationToken">Token to cancel the batch run.</param>
    /// <returns>The final batch result with per-patient outcomes.</returns>
    public async Task<BatchSendResult> RunAsync(
        BatchWorkflow workflow,
        string ipAddress,
        int port,
        string? encodingName = null,
        CancellationToken cancellationToken = default)
    {
        if (workflow is null) throw new ArgumentNullException(nameof(workflow));
        if (string.IsNullOrWhiteSpace(ipAddress)) throw new ArgumentException("IP address is required.", nameof(ipAddress));

        if (!workflow.IsValid(out var validationError))
            throw new InvalidOperationException(validationError);

        var stopwatch = Stopwatch.StartNew();
        var result = new BatchSendResult();

        // Generate all patient contexts
        var patients = _randomizer.GeneratePatients(workflow.PatientCount);
        _logger.LogInformation("Batch started: {PatientCount} patients, {StepCount} steps, delay {DelayMs}ms → {Ip}:{Port}{KeepConn}",
            workflow.PatientCount, workflow.Steps.Count, workflow.GlobalDelayMs, ipAddress, port,
            workflow.KeepConnectionOpen ? " [KeepConnection]" : string.Empty);

        // Global throttle: ensures at least GlobalDelayMs between any two sends
        var throttle = new SemaphoreSlim(1, 1);

        // If KeepConnectionOpen, create a single shared TCP connection
        TcpClient? sharedClient = null;
        if (workflow.KeepConnectionOpen)
        {
            sharedClient = new TcpClient();
            await sharedClient.ConnectAsync(ipAddress, port, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Shared TCP connection established to {Ip}:{Port}", ipAddress, port);
        }

        try
        {
            // Launch all patients in parallel
            var tasks = patients.Select(patient => RunPatientAsync(
                patient, workflow, ipAddress, port, encodingName, throttle, sharedClient, cancellationToken)).ToList();

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
        CancellationToken cancellationToken)
    {
        var patientResult = new PatientBatchResult
        {
            Patient = patient,
            TotalSteps = workflow.Steps.Count
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

                // Generate the HL7 message
                var message = _generator.Generate(request);

                // Acquire the global throttle (ensures delay between ALL messages)
                await throttle.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    // Send the message (pass shared client if KeepConnectionOpen)
                    var sendResult = await _sender.SendAsync(
                        message, ipAddress, port, cancellationToken, encodingName, sharedClient).ConfigureAwait(false);

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
                }
                finally
                {
                    throttle.Release();
                }

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
