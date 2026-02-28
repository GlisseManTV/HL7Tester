using HL7Tester.Core;

namespace HL7Tester.Tests;

[TestClass]
public sealed class AdtMessageGeneratorTests
{
    [TestMethod]
    public void Generate_WithBasicA01Request_ProducesMessageWithExpectedTypeAndPid()
    {
        // Arrange
        var generator = new AdtMessageGenerator();
        var request = new AdtMessageRequest
        {
            MessageTypeCode = "ADT A01",
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

        request.ObxEntries.Add(new ObxEntry { Type = "TYPE1", Reason = "Reason1" });
        request.ObxEntries.Add(new ObxEntry { Type = "TYPE2", Reason = "Reason2" });

        // Act
        string hl7 = generator.Generate(request);

        // Assert (vérifications simples sur la chaîne HL7)
        StringAssert.Contains(hl7, "MSH|");
        StringAssert.Contains(hl7, "ADT^A01");
        StringAssert.Contains(hl7, "PID|1||12345");
        StringAssert.Contains(hl7, "OBX|1|TX|TYPE1");
        StringAssert.Contains(hl7, "OBX|2|TX|TYPE2");
    }
}
