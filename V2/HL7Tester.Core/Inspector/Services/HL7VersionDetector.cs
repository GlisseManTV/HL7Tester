namespace HL7Tester.Core.Inspector.Services;

/// <summary>
/// Detects the HL7 version from MSH-12 field of a raw HL7 v2 message.
/// </summary>
public static class HL7VersionDetector
{
    private static readonly HashSet<string> KnownVersions = new(StringComparer.OrdinalIgnoreCase)
    {
        "2.1", "2.2", "2.3", "2.3.1", "2.4", "2.5", "2.5.1", "2.6", "2.7", "2.7.1", "2.8"
    };

    /// <summary>
    /// Reads MSH-12 from the raw message to detect the HL7 version.
    /// Returns "2.5" as a fallback if the version cannot be determined.
    /// </summary>
    /// <param name="rawMessage">Raw HL7 message text (may contain MLLP framing).</param>
    public static string Detect(string rawMessage)
    {
        if (string.IsNullOrWhiteSpace(rawMessage))
            return "2.5";

        // Normalize line endings and find the MSH line
        var normalized = rawMessage
            .Replace("\r\n", "\r")
            .Replace("\n", "\r")
            .Trim('\r', '\n', '\u000b', '\u001c');

        var firstLine = normalized.Split('\r')[0];

        if (!firstLine.StartsWith("MSH", StringComparison.OrdinalIgnoreCase))
            return "2.5";

        // MSH-12 is field index 11 (0-based) when splitting by |
        // MSH|^~\&|App|Fac|App|Fac|DT|Sec|MsgType|CtlID|ProcessID|Version|...
        //  0    1    2   3   4   5   6   7     8      9      10       11
        var fields = firstLine.Split('|');
        if (fields.Length <= 11)
            return "2.5";

        var version = fields[11].Trim();

        return KnownVersions.Contains(version) ? version : "2.5";
    }

    /// <summary>
    /// Maps a version string to the NHapi assembly suffix (e.g. "2.3" → "V23").
    /// </summary>
    public static string ToNHapiSuffix(string version) => version switch
    {
        "2.1"   => "V21",
        "2.2"   => "V22",
        "2.3"   => "V23",
        "2.3.1" => "V231",
        "2.4"   => "V24",
        "2.5"   => "V25",
        "2.5.1" => "V251",
        "2.6"   => "V26",
        "2.7"   => "V27",
        "2.7.1" => "V271",
        "2.8"   => "V28",
        _       => "V25"
    };
}
