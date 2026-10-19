using System.Text.Json;
using Hl7.Fhir.Model;
using HL7Tester.Core;
using HL7Tester.Core.Fhir;
using HL7Tester.Core.Inspector.Services;

namespace HL7Tester.Tests;

[TestClass]
public sealed class FhirConverterTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Hl7MessageExtractor tests
    // ═══════════════════════════════════════════════════════════════════════════

    private static readonly HL7ParserService _parser = new();

    [TestMethod]
    public void Extract_A01Message_ReturnsCorrectRequest()
    {
        var raw = "MSH|^~\\&|SENDER|FACILITY|RECEIVER|FACILITY|20240101120000||ADT^A01|CTRL001|P|2.3\r" +
                  "EVN|A01|20240101120000\r" +
                  "PID|1||PAT123||Doe^John||19800101|M\r" +
                  "PV1|1|I|UNIT1^ROOM1^BED1^FAC1||||REF||||||||||||ADM001";

        var parsed = _parser.Parse(raw);
        Assert.IsTrue(parsed.IsSuccess, parsed.ParseError);

        var request = Hl7MessageExtractor.Extract(parsed);

        Assert.AreEqual("ADT A01", request.MessageTypeCode);
        Assert.AreEqual("PAT123", request.PatientId);
        Assert.AreEqual("Doe", request.PatientFamilyName);
        Assert.AreEqual("John", request.PatientGivenName);
        Assert.AreEqual("19800101", request.BirthDate);
        Assert.AreEqual("M", request.Sex);
        Assert.AreEqual("SENDER", request.SendingApplication);
        Assert.AreEqual("FACILITY", request.SendingFacility);
        Assert.AreEqual("RECEIVER", request.ReceivingApplication);
        Assert.AreEqual("ADM001", request.AdmissionNumber);
        Assert.AreEqual("UNIT1", request.Unit);
        Assert.AreEqual("ROOM1", request.Room);
        Assert.AreEqual("BED1", request.Bed);
        Assert.AreEqual("FAC1", request.Facility);
    }

    [TestMethod]
    public void Extract_A03Message_ReturnsA03Type()
    {
        var raw = "MSH|^~\\&|SENDER|FAC|RECEIVER|FAC|20240101120000||ADT^A03|CTRL002|P|2.3\r" +
                  "EVN|A03|20240101120000\r" +
                  "PID|1||PAT456||Smith^Jane||19750515|F\r" +
                  "PV1|1|I|WARD^R10^B2^BLD|||||||REF|||||||||||ADM999";

        var parsed = _parser.Parse(raw);
        Assert.IsTrue(parsed.IsSuccess, parsed.ParseError);

        var request = Hl7MessageExtractor.Extract(parsed);

        Assert.AreEqual("ADT A03", request.MessageTypeCode);
        Assert.AreEqual("PAT456", request.PatientId);
        Assert.AreEqual("Smith", request.PatientFamilyName);
        Assert.AreEqual("Jane", request.PatientGivenName);
        Assert.AreEqual("F", request.Sex);
    }

    [TestMethod]
    public void Extract_O01Message_ReturnsOrmFields()
    {
        var raw = "MSH|^~\\&|SENDER|FAC|RECEIVER|FAC|20240101120000||ORM^O01|CTRL003|P|2.3\r" +
                  "PID|1||PAT789||Doe^John\r" +
                  "ORC|NW|PO-001|FA-001|||||||||PRV-1^HOUSE^GREGORY\r" +
                  "OBR|1|||LAB^Blood test";

        var parsed = _parser.Parse(raw);
        Assert.IsTrue(parsed.IsSuccess, parsed.ParseError);

        var request = Hl7MessageExtractor.Extract(parsed);

        Assert.AreEqual("ORM O01", request.MessageTypeCode);
        Assert.AreEqual("NW", request.OrderControl);
        Assert.AreEqual("PO-001", request.OrmPlacerOrderNumber);
        Assert.AreEqual("FA-001", request.OrmFillerOrderNumber);
        Assert.AreEqual("PRV-1", request.OrmOrderingProviderId);
        Assert.AreEqual("HOUSE", request.OrmOrderingProviderFamilyName);
        Assert.AreEqual("GREGORY", request.OrmOrderingProviderGivenName);
        Assert.AreEqual("LAB^Blood test", request.OrmUniversalServiceId);
    }

    [TestMethod]
    public void Extract_S12Message_ReturnsSiuFields()
    {
        var raw = "MSH|^~\\&|SENDER|FAC|RECEIVER|FAC|20240101120000||SIU^S12|CTRL004|P|2.3\r" +
                  "PID|1||PAT321||Doe^John\r" +
                  "SCH|APPT-1|FA-APPT-1|||1|OFFICE^Office visit|Checkup|30|60|m|^^^20261020100000^20261020103000||||||PRV-1^HOUSE^GREGORY|||||Scheduled";

        var parsed = _parser.Parse(raw);
        Assert.IsTrue(parsed.IsSuccess, parsed.ParseError);

        var request = Hl7MessageExtractor.Extract(parsed);

        Assert.AreEqual("SIU S12", request.MessageTypeCode);
        Assert.AreEqual("APPT-1", request.SiuPlacerAppointmentId);
        Assert.AreEqual("FA-APPT-1", request.SiuFillerAppointmentId);
        Assert.AreEqual("OFFICE^Office visit", request.SiuAppointmentType);
        Assert.AreEqual("Checkup", request.SiuAppointmentReason);
        Assert.AreEqual("60", request.SiuAppointmentDuration);
        Assert.AreEqual("m", request.SiuAppointmentDurationUnits);
        Assert.AreEqual("20261020100000", request.SiuAppointmentStartDateTime);
        Assert.AreEqual("20261020103000", request.SiuAppointmentEndDateTime);
        Assert.AreEqual("PRV-1", request.SiuPersonnelId);
        Assert.AreEqual("HOUSE", request.SiuPersonnelFamilyName);
        Assert.AreEqual("GREGORY", request.SiuPersonnelGivenName);
        Assert.AreEqual("Scheduled", request.SiuAppointmentStatus);
    }

    [TestMethod]
    public void Extract_EmptyMessage_ReturnsDefaults()
    {
        // Minimal message: only MSH segment.
        var raw = "MSH|^~\\&|SENDER|FAC|RECEIVER|FAC|20240101120000||ADT^A01|CTRL005|P|2.3";

        var parsed = _parser.Parse(raw);
        Assert.IsTrue(parsed.IsSuccess, parsed.ParseError);

        var request = Hl7MessageExtractor.Extract(parsed);

        Assert.AreEqual("ADT A01", request.MessageTypeCode);
        // No PID/PV1 → patient fields remain at default (empty string).
        Assert.AreEqual(string.Empty, request.PatientId);
        Assert.AreEqual(string.Empty, request.PatientFamilyName);
        Assert.AreEqual(string.Empty, request.PatientGivenName);
        Assert.AreEqual(string.Empty, request.BirthDate);
        Assert.AreEqual(string.Empty, request.Sex);
        Assert.AreEqual(string.Empty, request.AdmissionNumber);
        Assert.AreEqual(string.Empty, request.Unit);
        Assert.AreEqual(string.Empty, request.Room);
    }

    [TestMethod]
    public void Extract_MultipleObx_ReturnsObxEntries()
    {
        var raw = "MSH|^~\\&|SENDER|FAC|RECEIVER|FAC|20240101120000||ADT^A01|CTRL006|P|2.3\r" +
                  "PID|1||PAT123||Doe^John\r" +
                  "OBX|1|ST|OBS-1||Value One\r" +
                  "OBX|2|ST|OBS-2||Value Two\r" +
                  "OBX|3|ST|OBS-3||Value Three";

        var parsed = _parser.Parse(raw);
        Assert.IsTrue(parsed.IsSuccess, parsed.ParseError);

        var request = Hl7MessageExtractor.Extract(parsed);

        Assert.AreEqual(3, request.ObxEntries.Count);
        Assert.AreEqual("OBS-1", request.ObxEntries[0].Type);
        Assert.AreEqual("Value One", request.ObxEntries[0].Reason);
        Assert.AreEqual("OBS-2", request.ObxEntries[1].Type);
        Assert.AreEqual("Value Two", request.ObxEntries[1].Reason);
        Assert.AreEqual("OBS-3", request.ObxEntries[2].Type);
        Assert.AreEqual("Value Three", request.ObxEntries[2].Reason);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FhirToHl7Converter tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Convert_PatientJson_ReturnsA08Request()
    {
        var json = """
        {"resourceType":"Patient","id":"p1","identifier":[{"value":"PAT123"}],"name":[{"family":"Doe","given":["John"]}],"gender":"male","birthDate":"1980-01-01"}
        """;

        var request = FhirToHl7Converter.Convert(json);

        Assert.IsNotNull(request);
        Assert.AreEqual("ADT A08", request.MessageTypeCode);
        Assert.AreEqual("PAT123", request.PatientId);
        Assert.AreEqual("Doe", request.PatientFamilyName);
        Assert.AreEqual("John", request.PatientGivenName);
        Assert.AreEqual("M", request.Sex);
        Assert.AreEqual("19800101", request.BirthDate);
    }

    [TestMethod]
    public void Convert_BundleTransaction_ReturnsA01Request()
    {
        var json = """
        {"resourceType":"Bundle","type":"transaction","entry":[{"resource":{"resourceType":"Patient","identifier":[{"system":"urn:hl7tester:patient-id","value":"PAT123"}],"name":[{"family":"Doe","given":["John"]}],"gender":"male","birthDate":"1980-01-01"},"request":{"method":"POST","url":"Patient"}},{"resource":{"resourceType":"Encounter","status":"in-progress","class":{"system":"http://terminology.hl7.org/CodeSystem/v3-ActCode","code":"IMP"},"identifier":[{"system":"urn:hl7tester:admission-number","value":"ADM001"}],"location":[{"location":{"display":"Unit=ICU Room=101 Bed=3 Floor=2"}}]},"request":{"method":"POST","url":"Encounter"}}]}
        """;

        var request = FhirToHl7Converter.Convert(json);

        Assert.IsNotNull(request);
        Assert.AreEqual("ADT A01", request.MessageTypeCode);
        Assert.AreEqual("PAT123", request.PatientId);
        Assert.AreEqual("Doe", request.PatientFamilyName);
        Assert.AreEqual("John", request.PatientGivenName);
        Assert.AreEqual("ADM001", request.AdmissionNumber);
        Assert.AreEqual("ICU", request.Unit);
        Assert.AreEqual("101", request.Room);
        Assert.AreEqual("3", request.Bed);
        Assert.AreEqual("2", request.Floor);
    }

    [TestMethod]
    public void Convert_EncounterFinished_ReturnsA03()
    {
        var json = """
        {"resourceType":"Encounter","status":"finished","class":{"system":"http://terminology.hl7.org/CodeSystem/v3-ActCode","code":"IMP"},"identifier":[{"value":"VISIT-1"}]}
        """;

        var request = FhirToHl7Converter.Convert(json);

        Assert.IsNotNull(request);
        Assert.AreEqual("ADT A03", request.MessageTypeCode);
        Assert.AreEqual("VISIT-1", request.AdmissionNumber);
    }

    [TestMethod]
    public void Convert_ServiceRequest_ReturnsO01()
    {
        // Generate a valid ServiceRequest via FhirR4Generator, then round-trip through the converter.
        var generator = new FhirR4Generator();
        var genReq = new AdtMessageRequest
        {
            MessageTypeCode = "ORM O01",
            PatientId = "PAT-1",
            OrmPlacerOrderNumber = "PO-001",
            OrmUniversalServiceId = "LAB^Blood test",
            OrmOrderingProviderId = "PRV-1",
            OrmOrderingProviderFamilyName = "HOUSE",
            OrmOrderingProviderGivenName = "GREGORY"
        };
        var json = generator.ToJson(generator.Generate(genReq));

        var request = FhirToHl7Converter.Convert(json);

        Assert.IsNotNull(request);
        Assert.AreEqual("ORM O01", request.MessageTypeCode);
        Assert.AreEqual("PO-001", request.OrmPlacerOrderNumber);
    }

    [TestMethod]
    public void Convert_AppointmentCancelled_ReturnsS15()
    {
        // Generate a cancelled Appointment via FhirR4Generator, then round-trip through the converter.
        var generator = new FhirR4Generator();
        var genReq = new AdtMessageRequest
        {
            MessageTypeCode = "SIU S15",
            PatientId = "PAT-1",
            SiuPlacerAppointmentId = "APPT-1",
            SiuAppointmentStartDateTime = "20261020100000",
            SiuAppointmentEndDateTime = "20261020103000",
            SiuAppointmentReason = "Checkup",
            SiuPersonnelId = "PRV-1",
            SiuPersonnelFamilyName = "HOUSE",
            SiuPersonnelGivenName = "GREGORY",
            SiuLocationCode = "ROOM-1",
            SiuLocationDescription = "Main Office",
            SiuGeneralResourceId = "EQ-1",
            SiuGeneralResourceName = "Exam room device"
        };
        var json = generator.ToJson(generator.Generate(genReq));

        var request = FhirToHl7Converter.Convert(json);

        Assert.IsNotNull(request);
        Assert.AreEqual("SIU S15", request.MessageTypeCode);
    }

    [TestMethod]
    public void Convert_InvalidJson_ReturnsNull()
    {
        var request = FhirToHl7Converter.Convert("this is not json {{{");
        Assert.IsNull(request);
    }

    [TestMethod]
    public void Convert_UnknownResourceType_ReturnsNull()
    {
        var json = """
        {"resourceType":"Observation","status":"final","code":{"text":"test"}}
        """;

        var request = FhirToHl7Converter.Convert(json);
        Assert.IsNull(request);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FhirR4Generator — additional JSON validity tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ToJson_ServiceRequest_ProducesValidJsonWithResourceType()
    {
        var generator = new FhirR4Generator();
        var request = new AdtMessageRequest
        {
            MessageTypeCode = "ORM O01",
            PatientId = "PAT-1",
            OrmPlacerOrderNumber = "PO-001",
            OrmUniversalServiceId = "LAB^Blood test",
            OrmOrderingProviderId = "PRV-1",
            OrmOrderingProviderFamilyName = "HOUSE",
            OrmOrderingProviderGivenName = "GREGORY"
        };

        var json = generator.ToJson(generator.Generate(request));

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.AreEqual(JsonValueKind.Object, root.ValueKind);
        Assert.AreEqual("ServiceRequest", root.GetProperty("resourceType").GetString());
    }

    [TestMethod]
    public void ToJson_Appointment_ProducesValidJsonWithResourceType()
    {
        var generator = new FhirR4Generator();
        var request = new AdtMessageRequest
        {
            MessageTypeCode = "SIU S12",
            PatientId = "PAT-1",
            SiuPlacerAppointmentId = "APPT-1",
            SiuAppointmentStartDateTime = "20261020100000",
            SiuAppointmentEndDateTime = "20261020103000",
            SiuAppointmentReason = "Checkup",
            SiuPersonnelId = "PRV-1",
            SiuPersonnelFamilyName = "HOUSE",
            SiuPersonnelGivenName = "GREGORY",
            SiuLocationCode = "ROOM-1",
            SiuLocationDescription = "Main Office",
            SiuGeneralResourceId = "EQ-1",
            SiuGeneralResourceName = "Exam room device"
        };

        var json = generator.ToJson(generator.Generate(request));

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.AreEqual(JsonValueKind.Object, root.ValueKind);
        Assert.AreEqual("Appointment", root.GetProperty("resourceType").GetString());
        Assert.AreEqual("booked", root.GetProperty("status").GetString());
    }
}
