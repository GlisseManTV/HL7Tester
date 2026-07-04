using HL7Tester.Core.Inspector.Services;

namespace HL7Tester.Tests;

[TestClass]
public sealed class XmlHl7ParserServiceTests
{
    [TestMethod]
    public void Parse_WithUnescapedAmpersandInMsh2_ConvertsToStandardHl7()
    {
        var parser = new XmlHl7ParserService();
        const string xml = """
            <HL7Message>
              <MSH>
                <MSH.2>^~\&</MSH.2>
                <MSH.3>SENDER</MSH.3>
                <MSH.4>FACILITY</MSH.4>
                <MSH.9>ADT^A01</MSH.9>
                <MSH.12>2.3</MSH.12>
              </MSH>
              <PID>
                <PID.3>12345</PID.3>
              </PID>
            </HL7Message>
            """;

        string? hl7 = parser.Parse(xml);

        Assert.IsNotNull(hl7);
        StringAssert.Contains(hl7, "MSH|^~\\&|SENDER|FACILITY");
        StringAssert.Contains(hl7, "PID|||12345");
    }

    [TestMethod]
    public void Parse_WithEscapedAmpersandInMsh2_PreservesSingleAmpersandInStandardHl7()
    {
        var parser = new XmlHl7ParserService();
        const string xml = """
            <HL7Message>
              <MSH>
                <MSH.2>^~\&amp;</MSH.2>
                <MSH.3>SENDER</MSH.3>
                <MSH.12>2.3</MSH.12>
              </MSH>
            </HL7Message>
            """;

        string? hl7 = parser.Parse(xml);

        Assert.IsNotNull(hl7);
        StringAssert.Contains(hl7, "^~\\&");
        Assert.IsFalse(hl7.Contains("&amp;", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Parse_WithOtherValidXmlEntities_PreservesDecodedValues()
    {
        var parser = new XmlHl7ParserService();
        const string xml = """
            <HL7Message>
              <MSH>
                <MSH.2>^~\&amp;</MSH.2>
                <MSH.12>2.3</MSH.12>
              </MSH>
              <PID>
                <PID.5>DOE&lt;TEST&gt;^JOHN</PID.5>
              </PID>
            </HL7Message>
            """;

        string? hl7 = parser.Parse(xml);

        Assert.IsNotNull(hl7);
        StringAssert.Contains(hl7, "DOE<TEST>^JOHN");
    }

    [TestMethod]
    public void Parse_WithExplicitMsh1_DoesNotAddExtraLeadingFields()
    {
        var parser = new XmlHl7ParserService();
        const string xml = """
            <HL7Message>
              <MSH>
                <MSH.1>|</MSH.1>
                <MSH.2> ^~\&</MSH.2>
                <MSH.3></MSH.3>
              </MSH>
            </HL7Message>
            """;

        string? hl7 = parser.Parse(xml);

        Assert.AreEqual("MSH|^~\\&|", hl7);
    }

    [TestMethod]
    public void Parse_WithPLTypeData_PreservesAllComponentIndices()
    {
        var parser = new XmlHl7ParserService();
        const string xml = """
            <HL7Message>
              <PV1>
                <PV1.3>
                  <PL.1>PUR3</PL.1>
                  <PL.9>PLANTA 3 D'URG.</PL.9>
                </PV1.3>
                <PV1.4>EN</PV1.4>
              </PV1>
            </HL7Message>
            """;

        string? hl7 = parser.Parse(xml);

        Assert.IsNotNull(hl7);
        // PV1.3 should have 9 components: PUR3^^^^^^^^PLANTA 3 D'URG.
        StringAssert.Contains(hl7, "PUR3^^^^^^^^PLANTA 3 D'URG.");
        // PV1.4 should be a separate field, not contain the PL.9 value
        var parts = hl7.Split('|');
        Assert.AreEqual("PV1", parts[0]);
        Assert.AreEqual("", parts[1]); // PV1.1
        Assert.AreEqual("", parts[2]); // PV1.2
        Assert.AreEqual("PUR3^^^^^^^^PLANTA 3 D'URG.", parts[3]); // PV1.3
        Assert.AreEqual("EN", parts[4]); // PV1.4
    }

    [TestMethod]
    public void Parse_WithCXTypeData_PreservesAllComponentIndices()
    {
        var parser = new XmlHl7ParserService();
        const string xml = """
            <HL7Message>
              <PID>
                <PID.3>
                  <CX.1>12345</CX.1>
                  <CX.4>
                    <HD.1>HIS</HD.1>
                  </CX.4>
                  <CX.5>PI</CX.5>
                </PID.3>
              </PID>
            </HL7Message>
            """;

        string? hl7 = parser.Parse(xml);

        Assert.IsNotNull(hl7);
        // CX components use ^ separator: 12345^^^HIS^^^^PI
        StringAssert.Contains(hl7, "12345^^^HIS^^^^PI");
    }
}
