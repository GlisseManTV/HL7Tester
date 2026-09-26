namespace HL7Tester.Core.Batch.Models;

/// <summary>
/// Represents a single step in a batch workflow.
/// Each step defines an HL7 message type to generate and send.
/// Location fields (Room, Bed, Unit, Floor) are optional overrides applied to this step.
/// </summary>
public sealed class WorkflowStep
{
    private string _messageType = "ADT A01";
    private bool _isIdentityStep;
    private bool _isLocationStep = true;

    /// <summary>
    /// The HL7 message type code (e.g., "ADT A01", "ADT A02").
    /// </summary>
    public string MessageType
    {
        get => _messageType;
        set
        {
            _messageType = value;
            UpdateStepType();
        }
    }

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
    /// Optional family name override (used for A31 - Update Patient).
    /// If null, the randomized patient name is used.
    /// </summary>
    public string? FamilyName { get; set; }

    /// <summary>
    /// Optional given name override (used for A31 - Update Patient).
    /// If null, the randomized patient name is used.
    /// </summary>
    public string? GivenName { get; set; }

    /// <summary>
    /// True if this step is an identity update (A31), showing name fields instead of location fields.
    /// </summary>
    public bool IsIdentityStep
    {
        get => _isIdentityStep;
        private set => _isIdentityStep = value;
    }

    /// <summary>
    /// True if this step is a location step (not A31), showing location fields.
    /// </summary>
    public bool IsLocationStep
    {
        get => _isLocationStep;
        private set => _isLocationStep = value;
    }

    // ─── Commands (wired by the ViewModel) ──────────────────────────

    public System.Windows.Input.ICommand? MoveUpCommand { get; set; }
    public System.Windows.Input.ICommand? MoveDownCommand { get; set; }
    public System.Windows.Input.ICommand? RemoveCommand { get; set; }

    /// <summary>
    /// Available HL7 message types for the step picker.
    /// </summary>
    public static readonly string[] AvailableMessageTypes =
    {
        "ADT A01 - Inpatient or Day Hospital Admission",
        "ADT A02 - Patient Movement",
        "ADT A03 - Discharge",
        "ADT A08 - Update Patient Stay",
        "ADT A12 - Movement Cancellation",
        "ADT A31 - Update Patient"
    };

    /// <summary>
    /// Creates a copy of this step (for template instantiation).
    /// </summary>
    public WorkflowStep Clone() => new()
    {
        MessageType = MessageType,
        Room = Room,
        Bed = Bed,
        Unit = Unit,
        Floor = Floor,
        FamilyName = FamilyName,
        GivenName = GivenName
    };

    private void UpdateStepType()
    {
        _isIdentityStep = !_messageType.Contains("A31");
        _isLocationStep = !_isIdentityStep;
    }

    public override string ToString() => MessageType;
}
