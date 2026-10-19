using Hl7.Fhir.Model;

namespace HL7Tester.Core.Fhir;

/// <summary>
/// Centralized HL7 v2 -> FHIR R4 vocabulary mappings (design doc section 2.5).
/// All lookups are case-insensitive; unknown values return null so the caller
/// can decide on a sensible default.
/// </summary>
public static class FhirTerminologyMapper
{
    /// <summary>Canonical FHIR R4 code system for Encounter.class (v3 ActCode).</summary>
    public const string ActCodeSystem = "http://terminology.hl7.org/CodeSystem/v3-ActCode";

    /// <summary>Canonical FHIR R4 code system for Appointment.participant.type (v3 ParticipationType).</summary>
    public const string ParticipationTypeSystem = "http://terminology.hl7.org/CodeSystem/v3-ParticipationType";

    /// <summary>
    /// HL7 PID-8 (sex) -> FHIR AdministrativeGender.
    /// M -> Male, F -> Female, O -> Other, U -> Unknown. Anything else -> null.
    /// </summary>
    public static AdministrativeGender? MapSex(string? sex) => sex?.Trim().ToUpperInvariant() switch
    {
        "M" => AdministrativeGender.Male,
        "F" => AdministrativeGender.Female,
        "O" => AdministrativeGender.Other,
        "U" => AdministrativeGender.Unknown,
        _ => null
    };

    /// <summary>
    /// HL7 PV1-2 (patient class) -> Encounter.class Coding.
    /// The app always sends "I" for ADT (inpatient); "O" outpatient and "E" emergency
    /// are mapped as well for completeness. Unknown/empty defaults to inpatient.
    /// </summary>
    public static Coding MapEncounterClass(string? patientClass) => patientClass?.Trim().ToUpperInvariant() switch
    {
        "O" => new Coding(ActCodeSystem, "AMB", "outpatient"),
        "E" => new Coding(ActCodeSystem, "EMER", "emergency"),
        _ => new Coding(ActCodeSystem, "IMP", "inpatient")
    };

    /// <summary>
    /// ORM order status (ORC-5 display value used by the form) -> ServiceRequest.status
    /// (Firely SDK type: Hl7.Fhir.Model.RequestStatus).
    /// Note: FHIR R4 ServiceRequest has no "final" status; HL7 "Final"/"FM" (finished)
    /// maps to Completed, and cancellation maps to Revoked (R4 equivalent of cancelled).
    /// </summary>
    public static RequestStatus? MapServiceRequestStatus(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "draft" or "dr" => RequestStatus.Draft,
        "active" or "ca" or "ip" or "or" => RequestStatus.Active,
        "onhold" or "on-hold" or "on hold" or "hd" => RequestStatus.OnHold,
        "final" or "fm" or "complete" or "completed" => RequestStatus.Completed,
        "cancelled" or "canceled" or "x" or "revoked" => RequestStatus.Revoked,
        "unknown" => RequestStatus.Unknown,
        _ => null
    };

    /// <summary>
    /// SIU appointment status (SCH-24 display value used by the form) -> Appointment.status.
    /// Note: FHIR R4 Appointment has no "scheduled" status; the app default "Scheduled"
    /// maps to Booked (design doc section 2.5 vocabulary).
    /// </summary>
    public static Appointment.AppointmentStatus? MapAppointmentStatus(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "proposed" => Appointment.AppointmentStatus.Proposed,
        "pending" => Appointment.AppointmentStatus.Pending,
        "scheduled" or "booked" => Appointment.AppointmentStatus.Booked,
        "arrived" => Appointment.AppointmentStatus.Arrived,
        "fulfilled" => Appointment.AppointmentStatus.Fulfilled,
        "cancelled" or "canceled" => Appointment.AppointmentStatus.Cancelled,
        _ => null
    };
}
