namespace HL7Tester.Core.Inspector.Models;

/// <summary>
/// Represents a sub-component within an HL7 component (split by &).
/// Sub-components are the deepest level of HL7 v2 data hierarchy.
/// E.g. PV1-3.1.2 (the second sub-component of PV1-3.1)
/// </summary>
public class HL7SubComponentModel
{
    /// <summary>1-based sub-component index.</summary>
    public int Index { get; set; }

    /// <summary>Parent component index.</summary>
    public int ComponentIndex { get; set; }

    /// <summary>Parent field index.</summary>
    public int FieldIndex { get; set; }

    /// <summary>Parent segment identifier.</summary>
    public string SegmentId { get; set; } = string.Empty;

    /// <summary>The text value of this sub-component.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Short notation (e.g. "PV1-3.1.2").</summary>
    public string Notation => $"{SegmentId}-{FieldIndex}.{ComponentIndex}.{Index}";
}
