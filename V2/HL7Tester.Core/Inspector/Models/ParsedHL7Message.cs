namespace HL7Tester.Core.Inspector.Models;

/// <summary>
/// Represents a fully parsed HL7 v2 message with its segment hierarchy.
/// </summary>
public class ParsedHL7Message
{
    /// <summary>The detected HL7 version (e.g. "2.3", "2.5").</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>The original raw HL7 message text.</summary>
    public string RawMessage { get; set; } = string.Empty;

    /// <summary>All parsed segments in message order.</summary>
    public List<HL7SegmentModel> Segments { get; set; } = new();

    /// <summary>Error message if parsing failed, empty if successful.</summary>
    public string ParseError { get; set; } = string.Empty;

    /// <summary>Whether the message was parsed successfully.</summary>
    public bool IsSuccess => string.IsNullOrEmpty(ParseError);
}