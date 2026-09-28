namespace HL7Tester.Core.Batch.Models;

/// <summary>
/// Represents the identity of a single patient in a batch workflow.
/// Generated once per patient and shared across all steps.
/// Only name, given name, patient ID, and admission number are randomized.
/// Location (room, bed, unit, floor) is provided by the user globally.
/// </summary>
public sealed class PatientContext
{
    /// <summary>
    /// Unique patient identifier (randomized).
    /// </summary>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>
    /// Patient family name (randomized).
    /// </summary>
    public string FamilyName { get; set; } = string.Empty;

    /// <summary>
    /// Patient given name (randomized).
    /// </summary>
    public string GivenName { get; set; } = string.Empty;

    /// <summary>
    /// Admission number (randomized).
    /// </summary>
    public string AdmissionNumber { get; set; } = string.Empty;

    /// <summary>
    /// Patient sex (M/F).
    /// </summary>
    public string Sex { get; set; } = "M";

    /// <summary>
    /// Patient birth date in HL7 format (yyyyMMdd).
    /// </summary>
    public string BirthDate { get; set; } = string.Empty;

    public override string ToString() => $"{PatientId} ({FamilyName} {GivenName})";
}
