namespace HL7Tester.Core.Inspector.Models;

/// <summary>
/// Represents a single field within an HL7 segment (e.g. PV1-3).
/// A field may have multiple repetitions (separated by ~).
/// </summary>
public class HL7FieldModel
{
    /// <summary>1-based field index within the segment (e.g. 3 for PV1-3).</summary>
    public int Index { get; set; }

    /// <summary>Parent segment identifier (e.g. "PV1").</summary>
    public string SegmentId { get; set; } = string.Empty;

    /// <summary>Human-readable field name from the HL7 knowledge base (e.g. "Assigned Patient Location").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>HL7 data type code (e.g. "PL", "CWE", "IS", "ST").</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Whether this field is required per the HL7 specification.</summary>
    public bool Required { get; set; }

    /// <summary>Maximum number of repetitions allowed (0 = unlimited).</summary>
    public int MaxRepetitions { get; set; }

    /// <summary>The raw field value as it appears in the message (may contain ^ and &).</summary>
    public string RawValue { get; set; } = string.Empty;

    /// <summary>
    /// All repetitions of this field (separated by ~).
    /// Most fields have a single repetition.
    /// </summary>
    public List<HL7RepetitionModel> Repetitions { get; set; } = new();

    /// <summary>Short notation for this field (e.g. "PV1-3").</summary>
    public string Notation => $"{SegmentId}-{Index}";

    /// <summary>
    /// Display label combining notation, name and data type.
    /// E.g. "PV1-3   Assigned Patient Location   [PL]"
    /// </summary>
    public string DisplayLabel
    {
        get
        {
            var label = Notation;
            if (!string.IsNullOrEmpty(Name)) label += $"   {Name}";
            if (!string.IsNullOrEmpty(DataType)) label += $"   [{DataType}]";
            return label;
        }
    }

    /// <summary>True if this field has component-level detail to display.</summary>
    public bool HasComponents => Repetitions.Count > 0 && Repetitions[0].Components.Count > 1;
}
