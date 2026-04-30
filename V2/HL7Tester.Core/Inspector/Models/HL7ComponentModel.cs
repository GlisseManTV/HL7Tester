namespace HL7Tester.Core.Inspector.Models;

/// <summary>
/// Represents a single component within an HL7 field (split by ^).
/// E.g. PV1-3.1 = "Point of Care", PV1-3.8 = "Floor"
/// </summary>
public class HL7ComponentModel
{
    /// <summary>1-based component index (e.g. 1 for PV1-3.1).</summary>
    public int Index { get; set; }

    /// <summary>Parent field index (e.g. 3 for PV1-3.x).</summary>
    public int FieldIndex { get; set; }

    /// <summary>Parent segment identifier (e.g. "PV1").</summary>
    public string SegmentId { get; set; } = string.Empty;

    /// <summary>Human-readable component name from the HL7 knowledge base (e.g. "Point of Care").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>HL7 data type code for this component (e.g. "IS", "ST", "NM").</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>The text value of this component.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Sub-components within this component (split by &).</summary>
    public List<HL7SubComponentModel> SubComponents { get; set; } = new();

    /// <summary>Short notation (e.g. "PV1-3.1").</summary>
    public string Notation => $"{SegmentId}-{FieldIndex}.{Index}";

    /// <summary>
    /// Display label combining notation, name and value.
    /// E.g. "PV1-3.1   Point of Care   =   ROOM_A"
    /// </summary>
    public string DisplayLabel
    {
        get
        {
            var label = Notation;
            if (!string.IsNullOrEmpty(Name)) label += $"   {Name}";
            if (!string.IsNullOrEmpty(DataType)) label += $"  [{DataType}]";
            return label;
        }
    }

    /// <summary>True if this component has sub-component level detail.</summary>
    public bool HasSubComponents => SubComponents.Count > 1;
}
