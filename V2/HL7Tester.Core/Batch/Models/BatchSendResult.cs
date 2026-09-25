namespace HL7Tester.Core.Batch.Models;

/// <summary>
/// Represents the result of a single patient's batch run.
/// </summary>
public sealed class PatientBatchResult
{
    /// <summary>
    /// The patient context (ID, name, admission).
    /// </summary>
    public PatientContext Patient { get; set; } = new();

    /// <summary>
    /// Total number of steps in the workflow.
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Number of steps successfully sent.
    /// </summary>
    public int SuccessfulSteps { get; set; }

    /// <summary>
    /// Whether all steps completed successfully.
    /// </summary>
    public bool Success => SuccessfulSteps == TotalSteps;

    /// <summary>
    /// Error message if the run failed, null otherwise.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// The step index (1-based) where the failure occurred, or 0 if no failure.
    /// </summary>
    public int FailedStepIndex { get; set; }

    public override string ToString()
    {
        if (Success)
            return $"✓ {Patient.PatientId} (Adm: {Patient.AdmissionNumber}) → {TotalSteps}/{TotalSteps} steps OK";
        return $"✗ {Patient.PatientId} (Adm: {Patient.AdmissionNumber}) → {SuccessfulSteps}/{TotalSteps} (step {FailedStepIndex} failed)";
    }
}

/// <summary>
/// Represents the final result of a complete batch send operation.
/// </summary>
public sealed class BatchSendResult
{
    /// <summary>
    /// All patient results.
    /// </summary>
    public List<PatientBatchResult> PatientResults { get; } = new();

    /// <summary>
    /// Total number of patients attempted.
    /// </summary>
    public int TotalPatients => PatientResults.Count;

    /// <summary>
    /// Number of patients that completed all steps successfully.
    /// </summary>
    public int SuccessfulPatients => PatientResults.Count(r => r.Success);

    /// <summary>
    /// Number of patients that had at least one failure.
    /// </summary>
    public int FailedPatients => TotalPatients - SuccessfulPatients;

    /// <summary>
    /// Total messages sent successfully.
    /// </summary>
    public int TotalMessagesSent => PatientResults.Sum(r => r.SuccessfulSteps);

    /// <summary>
    /// Total messages attempted.
    /// </summary>
    public int TotalMessagesAttempted => PatientResults.Sum(r => r.TotalSteps);

    /// <summary>
    /// Whether the batch was stopped by the user (cancellation).
    /// </summary>
    public bool WasCancelled { get; set; }

    /// <summary>
    /// Elapsed time for the entire batch run.
    /// </summary>
    public TimeSpan Elapsed { get; set; }
}
