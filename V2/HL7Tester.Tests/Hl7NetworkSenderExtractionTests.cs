using HL7Tester.Core;

namespace HL7Tester.Tests;

[TestClass]
public sealed class Hl7NetworkSenderExtractionTests
{
    // Exact MSH line reported by the user (regression proof for the off-by-one bug).
    private const string UserMshLine =
        @"MSH|^~\&|HL7Tester|gaetan|||20261019202406||ADT^A01|a030c74e506f6f19782|P|2.3||||||ASCII";

    [TestMethod]
    public void ExtractMessageCode_ReturnsOnlyMsh9_NotMsh9PlusMsh10()
    {
        var messageCode = Hl7NetworkSender.ExtractMessageCode(UserMshLine);

        Assert.AreEqual("ADT^A01", messageCode);
        Assert.DoesNotContain('|', messageCode!, "MSH-9 extraction must not include the following field.");
    }

    [TestMethod]
    public void ExtractControlId_ReturnsOnlyMsh10_NotMsh10PlusMsh11()
    {
        var controlId = Hl7NetworkSender.ExtractControlId(UserMshLine);

        Assert.AreEqual("a030c74e506f6f19782", controlId);
        Assert.DoesNotContain('|', controlId!, "MSH-10 extraction must not include the following field.");
        Assert.AreNotEqual("P", controlId, "MSH-10 extraction must not return MSH-11 (processing ID).");
    }

    [TestMethod]
    public void Extractors_WorkOnMultiLineMessage_WithCrlfLineEndings()
    {
        var message =
            "MSH|^~\\&|HL7Tester|gaetan|||20261019202406||ADT^A01|a030c74e506f6f19782|P|2.3||||||ASCII\r\n" +
            "EVN|A01|20261019202406\r\n" +
            "PID|1||PAT001^^^HAPI||Doe^John\r\n";

        var messageCode = Hl7NetworkSender.ExtractMessageCode(message);
        var controlId = Hl7NetworkSender.ExtractControlId(message);

        Assert.AreEqual("ADT^A01", messageCode);
        Assert.AreEqual("a030c74e506f6f19782", controlId);
    }

    [TestMethod]
    public void Extractors_ReturnNull_WhenMshLineIsMissing()
    {
        var message = "EVN|A01|20261019202406\r\nPID|1||PAT001";

        Assert.IsNull(Hl7NetworkSender.ExtractMessageCode(message));
        Assert.IsNull(Hl7NetworkSender.ExtractControlId(message));
    }

    [TestMethod]
    public void Extractors_ReturnNull_WhenFieldsAreMissing()
    {
        // Truncated MSH line: no MSH-9/MSH-10 fields present.
        var message = "MSH|^~\\&|HL7Tester|gaetan";

        Assert.IsNull(Hl7NetworkSender.ExtractMessageCode(message));
        Assert.IsNull(Hl7NetworkSender.ExtractControlId(message));
    }
}
