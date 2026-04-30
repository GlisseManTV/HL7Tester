namespace HL7Tester.Core.Inspector.Models;

/// <summary>
/// Represents a single HL7 segment (e.g. MSH, PID, PV1) within a parsed message.
/// </summary>
public class HL7SegmentModel
{
    /// <summary>Segment identifier (e.g. "MSH", "PID", "PV1").</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable segment name (e.g. "Patient Visit").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The full raw text line of this segment.</summary>
    public string RawLine { get; set; } = string.Empty;

    /// <summary>All non-empty fields in this segment, in field order.</summary>
    public List<HL7FieldModel> Fields { get; set; } = new();

    /// <summary>Display label combining Id and Name (e.g. "PV1 — Patient Visit").</summary>
    public string DisplayLabel => string.IsNullOrEmpty(Name) || Name == Id
        ? Id
        : $"{Id}  —  {Name}";
}
