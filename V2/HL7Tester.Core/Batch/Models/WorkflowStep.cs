namespace HL7Tester.Core.Batch.Models;

/// <summary>
/// Represents a single step in a batch workflow.
/// Each step defines an HL7 message type to generate and send.
/// Location fields (Room, Bed, Unit, Floor) are optional overrides applied to this step.
/// </summary>
public sealed class WorkflowStep
{
    /// <summary>
    /// The HL7 message type code (e.g., "ADT A01", "ADT A02").
    /// </summary>
    public string MessageType { get; set; } = "ADT A01";

    /// <summary>
    /// Optional room override for this step. If null, the global location is used.
    /// </summary>
    public string? Room { get; set; }

    /// <summary>
    /// Optional bed override for this step. If null, the global location is used.
    /// </summary>
    public string? Bed { get; set; }

    /// <summary>
    /// Optional unit override for this step. If null, the global location is used.
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Optional floor override for this step. If null, the global location is used.
    /// </summary>
    public string? Floor { get; set; }

    /// <summary>
    /// Creates a copy of this step (for template instantiation).
    /// </summary>
    public WorkflowStep Clone() => new()
    {
        MessageType = MessageType,
        Room = Room,
        Bed = Bed,
        Unit = Unit,
        Floor = Floor
    };

    public override string ToString() => MessageType;
}
