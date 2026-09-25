using System.Collections.ObjectModel;

namespace HL7Tester.Core.Batch.Models;

/// <summary>
/// Represents a complete batch workflow configuration.
/// Defines the sequence of steps, number of patients, and global delay between messages.
/// </summary>
public sealed class BatchWorkflow
{
    /// <summary>
    /// The ordered list of steps in the workflow (2 to 6 steps).
    /// </summary>
    public ObservableCollection<WorkflowStep> Steps { get; } = new();

    /// <summary>
    /// Number of patients to send in parallel.
    /// </summary>
    public int PatientCount { get; set; } = 10;

    /// <summary>
    /// Global delay in milliseconds between ALL messages (across all patients).
    /// </summary>
    public int GlobalDelayMs { get; set; } = 500;

    /// <summary>
    /// Global room applied to all steps (unless overridden per step).
    /// </summary>
    public string Room { get; set; } = string.Empty;

    /// <summary>
    /// Global bed applied to all steps (unless overridden per step).
    /// </summary>
    public string Bed { get; set; } = string.Empty;

    /// <summary>
    /// Global unit applied to all steps (unless overridden per step).
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Global floor applied to all steps (unless overridden per step).
    /// </summary>
    public string Floor { get; set; } = string.Empty;

    /// <summary>
    /// Validates the workflow configuration.
    /// </summary>
    public bool IsValid(out string error)
    {
        if (Steps.Count < 2)
        {
            error = "Workflow must have at least 2 steps.";
            return false;
        }

        if (Steps.Count > 6)
        {
            error = "Workflow cannot have more than 6 steps.";
            return false;
        }

        if (PatientCount < 1)
        {
            error = "Patient count must be at least 1.";
            return false;
        }

        if (GlobalDelayMs < 0)
        {
            error = "Delay must be 0 or more.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
