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
    [DataRow("2.3", "CTRL23", "orm-o01-v23-minimal-nw.hl7")]
    [DataRow("2.5.1", "CTRL251", "orm-o01-v251-minimal-nw.hl7")]
    public void Generate_WithMinimalOrmO01Request_MatchesVersionedGoldenSample(
        string hl7Version,
        string controlId,
        string fixtureName)
    {
        var generator = new AdtMessageGenerator();
        var request = CreateBaseOrmRequest(hl7Version, controlId);

        string hl7 = generator.Generate(request);

        Assert.AreEqual(LoadFixture(fixtureName), NormalizeHl7(hl7));
        AssertSegmentOrder(hl7, "MSH", "PID", "ORC", "OBR");
        AssertField(hl7, "MSH", 9, "ORM^O01");
        AssertField(hl7, "MSH", 12, hl7Version);
        AssertField(hl7, "ORC", 1, "NW");
        AssertField(hl7, "ORC", 2, "PO-001");
        AssertField(hl7, "OBR", 4, "GENORDER^General Order^L");
        Assert.IsFalse(GetSegmentIds(hl7).Contains("OBX"));
    }

    [TestMethod]
    [DataRow("2.3", "CTRL23OPT", "orm-o01-v23-with-pv1-dg1-nte.hl7")]
    [DataRow("2.5.1", "CTRL251OPT", "orm-o01-v251-with-pv1-dg1-nte.hl7")]
    public void Generate_WithOptionalOrmSegments_MatchesVersionedGoldenSample(
        string hl7Version,
        string controlId,
        string fixtureName)
    {
        var generator = new AdtMessageGenerator();
        var request = CreateBaseOrmRequest(hl7Version, controlId);

        request.OrmIncludePv1 = true;
        request.OrmDiagnosisCode = "R50.9";
        request.OrmDiagnosisText = "Fever";
        request.OrmDiagnosisCodingSystem = "I10";
        request.OrmOrderNote = "Bring previous reports";

        string hl7 = generator.Generate(request);

        Assert.AreEqual(LoadFixture(fixtureName), NormalizeHl7(hl7));
        AssertSegmentOrder(hl7, "MSH", "PID", "PV1", "ORC", "OBR", "DG1", "NTE");
        AssertField(hl7, "PV1", 2, "O");
        AssertField(hl7, "DG1", 3, "R50.9^Fever^I10");
        AssertField(hl7, "NTE", 3, "Bring previous reports");
        Assert.IsFalse(GetSegmentIds(hl7).Contains("OBX"));
    }

    [TestMethod]
    [DataRow("NW")]
    [DataRow("CA")]
    public void Generate_WithSupportedOrmAction_PutsActionInOrc1(string orderControl)
    {
        var generator = new AdtMessageGenerator();
        var request = CreateBaseOrmRequest("2.5.1", $"CTRL-{orderControl}");
        request.OrderControl = orderControl;

        string hl7 = generator.Generate(request);

        AssertField(hl7, "ORC", 1, orderControl);
    }

    [TestMethod]
    public void Generate_WithUnsupportedOrmAction_Throws()
    {
        var generator = new AdtMessageGenerator();
        var request = CreateBaseOrmRequest("2.5.1", "CTRL-BAD");
        request.OrderControl = "XO";

        try
        {
            generator.Generate(request);
            Assert.Fail("Expected unsupported ORM order control to throw NotSupportedException.");
        }
        catch (NotSupportedException)
        {
        }
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

    private static AdtMessageRequest CreateBaseOrmRequest(string hl7Version, string controlId)
    {
        var request = CreateBaseRequest("ORM O01");
        request.MessageDateTime = "20260102030405";
        request.MessageControlId = controlId;
        request.SendingFacility = "TESTFAC";
        request.OrmHl7Version = hl7Version;
        request.OrderControl = "NW";
        request.OrmPlacerOrderNumber = "PO-001";
        request.OrmFillerOrderNumber = string.Empty;
        request.OrmOrderStatus = "SC";
        request.OrmUniversalServiceCode = "GENORDER";
        request.OrmUniversalServiceText = "General Order";
        request.OrmUniversalServiceCodingSystem = "L";
        request.OrmRequestedDateTime = "20260102030405";
        request.OrmOrderingProviderId = "PROV001";
        request.OrmOrderingProviderFamilyName = "ORDERING";
        request.OrmOrderingProviderGivenName = "PROVIDER";
        request.PatientId = "PAT001";
        request.PatientFamilyName = "TEST";
        request.PatientGivenName = "PATIENT";
        request.BirthDate = "19800101";
        request.Sex = "U";

        return request;
    }

    private static string LoadFixture(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        return NormalizeHl7(File.ReadAllText(path));
    }

    private static string NormalizeHl7(string hl7)
        => hl7.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd();

    private static IReadOnlyList<string> GetSegmentIds(string hl7)
        => NormalizeHl7(hl7)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split('|')[0])
            .ToArray();

    private static void AssertSegmentOrder(string hl7, params string[] expectedSegments)
    {
        CollectionAssert.AreEqual(expectedSegments, GetSegmentIds(hl7).ToArray());
    }

    private static void AssertField(string hl7, string segmentId, int fieldNumber, string expectedValue)
    {
        string? line = NormalizeHl7(hl7)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(value => value.StartsWith(segmentId + "|", StringComparison.Ordinal));

        Assert.IsNotNull(line, $"Segment {segmentId} was not found.");

        string[] fields = line.Split('|');
        int index = segmentId == "MSH" ? fieldNumber - 1 : fieldNumber;

        Assert.IsTrue(fields.Length > index, $"{segmentId}-{fieldNumber} was not present.");
        Assert.AreEqual(expectedValue, fields[index]);
    }
}
