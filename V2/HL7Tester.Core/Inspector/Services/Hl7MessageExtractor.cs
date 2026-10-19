using HL7Tester.Core.Inspector.Models;

namespace HL7Tester.Core.Inspector.Services;

/// <summary>
/// Extracts an <see cref="AdtMessageRequest"/> from a parsed HL7 v2 message
/// (<see cref="ParsedHL7Message"/>), enabling HL7v2 → FHIR conversion through
/// the shared request model consumed by <see cref="Fhir.IFhirResourceGenerator"/>.
///
/// Missing segments/fields simply leave the corresponding request properties at
/// their default values, so partial messages are tolerated.
/// </summary>
public static class Hl7MessageExtractor
{
    /// <summary>
    /// Build an <see cref="AdtMessageRequest"/> from a parsed HL7 v2 message.
    /// </summary>
    public static AdtMessageRequest Extract(ParsedHL7Message parsed)
    {
        ArgumentNullException.ThrowIfNull(parsed);

        var request = new AdtMessageRequest();

        // ── MSH ───────────────────────────────────────────────────────────────
        var msh = FindSegment(parsed, "MSH");
        if (msh != null)
        {
            // MSH-9: message type (CWE) — "ADT^A01" → "ADT A01"
            var family = GetComponentValue(msh, 9, 1);
            var trigger = GetComponentValue(msh, 9, 2);
            if (!string.IsNullOrEmpty(family) && !string.IsNullOrEmpty(trigger))
                request.MessageTypeCode = $"{family} {trigger}";

            // MSH-3..6: routing (HD — first component = namespace ID)
            request.SendingApplication = GetComponentValue(msh, 3, 1) ?? request.SendingApplication;
            request.SendingFacility = GetComponentValue(msh, 4, 1) ?? request.SendingFacility;
            request.ReceivingApplication = GetComponentValue(msh, 5, 1) ?? request.ReceivingApplication;
            request.ReceivingFacility = GetComponentValue(msh, 6, 1) ?? request.ReceivingFacility;
        }

        // ── EVN ───────────────────────────────────────────────────────────────
        var evn = FindSegment(parsed, "EVN");
        if (evn != null)
        {
            // EVN-4 = Event Occurred (TS); fall back to EVN-2 (Recorded DateTime).
            var eventDateTime = GetFieldValue(evn, 4) ?? GetFieldValue(evn, 2);
            if (!string.IsNullOrEmpty(eventDateTime))
                request.EventDateTime = eventDateTime;
        }

        // ── PID ───────────────────────────────────────────────────────────────
        var pid = FindSegment(parsed, "PID");
        if (pid != null)
        {
            // PID-3 (CX): first repetition, first component = patient id
            request.PatientId = GetComponentValue(pid, 3, 1) ?? request.PatientId;

            // PID-5 (XPN): comp1 = family, comp2 = given
            request.PatientFamilyName = GetComponentValue(pid, 5, 1) ?? request.PatientFamilyName;
            request.PatientGivenName = GetComponentValue(pid, 5, 2) ?? request.PatientGivenName;

            // PID-7 (TS): date of birth, kept as-is (yyyyMMdd)
            request.BirthDate = GetFieldValue(pid, 7) ?? request.BirthDate;

            // PID-8 (IS): sex
            request.Sex = GetFieldValue(pid, 8) ?? request.Sex;
        }

        // ── PV1 ───────────────────────────────────────────────────────────────
        var pv1 = FindSegment(parsed, "PV1");
        if (pv1 != null)
        {
            // PV1-3 (PL): comp1=Unit(PointOfCare), 2=Room, 3=Bed, 4=Facility, 8=Floor
            request.Unit = GetComponentValue(pv1, 3, 1) ?? request.Unit;
            request.Room = GetComponentValue(pv1, 3, 2) ?? request.Room;
            request.Bed = GetComponentValue(pv1, 3, 3) ?? request.Bed;
            request.Facility = GetComponentValue(pv1, 3, 4) ?? request.Facility;
            request.Floor = GetComponentValue(pv1, 3, 8) ?? request.Floor;

            // PV1-19 (CX): visit number → AdmissionNumber
            request.AdmissionNumber = GetComponentValue(pv1, 19, 1) ?? request.AdmissionNumber;
        }

        // ── MRG (merge) ───────────────────────────────────────────────────────
        var mrg = FindSegment(parsed, "MRG");
        if (mrg != null)
        {
            // MRG-1 (CX): prior patient id — for A18/A40 the merged (source) id.
            var priorId = GetComponentValue(mrg, 1, 1);
            if (!string.IsNullOrEmpty(priorId))
                request.PatientId = priorId;
        }

        // ── ORM (ORC / OBR) ───────────────────────────────────────────────────
        var orc = FindSegment(parsed, "ORC");
        if (orc != null)
        {
            request.OrderControl = GetFieldValue(orc, 1) ?? request.OrderControl;
            request.OrmPlacerOrderNumber = GetComponentValue(orc, 2, 1) ?? request.OrmPlacerOrderNumber;
            request.OrmFillerOrderNumber = GetComponentValue(orc, 3, 1) ?? request.OrmFillerOrderNumber;

            // Ordering provider (XCN): id ^ family ^ given
            var providerId = GetComponentValue(orc, 12, 1);
            if (!string.IsNullOrEmpty(providerId))
            {
                request.OrmOrderingProviderId = providerId;
                request.OrmOrderingProviderFamilyName = GetComponentValue(orc, 12, 2) ?? string.Empty;
                request.OrmOrderingProviderGivenName = GetComponentValue(orc, 12, 3) ?? string.Empty;
            }
        }

        var obr = FindSegment(parsed, "OBR");
        if (obr != null)
        {
            // OBR-4 (CWE): raw value kept as "CODE^Display"
            request.OrmUniversalServiceId = GetFieldValue(obr, 4) ?? request.OrmUniversalServiceId;
        }

        // ── SIU (SCH) ─────────────────────────────────────────────────────────
        var sch = FindSegment(parsed, "SCH");
        if (sch != null)
        {
            request.SiuPlacerAppointmentId = GetComponentValue(sch, 1, 1) ?? request.SiuPlacerAppointmentId;
            request.SiuFillerAppointmentId = GetComponentValue(sch, 2, 1) ?? request.SiuFillerAppointmentId;
            request.SiuOccurrenceNumber = GetFieldValue(sch, 5) ?? request.SiuOccurrenceNumber;
            request.SiuAppointmentType = GetFieldValue(sch, 6) ?? request.SiuAppointmentType;
            request.SiuAppointmentReason = GetFieldValue(sch, 7) ?? request.SiuAppointmentReason;
            request.SiuAppointmentDuration = GetFieldValue(sch, 9) ?? request.SiuAppointmentDuration;
            request.SiuAppointmentDurationUnits = GetFieldValue(sch, 10) ?? request.SiuAppointmentDurationUnits;

            // SCH-11 (TQ timing): comp3=duration, comp4=start, comp5=end
            request.SiuAppointmentStartDateTime = GetComponentValue(sch, 11, 4) ?? request.SiuAppointmentStartDateTime;
            request.SiuAppointmentEndDateTime = GetComponentValue(sch, 11, 5) ?? request.SiuAppointmentEndDateTime;

            request.SiuAppointmentStatus = GetFieldValue(sch, 22) ?? request.SiuAppointmentStatus;

            // SCH-17 (XCN): personnel id ^ family ^ given
            var personnelId = GetComponentValue(sch, 17, 1);
            if (!string.IsNullOrEmpty(personnelId))
            {
                request.SiuPersonnelId = personnelId;
                request.SiuPersonnelFamilyName = GetComponentValue(sch, 17, 2) ?? string.Empty;
                request.SiuPersonnelGivenName = GetComponentValue(sch, 17, 3) ?? string.Empty;
            }
        }

        // ── OBX ───────────────────────────────────────────────────────────────
        foreach (var obx in parsed.Segments)
        {
            if (!string.Equals(obx.Id, "OBX", StringComparison.OrdinalIgnoreCase))
                continue;

            var obsId = GetFieldValue(obx, 3);
            var obsValue = GetFieldValue(obx, 5);

            if (!string.IsNullOrEmpty(obsId) || !string.IsNullOrEmpty(obsValue))
            {
                request.ObxEntries.Add(new ObxEntry
                {
                    Type = obsId,
                    Reason = obsValue
                });
            }
        }

        return request;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static HL7SegmentModel? FindSegment(ParsedHL7Message parsed, string segmentId)
        => parsed.Segments.FirstOrDefault(
            s => string.Equals(s.Id, segmentId, StringComparison.OrdinalIgnoreCase));

    /// <summary>First repetition raw value of a field, or null when absent/empty.</summary>
    private static string? GetFieldValue(HL7SegmentModel segment, int fieldIndex)
    {
        var field = segment.Fields.FirstOrDefault(f => f.Index == fieldIndex);
        if (field is null || string.IsNullOrEmpty(field.RawValue))
            return null;

        return field.Repetitions.Count > 0 ? field.Repetitions[0].RawValue : field.RawValue;
    }

    /// <summary>First repetition, component N decoded value, or null when absent/empty.</summary>
    private static string? GetComponentValue(HL7SegmentModel segment, int fieldIndex, int componentIndex)
    {
        var field = segment.Fields.FirstOrDefault(f => f.Index == fieldIndex);
        if (field is null || field.Repetitions.Count == 0)
            return null;

        var component = field.Repetitions[0].Components
            .FirstOrDefault(c => c.Index == componentIndex);

        return string.IsNullOrEmpty(component?.Value) ? null : component!.Value;
    }
}
