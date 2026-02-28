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
/// Représente les données nécessaires pour générer un message ADT.
/// Cette classe est indépendante de toute UI (WinForms, MAUI, ...).
/// </summary>
public sealed class AdtMessageRequest
{
    /// <summary>
    /// Code de message sous la forme "ADT A01", "ADT A03", ...
    /// </summary>
    public string MessageTypeCode { get; set; } = "ADT A01";

    public string PatientId { get; set; } = string.Empty;
    public string PatientFamilyName { get; set; } = string.Empty;
    public string PatientGivenName { get; set; } = string.Empty;
    public string BirthDate { get; set; } = string.Empty; // format attendu : yyyyMMdd
    public string Sex { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string Bed { get; set; } = string.Empty;
    public string Facility { get; set; } = string.Empty;
    public string Floor { get; set; } = string.Empty;
    public string AdmissionNumber { get; set; } = string.Empty;

    /// <summary>
    /// Nouvel identifiant patient utilisé pour les messages de type fusion / mise à jour
    /// (ex. ADT A18 / ADT A40). Correspond à txtNewPatientID dans l'UI WinForms.
    /// </summary>
    public string? NewPatientId { get; set; }

    /// <summary>
    /// Date/heure d'événement au format HL7 (yyyyMMddHHmm). Si null, la date courante sera utilisée.
    /// </summary>
    public string? EventDateTime { get; set; }

    /// <summary>
    /// Nom de l'application émettrice (MSH-3).
    /// </summary>
    public string SendingApplication { get; set; } = "Hl7Tester-Core";

    /// <summary>
    /// Entrées OBX optionnelles (jusqu'à 3 dans l'UI actuelle, mais extensible côté Core).
    /// </summary>
    public IList<ObxEntry> ObxEntries { get; } = new List<ObxEntry>();
}

/// <summary>
/// Représente une observation OBX libre (type + raison/texte).
/// </summary>
public sealed class ObxEntry
{
    public string? Type { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Service de génération de messages ADT (HL7 v2.3) basé sur NHapi.
/// </summary>
public sealed class AdtMessageGenerator
{
    private readonly ILogger<AdtMessageGenerator> _logger;

    static AdtMessageGenerator()
    {
        // Permet d'utiliser des encodages codepages (windows-1252) sur .NET moderne / macOS.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public AdtMessageGenerator(ILogger<AdtMessageGenerator>? logger = null)
    {
        _logger = logger ?? NullLogger<AdtMessageGenerator>.Instance;
    }

    public string Generate(AdtMessageRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        _logger.LogInformation("Generating HL7 message for type {MessageType}", request.MessageTypeCode);

        var message = CreateMessage(request.MessageTypeCode, out var messageCode, out var triggerEvent);

        var msh = (MSH)message.GetStructure("MSH");
        msh.FieldSeparator.Value = "|";
        msh.EncodingCharacters.Value = "^~\\&";
        msh.SendingApplication.NamespaceID.Value = request.SendingApplication;
        msh.MessageType.MessageType.Value = messageCode;
        msh.MessageType.TriggerEvent.Value = triggerEvent;
        msh.ProcessingID.ProcessingID.Value = "P";
        msh.VersionID.Value = "2.3";
        msh.DateTimeOfMessage.TimeOfAnEvent.Value = DateTime.Now.ToString("yyyyMMddHHmmss");
        msh.CharacterSet.Value = "ASCII";

        // Remplissage des segments dépendant du type de message
        if (message is ADT_A40 adtA40)
        {
            // Cas fusion de dossiers patients (A40) :
            // - PID contient le "nouveau" patient (NewPatientId)
            // - MRG contient l'ancien identifiant (PatientId)
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
            // Cas "merge"/mise à jour A18 : PID = nouveau, MRG = ancien
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
            // Cas "classique" (A01, A03, ...): PID + PV1 "visite" complète
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

            // OBX optionnels
            FillObxSegments(message, request.ObxEntries);
        }

        // EVN
        var evn = (EVN)message.GetStructure("EVN");
        evn.EventTypeCode.Value = triggerEvent;
        evn.RecordedDateTime.TimeOfAnEvent.Value = DateTime.Now.ToString("yyyyMMddHHmm");
        evn.EventOccured.TimeOfAnEvent.Value =
            string.IsNullOrWhiteSpace(request.EventDateTime)
                ? DateTime.Now.ToString("yyyyMMddHHmm")
                : request.EventDateTime;

        var parser = new PipeParser();

        // Premier encodage pour calculer le Control ID à partir du message.
        string encoded = parser.Encode(message);
        string controlId = GenerateControlIdFromMessage(encoded);
        msh.MessageControlID.Value = controlId;

        // Ré-encodage avec le Control ID en place.
        var finalMessage = parser.Encode(message);
        _logger.LogInformation("HL7 message generated successfully (ControlID={ControlId})", controlId);
        return finalMessage;
    }

    private static void FillObxSegments(IMessage message, IEnumerable<ObxEntry> entries)
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
                Value = string.IsNullOrWhiteSpace(reason) ? "Non spécifié" : reason
            };

            obx.GetObservationValue(0).Data = value;
            obx.ObservResultStatus.Value = "F";
        }
    }

    private static IMessage CreateMessage(string messageTypeCode, out string messageCode, out string triggerEvent)
    {
        if (string.IsNullOrWhiteSpace(messageTypeCode))
            throw new ArgumentException("MessageTypeCode ne peut pas être vide.", nameof(messageTypeCode));

        var parts = messageTypeCode.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            throw new ArgumentException("MessageTypeCode doit être de la forme 'ADT A01'.", nameof(messageTypeCode));

        messageCode = parts[0].Trim();
        triggerEvent = parts[1].Trim();

        string key = $"{messageCode} {triggerEvent}";

        return key switch
        {
            "ADT A01" => new ADT_A01(),
            "ADT A02" => new ADT_A02(),
            "ADT A03" => new ADT_A03(),
            "ADT A04" => new ADT_A04(),
            "ADT A05" => new ADT_A05(),
            "ADT A06" => new ADT_A06(),
            "ADT A07" => new ADT_A07(),
            "ADT A08" => new ADT_A08(),
            "ADT A09" => new ADT_A09(),
            "ADT A10" => new ADT_A10(),
            "ADT A11" => new ADT_A11(),
            "ADT A12" => new ADT_A12(),
            "ADT A13" => new ADT_A13(),
            "ADT A14" => new ADT_A14(),
            "ADT A15" => new ADT_A15(),
            "ADT A16" => new ADT_A16(),
            "ADT A18" => new ADT_A18(),
            "ADT A21" => new ADT_A21(),
            "ADT A22" => new ADT_A22(),
            "ADT A24" => new ADT_A24(),
            "ADT A25" => new ADT_A25(),
            "ADT A26" => new ADT_A26(),
            "ADT A27" => new ADT_A27(),
            "ADT A28" => new ADT_A28(),
            "ADT A31" => new ADT_A31(),
            "ADT A32" => new ADT_A32(),
            "ADT A33" => new ADT_A33(),
            "ADT A37" => new ADT_A37(),
            "ADT A38" => new ADT_A38(),
            "ADT A40" => new ADT_A40(),
            _ => throw new NotSupportedException($"Type de message ADT non supporté : '{messageTypeCode}'")
        };
    }

    private static string GenerateControlIdFromMessage(string message)
    {
        using var sha256 = SHA256.Create();
        // Sous macOS / .NET moderne, on s'appuie sur Latin1, très proche de Windows-1252
        // et suffisant pour nos besoins HL7 (messages principalement ASCII).
        byte[] bytes = sha256.ComputeHash(Encoding.Latin1.GetBytes(message));
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }

        // On garde 19 caractères comme dans l'implémentation WinForms existante.
        return builder.ToString().Substring(0, 19);
    }

}

