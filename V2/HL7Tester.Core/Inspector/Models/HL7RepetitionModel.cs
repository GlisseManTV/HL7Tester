namespace HL7Tester.Core.Inspector.Models;

/// <summary>
/// Represents one repetition of an HL7 field (fields can repeat using the ~ separator).
/// Each repetition contains one or more components.
/// </summary>
public class HL7RepetitionModel
{
    /// <summary>1-based repetition index (most fields have only repetition 1).</summary>
    public int Index { get; set; }

    /// <summary>The raw value of this repetition (may contain ^).</summary>
    public string RawValue { get; set; } = string.Empty;

    /// <summary>Components within this repetition (split by ^).</summary>
    public List<HL7ComponentModel> Components { get; set; } = new();

    /// <summary>True if more than one component is present.</summary>
    public bool HasComponents => Components.Count > 1;
}
