using System.Text.Json;
using Hl7.Fhir.Model;
using HL7Tester.Core;
using HL7Tester.Core.Fhir;

namespace HL7Tester.Tests;

[TestClass]
public sealed class FhirR4GeneratorTests
{
    private static AdtMessageRequest CreateBaseRequest(string messageTypeCode) => new()
    {
        MessageTypeCode = messageTypeCode,
        PatientId = "12345",
        PatientFamilyName = "DUPONT",
        PatientGivenName = "JEAN",
        BirthDate = "19800101",
        Sex = "M",
        Unit = "ICU",
        Room = "101",
        Bed = "3",
        Floor = "2",
        AdmissionNumber = "ADM-42",
        NewPatientId = "99999",
        OrmOrderStatus = "Final",
        OrmUniversalServiceId = "LAB^Blood test",
        OrmPlacerOrderNumber = "PO-001",
        OrmOrderingProviderId = "PRV-1",
        OrmOrderingProviderFamilyName = "HOUSE",
        OrmOrderingProviderGivenName = "GREGORY",
        SiuAppointmentStatus = "Scheduled",
        SiuAppointmentStartDateTime = "20261020100000",
        SiuAppointmentEndDateTime = "20261020103000",
        SiuAppointmentReason = "Annual checkup",
        SiuPlacerAppointmentId = "APPT-1",
        SiuPersonnelId = "PRV-1",
        SiuPersonnelFamilyName = "HOUSE",
        SiuPersonnelGivenName = "GREGORY",
        SiuGeneralResourceId = "EQ-1",
        SiuGeneralResourceName = "Exam room device",
        SiuLocationCode = "ROOM-1",
        SiuLocationDescription = "Main Office"
    };

    [TestMethod]
    public void Generate_A01_ReturnsTransactionBundle()
    {
        var generator = new FhirR4Generator();
        var request = CreateBaseRequest("ADT A01 - Inpatient or Day Hospital Admission");

        var resource = generator.Generate(request);

        Assert.IsInstanceOfType(resource, typeof(Bundle));
        var bundle = (Bundle)resource;

        Assert.AreEqual(Bundle.BundleType.Transaction, bundle.Type);
        Assert.AreEqual(2, bundle.Entry.Count);

        // Entry 1: Patient (POST) with urn:uuid fullUrl.
        Assert.AreEqual(Bundle.HTTPVerb.POST, bundle.Entry[0].Request!.Method);
        Assert.AreEqual("Patient", bundle.Entry[0].Request.Url);
        StringAssert.StartsWith(bundle.Entry[0].FullUrl!, "urn:uuid:");

        var patient = bundle.Entry[0].Resource as Patient;
        Assert.IsNotNull(patient);
        Assert.AreEqual("DUPONT", patient!.Name[0].Family);
        Assert.AreEqual("JEAN", patient.Name[0].Given!.First());
        Assert.AreEqual("12345", patient.Identifier[0].Value);
        Assert.AreEqual(AdministrativeGender.Male, patient.Gender);
        Assert.AreEqual("1980-01-01", patient.BirthDate);

        // Entry 2: Encounter (POST) in-progress, linked to the Patient via urn:uuid.
        Assert.AreEqual(Bundle.HTTPVerb.POST, bundle.Entry[1].Request!.Method);
        Assert.AreEqual("Encounter", bundle.Entry[1].Request.Url);

        var encounter = bundle.Entry[1].Resource as Encounter;
        Assert.IsNotNull(encounter);
        Assert.AreEqual(Encounter.EncounterStatus.InProgress, encounter!.Status);
        Assert.AreEqual("IMP", encounter.Class!.Code);
        Assert.AreEqual(bundle.Entry[0].FullUrl, encounter.Subject!.Reference);
        Assert.AreEqual(1, encounter.Location.Count);
    }

    [TestMethod]
    public void Generate_A08_ReturnsPatient()
    {
        var generator = new FhirR4Generator();
        var request = CreateBaseRequest("ADT A08 - Update Visit Information");
        request.Sex = "F";

        var resource = generator.Generate(request);

        Assert.IsInstanceOfType(resource, typeof(Patient));
        var patient = (Patient)resource;

        Assert.AreEqual("12345", patient.Id);
        Assert.AreEqual("DUPONT", patient.Name[0].Family);
        Assert.AreEqual(AdministrativeGender.Female, patient.Gender);
        Assert.AreEqual("1980-01-01", patient.BirthDate);
    }

    [TestMethod]
    public void Generate_A31_ReturnsPatient()
    {
        var generator = new FhirR4Generator();
        var request = CreateBaseRequest("ADT A31 - Update Patient Information");

        var resource = generator.Generate(request);

        Assert.IsInstanceOfType(resource, typeof(Patient));
        var patient = (Patient)resource;
        Assert.AreEqual("12345", patient.Id);
    }

    [TestMethod]
    public void Generate_A03_ReturnsEncounterFinished()
    {
        var generator = new FhirR4Generator();
        var request = CreateBaseRequest("ADT A03 - Discharge Patient");

        var resource = generator.Generate(request);

        Assert.IsInstanceOfType(resource, typeof(Encounter));
        var encounter = (Encounter)resource;

        Assert.AreEqual(Encounter.EncounterStatus.Finished, encounter.Status);
        Assert.IsNotNull(encounter.Period);
        Assert.IsNotNull(encounter.Period!.End);
        // Conditional subject reference (decision D7).
        StringAssert.StartsWith(encounter.Subject!.Reference!, "Patient?identifier=");
    }

    [TestMethod]
    public void Generate_A02_ReturnsEncounterInProgressWithLocation()
    {
        var generator = new FhirR4Generator();
        var request = CreateBaseRequest("ADT A02 - Transfer Patient");

        var resource = generator.Generate(request);

        Assert.IsInstanceOfType(resource, typeof(Encounter));
        var encounter = (Encounter)resource;

        Assert.AreEqual(Encounter.EncounterStatus.InProgress, encounter.Status);
        Assert.AreEqual(1, encounter.Location.Count);
        StringAssert.Contains(encounter.Location[0].Location!.Display!, "Unit=ICU");
    }

    [TestMethod]
    public void Generate_A40_ReturnsMergeParameters()
    {
        var generator = new FhirR4Generator();
        var request = CreateBaseRequest("ADT A40 - Merge Patient Records");

        var resource = generator.Generate(request);

        Assert.IsInstanceOfType(resource, typeof(Parameters));
        var parameters = (Parameters)resource;

        Assert.AreEqual(2, parameters.Parameter.Count);
        Assert.AreEqual("sourcePatientId", parameters.Parameter[0].Name);
        Assert.AreEqual("12345", ((FhirString)parameters.Parameter[0].Value!).Value);
        Assert.AreEqual("targetPatientId", parameters.Parameter[1].Name);
        Assert.AreEqual("99999", ((FhirString)parameters.Parameter[1].Value!).Value);
    }

    [TestMethod]
    public void Generate_A18_UsesConfiguredMergeParameterNames()
    {
        var config = new FhirMergeConfig
        {
            SourceParamName = "oldPatientId",
            TargetParamName = "newPatientId"
        };
        var generator = new FhirR4Generator(config);
        var request = CreateBaseRequest("ADT A18 - Merge Patient Information");

        var resource = generator.Generate(request);

        var parameters = (Parameters)resource;
        Assert.AreEqual("oldPatientId", parameters.Parameter[0].Name);
        Assert.AreEqual("newPatientId", parameters.Parameter[1].Name);
    }

    [TestMethod]
    public void Generate_O01_ReturnsServiceRequest()
    {
        var generator = new FhirR4Generator();
        var request = CreateBaseRequest("ORM O01 - General Order");

        var resource = generator.Generate(request);

        Assert.IsInstanceOfType(resource, typeof(ServiceRequest));
        var serviceRequest = (ServiceRequest)resource;

        // HL7 "Final" maps to FHIR R4 Completed (no "final" status in R4 ServiceRequest).
        Assert.AreEqual(RequestStatus.Completed, serviceRequest.Status);
        Assert.AreEqual(RequestIntent.Order, serviceRequest.Intent);
        Assert.AreEqual("LAB", serviceRequest.Code!.Coding[0].Code);
        Assert.AreEqual("Blood test", serviceRequest.Code.Coding[0].Display);
        StringAssert.StartsWith(serviceRequest.Subject!.Reference!, "Patient?identifier=");
        StringAssert.StartsWith(serviceRequest.Requester!.Reference!, "Practitioner?identifier=");
    }

    [TestMethod]
    public void Generate_S12_ReturnsAppointment()
    {
        var generator = new FhirR4Generator();
        var request = CreateBaseRequest("SIU S12 - New Appointment Request");

        var resource = generator.Generate(request);

        Assert.IsInstanceOfType(resource, typeof(Appointment));
        var appointment = (Appointment)resource;

        // HL7 "Scheduled" maps to FHIR R4 Booked (no "scheduled" status in R4 Appointment).
        Assert.AreEqual(Appointment.AppointmentStatus.Booked, appointment.Status);

        // Firely SDK 6.x exposes instants as DateTimeOffset; compare local wall-clock.
        Assert.IsNotNull(appointment.Start);
        Assert.IsNotNull(appointment.End);
        Assert.AreEqual("2026-10-20T10:00:00", appointment.Start!.Value.ToString("yyyy-MM-ddTHH:mm:ss"));
        Assert.AreEqual("2026-10-20T10:30:00", appointment.End!.Value.ToString("yyyy-MM-ddTHH:mm:ss"));
        Assert.AreEqual("Annual checkup", appointment.Description);

        // Patient + practitioner + resource + location participants.
        Assert.AreEqual(4, appointment.Participant.Count);
        StringAssert.StartsWith(appointment.Participant[0].Actor!.Reference!, "Patient?identifier=");
        StringAssert.StartsWith(appointment.Participant[1].Actor!.Reference!, "Practitioner?identifier=");
        StringAssert.StartsWith(appointment.Participant[2].Actor!.Reference!, "HealthcareService?identifier=");
        StringAssert.StartsWith(appointment.Participant[3].Actor!.Reference!, "Location?identifier=");
    }

    [TestMethod]
    public void Generate_S15_ReturnsCancelledAppointment()
    {
        var generator = new FhirR4Generator();
        var request = CreateBaseRequest("SIU S15 - Appointment Cancellation Request");

        var resource = generator.Generate(request);

        Assert.IsInstanceOfType(resource, typeof(Appointment));
        var appointment = (Appointment)resource;

        Assert.AreEqual(Appointment.AppointmentStatus.Cancelled, appointment.Status);
        Assert.AreEqual(1, appointment.Identifier.Count);
        Assert.AreEqual("APPT-1", appointment.Identifier[0].Value);
    }

    [TestMethod]
    public void Generate_UnknownMessageType_Throws()
    {
        var generator = new FhirR4Generator();
        var request = CreateBaseRequest("XYZ Z99 - Not Supported");

        Assert.Throws<NotSupportedException>(() => generator.Generate(request));
    }

    [TestMethod]
    public void ToJson_ProducesValidJson()
    {
        var generator = new FhirR4Generator();
        var request = CreateBaseRequest("ADT A01");

        var json = generator.ToJson(generator.Generate(request));

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.AreEqual(JsonValueKind.Object, root.ValueKind);
        Assert.AreEqual("Bundle", root.GetProperty("resourceType").GetString());
        Assert.AreEqual("transaction", root.GetProperty("type").GetString());

        // Pretty-printed: the serializer emits newlines and indentation.
        StringAssert.Contains(json, Environment.NewLine);
    }

    [TestMethod]
    public void ToJson_PatientJsonContainsExpectedFields()
    {
        var generator = new FhirR4Generator();
        var request = CreateBaseRequest("ADT A08");

        var json = generator.ToJson(generator.Generate(request));

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.AreEqual("Patient", root.GetProperty("resourceType").GetString());
        Assert.AreEqual("12345", root.GetProperty("id").GetString());
        Assert.AreEqual("male", root.GetProperty("gender").GetString());
        Assert.AreEqual("1980-01-01", root.GetProperty("birthDate").GetString());
        Assert.AreEqual("DUPONT", root.GetProperty("name")[0].GetProperty("family").GetString());
    }
}
