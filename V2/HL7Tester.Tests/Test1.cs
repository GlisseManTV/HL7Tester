using HL7Tester.Core;

namespace HL7Tester.Tests;

[TestClass]
public sealed class AdtMessageGeneratorTests
{
    [TestMethod]
    public void Generate_WithBasicA01Request_ProducesMessageWithExpectedTypeAndPid()
    {
        var generator = new AdtMessageGenerator();
        var request = CreateBaseRequest("ADT A01");

        request.ObxEntries.Add(new ObxEntry { Type = "TYPE1", Reason = "Reason1" });
        request.ObxEntries.Add(new ObxEntry { Type = "TYPE2", Reason = "Reason2" });

        string hl7 = generator.Generate(request);

        StringAssert.Contains(hl7, "MSH|");
        StringAssert.Contains(hl7, "ADT^A01");
        StringAssert.Contains(hl7, "PID|1||12345");
        StringAssert.Contains(hl7, "OBX|1|TX|TYPE1");
        StringAssert.Contains(hl7, "OBX|2|TX|TYPE2");
    }

    [TestMethod]
    public void Generate_WithOrmO01Request_ProducesExpectedOrmSegments()
    {
        var generator = new AdtMessageGenerator();
        var request = CreateBaseRequest("ORM O01");

        request.OrderControl = "NW";
        request.OrmPlacerOrderNumber = "PO-001";
        request.OrmFillerOrderNumber = "FO-001";
        request.OrmOrderStatus = "CM";
        request.OrmUniversalServiceId = "LAB^Blood test";
        request.OrmRequestedDateTime = "20260305183000";
        request.OrmOrderingProviderId = "1234";
        request.OrmOrderingProviderFamilyName = "HOUSE";
        request.OrmOrderingProviderGivenName = "GREGORY";
        request.OrmDiagnosisCode = "R50.9";
        request.OrmDiagnosisText = "Fever";
        request.ObxEntries.Add(new ObxEntry { Type = "NOTE", Reason = "Order created" });

        string hl7 = generator.Generate(request);

        StringAssert.Contains(hl7, "MSH|");
        StringAssert.Contains(hl7, "ORM^O01");
        StringAssert.Contains(hl7, "PID|1||12345");
        StringAssert.Contains(hl7, "PV1|1|I|CARDIO^101^A^HOSP1");
        StringAssert.Contains(hl7, "ADM-001");
        StringAssert.Contains(hl7, "ORC|NW|PO-001|FO-001||CM");
        StringAssert.Contains(hl7, "OBR|1|PO-001|FO-001|LAB^Blood test");
        StringAssert.Contains(hl7, "DG1|1||R50.9^Fever");
        StringAssert.Contains(hl7, "OBX|1|TX|NOTE");
    }

    [TestMethod]
    public void Generate_WithSiuRequests_ProducesExpectedSiuSegments()
    {
        var generator = new AdtMessageGenerator();

        foreach (string triggerEvent in new[] { "S12", "S13", "S14", "S15" })
        {
            var request = CreateBaseRequest($"SIU {triggerEvent}");

            request.SiuPlacerAppointmentId = "APT-PLACER-01";
            request.SiuFillerAppointmentId = "APT-FILLER-01";
            request.SiuOccurrenceNumber = "1";
            request.SiuAppointmentType = "CONS^Consultation";
            request.SiuAppointmentReason = "Follow-up";
            request.SiuAppointmentStartDateTime = "20260306100000";
            request.SiuAppointmentEndDateTime = "20260306103000";
            request.SiuAppointmentDuration = "30";
            request.SiuAppointmentDurationUnits = "m";
            request.SiuAppointmentStatus = "Scheduled";

            request.SiuGeneralResourceId = "MRI-01";
            request.SiuGeneralResourceName = "MRI Room";
            request.SiuGeneralResourceType = "D";

            request.SiuLocationCode = "RAD01";
            request.SiuLocationDescription = "Radiology";
            request.SiuLocationSite = "Main Campus";

            request.SiuPersonnelId = "DOC-42";
            request.SiuPersonnelFamilyName = "WHO";
            request.SiuPersonnelGivenName = "DOCTOR";
            request.SiuPersonnelRole = "D";

            request.ObxEntries.Add(new ObxEntry { Type = "COMMENT", Reason = "Bring previous reports" });

            string hl7 = generator.Generate(request);

            StringAssert.Contains(hl7, "MSH|");
            StringAssert.Contains(hl7, $"SIU^{triggerEvent}");
            StringAssert.Contains(hl7, "SCH|APT-PLACER-01|APT-FILLER-01");
            StringAssert.Contains(hl7, "PID|1||12345");
            StringAssert.Contains(hl7, "PV1|1|O|CARDIO^101^A^HOSP1");
            StringAssert.Contains(hl7, "RGS|1|A");
            StringAssert.Contains(hl7, "AIG|1|A|MRI-01^MRI Room");
            StringAssert.Contains(hl7, "AIL|1|A|RAD01^Radiology^Main Campus");
            StringAssert.Contains(hl7, "AIP|1|A|DOC-42^WHO^DOCTOR");
            StringAssert.Contains(hl7, "OBX|1|TX|COMMENT");
        }
    }

    private static AdtMessageRequest CreateBaseRequest(string messageTypeCode)
    {
        return new AdtMessageRequest
        {
            MessageTypeCode = messageTypeCode,
            PatientId = "12345",
            PatientFamilyName = "DOE",
            PatientGivenName = "John",
            BirthDate = "19800101",
            Sex = "M",
            Unit = "CARDIO",
            Room = "101",
            Bed = "A",
            Facility = "HOSP1",
            Floor = "1",
            AdmissionNumber = "ADM-001"
        };
    }
}
