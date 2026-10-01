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
    private bool _isMergeStep;

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
    /// Optional new patient ID (used for A18/A40 - Patient Record Merge).
    /// If null, the existing PatientId is used as the merge target.
    /// </summary>
    public string? NewPatientId { get; set; }

    /// <summary>
    /// True if this step is an identity update (A31), showing name fields instead of location fields.
    /// </summary>
    public bool IsIdentityStep
    {
        get => _isIdentityStep;
        private set => _isIdentityStep = value;
    }

    /// <summary>
    /// True if this step is a location step (not A31 and not A18/A40), showing location fields.
    /// </summary>
    public bool IsLocationStep
    {
        get => _isLocationStep;
        private set => _isLocationStep = value;
    }

    /// <summary>
    /// True if this step is a merge step (A18/A40), showing the NewPatientId field.
    /// </summary>
    public bool IsMergeStep
    {
        get => _isMergeStep;
        private set => _isMergeStep = value;
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
        "ADT A04 - Outpatient Admission",
        "ADT A05 - Pre-admission",
        "ADT A06 - Transformation of an Outpatient Visit into Admission",
        "ADT A07 - Transformation of an Admission into Outpatient Visit",
        "ADT A08 - Update Patient Stay",
        "ADT A09 - Temporary Movement",
        "ADT A10 - Return from Temporary Movement",
        "ADT A11 - Admission Cancellation",
        "ADT A12 - Movement Cancellation",
        "ADT A13 - Discharge Cancellation",
        "ADT A14 - Scheduled Admission in the Future (not used)",
        "ADT A15 - Scheduled Movement in the Future (not used)",
        "ADT A16 - Scheduled Discharge in the Future (not used)",
        "ADT A18 - Merge Patient Records",
        "ADT A21 - Leave of Absence Departure",
        "ADT A22 - Return from Leave of Absence",
        "ADT A24 - Link between Two Patients (not used)",
        "ADT A25 - Cancellation of Future Scheduled Admission (not used)",
        "ADT A26 - Cancellation of Future Scheduled Movement (not used)",
        "ADT A27 - Cancellation of Future Scheduled Discharge (not used)",
        "ADT A28 - Patient Creation",
        "ADT A31 - Update Patient",
        "ADT A32 - Cancellation of a Return from Temporary Movement",
        "ADT A33 - Cancellation of Temporary Movement",
        "ADT A37 - Cancellation of a Patient Link (not used)",
        "ADT A38 - Pre-admission Cancellation",
        "ADT A40 - Patient Record Merge"
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
        GivenName = GivenName,
        NewPatientId = NewPatientId
    };

    private void UpdateStepType()
    {
        _isIdentityStep = _messageType.Contains("A31");
        _isMergeStep = _messageType.Contains("A18") || _messageType.Contains("A40");
        _isLocationStep = !_isIdentityStep && !_isMergeStep;
    }

    public override string ToString() => MessageType;
}
