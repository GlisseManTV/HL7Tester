using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Model.V23.Message;
using NHapi.Model.V23.Segment;

namespace HL7Tester.Core;

/// <summary>
/// Request model used by the main generator.
/// It keeps backward compatibility with existing ADT fields and also contains ORM/SIU fields.
/// </summary>
public sealed class AdtMessageRequest
{
    public string MessageTypeCode { get; set; } = "ADT A01";

    // Common patient/location fields
    public string PatientId { get; set; } = string.Empty;
    public string PatientFamilyName { get; set; } = string.Empty;
    public string PatientGivenName { get; set; } = string.Empty;
    public string BirthDate { get; set; } = string.Empty;
    public string Sex { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string Bed { get; set; } = string.Empty;
    public string Facility { get; set; } = string.Empty;
    public string Floor { get; set; } = string.Empty;
    public string AdmissionNumber { get; set; } = string.Empty;

    // ADT-specific
    public string? NewPatientId { get; set; }
    public string? EventDateTime { get; set; }

    // MSH routing fields
    public string SendingApplication { get; set; } = "Hl7Tester-Core";
    public string SendingFacility { get; set; } = "HL7Tester";
    public string ReceivingApplication { get; set; } = "Receiver";
    public string ReceivingFacility { get; set; } = "ReceiverFacility";

    public IList<ObxEntry> ObxEntries { get; } = new List<ObxEntry>();

    // ORM-specific
    public string OrderControl { get; set; } = "NW";
    public string OrmPlacerOrderNumber { get; set; } = string.Empty;
    public string OrmFillerOrderNumber { get; set; } = string.Empty;
    public string OrmOrderStatus { get; set; } = "Final";
    public string OrmUniversalServiceId { get; set; } = string.Empty;
    public string? OrmRequestedDateTime { get; set; }
    public string OrmOrderingProviderId { get; set; } = string.Empty;
    public string OrmOrderingProviderFamilyName { get; set; } = string.Empty;
    public string OrmOrderingProviderGivenName { get; set; } = string.Empty;
    public string OrmDiagnosisCode { get; set; } = string.Empty;
    public string OrmDiagnosisText { get; set; } = string.Empty;

    // SIU-specific
    public string SiuPlacerAppointmentId { get; set; } = string.Empty;
    public string SiuFillerAppointmentId { get; set; } = string.Empty;
    public string SiuOccurrenceNumber { get; set; } = "1";
    public string SiuAppointmentType { get; set; } = "OFFICE^Office visit";
    public string SiuAppointmentReason { get; set; } = string.Empty;
    public string? SiuAppointmentStartDateTime { get; set; }
    public string? SiuAppointmentEndDateTime { get; set; }
    public string SiuAppointmentDuration { get; set; } = "60";
    public string SiuAppointmentDurationUnits { get; set; } = "m";
    public string SiuAppointmentStatus { get; set; } = "Scheduled";

    public string SiuGeneralResourceId { get; set; } = string.Empty;
    public string SiuGeneralResourceName { get; set; } = string.Empty;
    public string SiuGeneralResourceType { get; set; } = "D";

    public string SiuLocationCode { get; set; } = string.Empty;
    public string SiuLocationDescription { get; set; } = string.Empty;
    public string SiuLocationSite { get; set; } = string.Empty;

    public string SiuPersonnelId { get; set; } = string.Empty;
    public string SiuPersonnelFamilyName { get; set; } = string.Empty;
    public string SiuPersonnelGivenName { get; set; } = string.Empty;
    public string SiuPersonnelRole { get; set; } = "D";
}

/// <summary>
/// Free OBX entry.
/// </summary>
public sealed class ObxEntry
{
    public string? Type { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Main HL7 v2.3 message generator (ADT + ORM + SIU).
/// </summary>
public sealed class AdtMessageGenerator
{
    private readonly ILogger<AdtMessageGenerator> _logger;

    static AdtMessageGenerator()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public AdtMessageGenerator(ILogger<AdtMessageGenerator>? logger = null)
    {
        _logger = logger ?? NullLogger<AdtMessageGenerator>.Instance;
    }

    public string Generate(AdtMessageRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var (messageCode, triggerEvent) = ParseMessageTypeCode(request.MessageTypeCode);
        _logger.LogInformation("Generating HL7 message for type {MessageType}", request.MessageTypeCode);

        return messageCode.ToUpperInvariant() switch
        {
            "ADT" => GenerateAdtMessage(request, triggerEvent),
            "ORM" => GenerateOrmMessage(request, messageCode, triggerEvent),
            "SIU" => GenerateSiuMessage(request, messageCode, triggerEvent),
            _ => throw new NotSupportedException($"Unsupported HL7 message family: '{messageCode}'.")
        };
    }

    private string GenerateAdtMessage(AdtMessageRequest request, string triggerEvent)
    {
        var message = CreateAdtMessage(triggerEvent);

        var msh = (MSH)message.GetStructure("MSH");
        msh.FieldSeparator.Value = "|";
        msh.EncodingCharacters.Value = "^~\\&";
        msh.SendingApplication.NamespaceID.Value = request.SendingApplication;
        msh.SendingFacility.NamespaceID.Value = request.SendingFacility;
        msh.ReceivingApplication.NamespaceID.Value = request.ReceivingApplication;
        msh.ReceivingFacility.NamespaceID.Value = request.ReceivingFacility;
        msh.MessageType.MessageType.Value = "ADT";
        msh.MessageType.TriggerEvent.Value = triggerEvent;
        msh.ProcessingID.ProcessingID.Value = "P";
        msh.VersionID.Value = "2.3";
        msh.DateTimeOfMessage.TimeOfAnEvent.Value = DateTime.Now.ToString("yyyyMMddHHmmss");
        msh.CharacterSet.Value = "ASCII";

        if (message is ADT_A40 adtA40)
        {
            var patient = adtA40.GetPATIENT();
            var pidA40 = patient.PID;
            pidA40.SetIDPatientID.Value = "1";
            pidA40.GetPatientIDInternalID(0).ID.Value = request.NewPatientId ?? request.PatientId;
            pidA40.GetPatientName(0).FamilyName.Value = request.PatientFamilyName;
            pidA40.GetPatientName(0).GivenName.Value = request.PatientGivenName;
            pidA40.DateOfBirth.TimeOfAnEvent.Value = request.BirthDate;
            pidA40.Sex.Value = request.Sex;

            var mrg = patient.MRG;
            mrg.GetPriorPatientIDInternal(0).ID.Value = request.PatientId;
        }
        else if (message is ADT_A18)
        {
            var pidA18 = (PID)message.GetStructure("PID");
            pidA18.SetIDPatientID.Value = "1";
            pidA18.GetPatientIDInternalID(0).ID.Value = request.NewPatientId ?? request.PatientId;
            pidA18.GetPatientName(0).FamilyName.Value = request.PatientFamilyName;
            pidA18.GetPatientName(0).GivenName.Value = request.PatientGivenName;
            pidA18.DateOfBirth.TimeOfAnEvent.Value = request.BirthDate;
            pidA18.Sex.Value = request.Sex;

            var mrg = (MRG)message.GetStructure("MRG");
            mrg.GetPriorPatientIDInternal(0).ID.Value = request.PatientId;
        }
        else
        {
            var pid = (PID)message.GetStructure("PID");
            pid.SetIDPatientID.Value = "1";
            pid.GetPatientIDInternalID(0).ID.Value = request.PatientId;
            pid.GetPatientName(0).FamilyName.Value = request.PatientFamilyName;
            pid.GetPatientName(0).GivenName.Value = request.PatientGivenName;
            pid.DateOfBirth.TimeOfAnEvent.Value = request.BirthDate;
            pid.Sex.Value = request.Sex;

            var pv1 = (PV1)message.GetStructure("PV1");
            pv1.SetIDPatientVisit.Value = "1";
            pv1.PatientClass.Value = "I";
            pv1.AssignedPatientLocation.PointOfCare.Value = request.Unit;
            pv1.AssignedPatientLocation.Room.Value = request.Room;
            pv1.AssignedPatientLocation.Bed.Value = request.Bed;
            pv1.AssignedPatientLocation.Facility.NamespaceID.Value = request.Facility;
            pv1.AssignedPatientLocation.Floor.Value = request.Floor;
            pv1.VisitNumber.ID.Value = request.AdmissionNumber;

            FillAdtObxSegments(message, request.ObxEntries);
        }

        var evn = (EVN)message.GetStructure("EVN");
        evn.EventTypeCode.Value = triggerEvent;
        evn.RecordedDateTime.TimeOfAnEvent.Value = DateTime.Now.ToString("yyyyMMddHHmm");
        evn.EventOccured.TimeOfAnEvent.Value =
            string.IsNullOrWhiteSpace(request.EventDateTime)
                ? DateTime.Now.ToString("yyyyMMddHHmm")
                : request.EventDateTime;

        var parser = new PipeParser();
        string encoded = parser.Encode(message);
        string controlId = GenerateControlIdFromMessage(encoded);
        msh.MessageControlID.Value = controlId;

        var finalMessage = parser.Encode(message);
        _logger.LogInformation("ADT message generated successfully (ControlID={ControlId})", controlId);
        return finalMessage;
    }

    private string GenerateOrmMessage(AdtMessageRequest request, string messageCode, string triggerEvent)
    {
        if (!string.Equals(triggerEvent, "O01", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Unsupported ORM trigger event: '{triggerEvent}'.");
        }

        var now = DateTime.Now;
        string timestamp = now.ToString("yyyyMMddHHmmss");
        string requestedDateTime = string.IsNullOrWhiteSpace(request.OrmRequestedDateTime)
            ? timestamp
            : request.OrmRequestedDateTime;

        var tempSegments = BuildOrmSegments(request, messageCode, triggerEvent, timestamp, string.Empty, requestedDateTime);
        string controlId = GenerateControlIdFromMessage(string.Join("\r", tempSegments));
        var segments = BuildOrmSegments(request, messageCode, triggerEvent, timestamp, controlId, requestedDateTime);

        var finalMessage = string.Join("\r", segments);
        _logger.LogInformation("ORM message generated successfully (ControlID={ControlId})", controlId);
        return finalMessage;
    }

    private string GenerateSiuMessage(AdtMessageRequest request, string messageCode, string triggerEvent)
    {
        if (triggerEvent is not ("S12" or "S13" or "S14" or "S15"))
        {
            throw new NotSupportedException($"Unsupported SIU trigger event: '{triggerEvent}'.");
        }

        var now = DateTime.Now;
        string timestamp = now.ToString("yyyyMMddHHmmss");
        string startDateTime = string.IsNullOrWhiteSpace(request.SiuAppointmentStartDateTime)
            ? now.AddMinutes(30).ToString("yyyyMMddHHmmss")
            : request.SiuAppointmentStartDateTime;

        string endDateTime = string.IsNullOrWhiteSpace(request.SiuAppointmentEndDateTime)
            ? now.AddMinutes(90).ToString("yyyyMMddHHmmss")
            : request.SiuAppointmentEndDateTime;

        var tempSegments = BuildSiuSegments(request, messageCode, triggerEvent, timestamp, string.Empty, startDateTime, endDateTime);
        string controlId = GenerateControlIdFromMessage(string.Join("\r", tempSegments));
        var segments = BuildSiuSegments(request, messageCode, triggerEvent, timestamp, controlId, startDateTime, endDateTime);

        var finalMessage = string.Join("\r", segments);
        _logger.LogInformation("SIU message generated successfully (ControlID={ControlId})", controlId);
        return finalMessage;
    }

    private List<string> BuildOrmSegments(
        AdtMessageRequest request,
        string messageCode,
        string triggerEvent,
        string timestamp,
        string controlId,
        string requestedDateTime)
    {
        var segments = new List<string>
        {
            BuildMshSegment(request, messageCode, triggerEvent, timestamp, controlId),
            BuildPidSegment(request),
            BuildPv1Segment(request, patientClass: "I", includeVisitNumber: true),
            BuildSegment(
                "ORC",
                request.OrderControl,
                request.OrmPlacerOrderNumber,
                request.OrmFillerOrderNumber,
                string.Empty,
                request.OrmOrderStatus,
                string.Empty,
                $"^^^{requestedDateTime}^^^^",
                string.Empty,
                timestamp,
                string.Empty,
                BuildProviderField(request.OrmOrderingProviderId, request.OrmOrderingProviderFamilyName, request.OrmOrderingProviderGivenName)),
            BuildSegment(
                "OBR",
                "1",
                request.OrmPlacerOrderNumber,
                request.OrmFillerOrderNumber,
                request.OrmUniversalServiceId,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                BuildProviderField(request.OrmOrderingProviderId, request.OrmOrderingProviderFamilyName, request.OrmOrderingProviderGivenName),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                request.OrmOrderStatus)
        };

        if (!string.IsNullOrWhiteSpace(request.OrmDiagnosisCode) || !string.IsNullOrWhiteSpace(request.OrmDiagnosisText))
        {
            segments.Add(
                BuildSegment(
                    "DG1",
                    "1",
                    string.Empty,
                    $"{request.OrmDiagnosisCode}^{request.OrmDiagnosisText}"));
        }

        segments.AddRange(BuildObxSegments(request.ObxEntries));
        return segments;
    }

    private List<string> BuildSiuSegments(
        AdtMessageRequest request,
        string messageCode,
        string triggerEvent,
        string timestamp,
        string controlId,
        string startDateTime,
        string endDateTime)
    {
        var providerField = BuildProviderField(request.SiuPersonnelId, request.SiuPersonnelFamilyName, request.SiuPersonnelGivenName);

        var segments = new List<string>
        {
            BuildMshSegment(request, messageCode, triggerEvent, timestamp, controlId),
            BuildSegment(
                "SCH",
                request.SiuPlacerAppointmentId,
                request.SiuFillerAppointmentId,
                string.Empty,
                string.Empty,
                request.SiuOccurrenceNumber,
                request.SiuAppointmentType,
                request.SiuAppointmentReason,
                "OFFICE",
                request.SiuAppointmentDuration,
                request.SiuAppointmentDurationUnits,
                $"^^{request.SiuAppointmentDuration}^{startDateTime}^{endDateTime}",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                providerField,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                request.SiuAppointmentStatus),
            BuildPidSegment(request),
            BuildPv1Segment(request, patientClass: "O", includeVisitNumber: false)
        };

        segments.AddRange(BuildObxSegments(request.ObxEntries));

        segments.Add(BuildSegment("RGS", "1", "A"));
        segments.Add(
            BuildSegment(
                "AIG",
                "1",
                "A",
                $"{request.SiuGeneralResourceId}^{request.SiuGeneralResourceName}",
                $"{request.SiuGeneralResourceType}^^",
                string.Empty,
                startDateTime,
                string.Empty,
                string.Empty,
                request.SiuAppointmentDuration,
                $"{request.SiuAppointmentDurationUnits}^Minutes",
                string.Empty,
                request.SiuAppointmentStatus));

        segments.Add(
            BuildSegment(
                "AIL",
                "1",
                "A",
                $"{request.SiuLocationCode}^{request.SiuLocationDescription}^{request.SiuLocationSite}",
                "^Main Office",
                string.Empty,
                startDateTime,
                string.Empty,
                string.Empty,
                request.SiuAppointmentDuration,
                $"{request.SiuAppointmentDurationUnits}^Minutes",
                string.Empty,
                request.SiuAppointmentStatus));

        segments.Add(
            BuildSegment(
                "AIP",
                "1",
                "A",
                providerField,
                $"{request.SiuPersonnelRole}^^",
                string.Empty,
                startDateTime,
                string.Empty,
                string.Empty,
                request.SiuAppointmentDuration,
                $"{request.SiuAppointmentDurationUnits}^Minutes",
                string.Empty,
                request.SiuAppointmentStatus));

        return segments;
    }

    private static string BuildMshSegment(
        AdtMessageRequest request,
        string messageCode,
        string triggerEvent,
        string timestamp,
        string controlId)
    {
        return BuildSegment(
            "MSH",
            "^~\\&",
            request.SendingApplication,
            request.SendingFacility,
            request.ReceivingApplication,
            request.ReceivingFacility,
            timestamp,
            string.Empty,
            $"{messageCode}^{triggerEvent}",
            controlId,
            "P",
            "2.3",
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            "ASCII");
    }

    private static string BuildPidSegment(AdtMessageRequest request)
    {
        return BuildSegment(
            "PID",
            "1",
            string.Empty,
            request.PatientId,
            string.Empty,
            $"{request.PatientFamilyName}^{request.PatientGivenName}",
            string.Empty,
            request.BirthDate,
            request.Sex);
    }

    private static string BuildPv1Segment(AdtMessageRequest request, string patientClass, bool includeVisitNumber)
    {
        string location = $"{request.Unit}^{request.Room}^{request.Bed}^{request.Facility}^^^^^{request.Floor}";

        if (!includeVisitNumber)
        {
            return BuildSegment("PV1", "1", patientClass, location);
        }

        return BuildSegment(
            "PV1",
            "1",
            patientClass,
            location,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            request.AdmissionNumber);
    }

    private static string BuildProviderField(string id, string familyName, string givenName)
        => $"{id}^{familyName}^{givenName}";

    private static IEnumerable<string> BuildObxSegments(IEnumerable<ObxEntry> entries)
    {
        if (entries is null)
        {
            yield break;
        }

        int index = 0;
        foreach (var entry in entries)
        {
            string type = string.IsNullOrWhiteSpace(entry.Type) ? string.Empty : entry.Type;
            string reason = string.IsNullOrWhiteSpace(entry.Reason) ? string.Empty : entry.Reason;

            if (string.IsNullOrWhiteSpace(type) && string.IsNullOrWhiteSpace(reason))
            {
                continue;
            }

            index++;
            yield return BuildSegment(
                "OBX",
                index.ToString(),
                "TX",
                string.IsNullOrWhiteSpace(type) ? "UNK" : type,
                string.Empty,
                string.IsNullOrWhiteSpace(reason) ? "Not specified" : reason,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                "F");
        }
    }

    private static string BuildSegment(params string?[] fields)
        => string.Join("|", fields.Select(static field => field ?? string.Empty));

    private static void FillAdtObxSegments(IMessage message, IEnumerable<ObxEntry> entries)
    {
        if (entries is null)
        {
            return;
        }

        int obxIndex = 0;

        foreach (var entry in entries)
        {
            var type = entry.Type;
            var reason = entry.Reason;

            if (string.IsNullOrWhiteSpace(type) && string.IsNullOrWhiteSpace(reason))
            {
                continue;
            }

            var obx = (OBX)message.GetStructure("OBX", obxIndex++);
            obx.SetIDOBX.Value = obxIndex.ToString();
            obx.ValueType.Value = "TX";
            obx.ObservationIdentifier.Identifier.Value = string.IsNullOrWhiteSpace(type) ? "UNK" : type;
            obx.ObservationSubID.Value = "1";

            var value = new NHapi.Model.V23.Datatype.TX(message)
            {
                Value = string.IsNullOrWhiteSpace(reason) ? "Not specified" : reason
            };

            obx.GetObservationValue(0).Data = value;
            obx.ObservResultStatus.Value = "F";
        }
    }

    private static IMessage CreateAdtMessage(string triggerEvent)
    {
        return triggerEvent.ToUpperInvariant() switch
        {
            "A01" => new ADT_A01(),
            "A02" => new ADT_A02(),
            "A03" => new ADT_A03(),
            "A04" => new ADT_A04(),
            "A05" => new ADT_A05(),
            "A06" => new ADT_A06(),
            "A07" => new ADT_A07(),
            "A08" => new ADT_A08(),
            "A09" => new ADT_A09(),
            "A10" => new ADT_A10(),
            "A11" => new ADT_A11(),
            "A12" => new ADT_A12(),
            "A13" => new ADT_A13(),
            "A14" => new ADT_A14(),
            "A15" => new ADT_A15(),
            "A16" => new ADT_A16(),
            "A18" => new ADT_A18(),
            "A21" => new ADT_A21(),
            "A22" => new ADT_A22(),
            "A24" => new ADT_A24(),
            "A25" => new ADT_A25(),
            "A26" => new ADT_A26(),
            "A27" => new ADT_A27(),
            "A28" => new ADT_A28(),
            "A31" => new ADT_A31(),
            "A32" => new ADT_A32(),
            "A33" => new ADT_A33(),
            "A37" => new ADT_A37(),
            "A38" => new ADT_A38(),
            "A40" => new ADT_A40(),
            _ => throw new NotSupportedException($"Unsupported ADT trigger event: '{triggerEvent}'.")
        };
    }

    private static (string messageCode, string triggerEvent) ParseMessageTypeCode(string messageTypeCode)
    {
        if (string.IsNullOrWhiteSpace(messageTypeCode))
        {
            throw new ArgumentException("MessageTypeCode cannot be empty.", nameof(messageTypeCode));
        }

        var parts = messageTypeCode.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            throw new ArgumentException("MessageTypeCode must follow '<TYPE> <TRIGGER>' format.", nameof(messageTypeCode));
        }

        return (parts[0].Trim(), parts[1].Trim());
    }

    private static string GenerateControlIdFromMessage(string message)
    {
        using var sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.Latin1.GetBytes(message));

        var builder = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }

        return builder.ToString().Substring(0, 19);
    }
}
