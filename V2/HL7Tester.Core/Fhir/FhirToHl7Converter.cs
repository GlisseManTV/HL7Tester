using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace HL7Tester.Core.Fhir;

/// <summary>
/// Reverse converter: FHIR R4 JSON (single resource or transaction Bundle) →
/// <see cref="AdtMessageRequest"/>, which can then be fed to
/// <see cref="AdtMessageGenerator"/> to produce an HL7 v2 message.
///
/// Supported inputs:
/// - Bundle (transaction) → Patient + Encounter entries → "ADT A01"
/// - Patient → "ADT A08" (update)
/// - Encounter → "ADT A03" (finished) or "ADT A02" (in progress)
/// - ServiceRequest → "ORM O01"
/// - Appointment → "SIU S12" (or "SIU S15" when status = cancelled)
///
/// Returns null when the JSON is invalid or the resource type is not recognized.
/// </summary>
public static class FhirToHl7Converter
{
    /// <summary>
    /// Deserialize a FHIR R4 JSON payload and map it to an <see cref="AdtMessageRequest"/>.
    /// </summary>
    public static AdtMessageRequest? Convert(string fhirJson)
    {
        if (string.IsNullOrWhiteSpace(fhirJson))
            return null;

        Resource resource;
        try
        {
            var deserializer = new FhirJsonDeserializer();
            resource = deserializer.Deserialize<Resource>(fhirJson);
        }
        catch
        {
            return null;
        }

        return resource switch
        {
            Bundle bundle => ConvertBundle(bundle),
            Patient patient => ConvertPatient(patient),
            Encounter encounter => ConvertEncounter(encounter),
            ServiceRequest serviceRequest => ConvertServiceRequest(serviceRequest),
            Appointment appointment => ConvertAppointment(appointment),
            _ => null
        };
    }

    // ── Bundle (transaction) ──────────────────────────────────────────────────

    private static AdtMessageRequest? ConvertBundle(Bundle bundle)
    {
        Patient? patient = null;
        Encounter? encounter = null;

        foreach (var entry in bundle.Entry)
        {
            switch (entry.Resource)
            {
                case Patient p when patient is null:
                    patient = p;
                    break;
                case Encounter e when encounter is null:
                    encounter = e;
                    break;
            }
        }

        if (patient is null && encounter is null)
            return null;

        var request = new AdtMessageRequest { MessageTypeCode = "ADT A01" };

        if (patient != null)
            ApplyPatient(request, patient);

        if (encounter != null)
            ApplyEncounter(request, encounter);

        return request;
    }

    // ── Patient ───────────────────────────────────────────────────────────────

    private static AdtMessageRequest ConvertPatient(Patient patient)
    {
        var request = new AdtMessageRequest { MessageTypeCode = "ADT A08" };
        ApplyPatient(request, patient);
        return request;
    }

    private static void ApplyPatient(AdtMessageRequest request, Patient patient)
    {
        // Identifier: prefer the HL7Tester patient-id system, fall back to first identifier.
        var id = patient.Identifier.FirstOrDefault(
            i => i.System == FhirR4Generator.PatientIdSystem)?.Value
            ?? patient.Identifier.FirstOrDefault()?.Value;

        if (!string.IsNullOrWhiteSpace(id))
            request.PatientId = id;

        var name = patient.Name.FirstOrDefault();
        if (name != null)
        {
            if (!string.IsNullOrWhiteSpace(name.Family))
                request.PatientFamilyName = name.Family;

            var given = name.Given?.FirstOrDefault(g => !string.IsNullOrWhiteSpace(g));
            if (!string.IsNullOrWhiteSpace(given))
                request.PatientGivenName = given!;
        }

        if (!string.IsNullOrWhiteSpace(patient.BirthDate))
            request.BirthDate = ToHl7Date(patient.BirthDate);

        request.Sex = patient.Gender switch
        {
            AdministrativeGender.Male => "M",
            AdministrativeGender.Female => "F",
            AdministrativeGender.Other => "O",
            AdministrativeGender.Unknown => "U",
            _ => request.Sex
        };
    }

    // ── Encounter ─────────────────────────────────────────────────────────────

    private static AdtMessageRequest ConvertEncounter(Encounter encounter)
    {
        var request = new AdtMessageRequest
        {
            MessageTypeCode = encounter.Status == Encounter.EncounterStatus.Finished
                ? "ADT A03"
                : "ADT A02"
        };

        ApplyEncounter(request, encounter);
        return request;
    }

    private static void ApplyEncounter(AdtMessageRequest request, Encounter encounter)
    {
        // Admission number: HL7Tester system identifier, else first identifier.
        var visitNumber = encounter.Identifier.FirstOrDefault(
            i => i.System == FhirR4Generator.AdmissionNumberSystem)?.Value
            ?? encounter.Identifier.FirstOrDefault()?.Value;

        if (!string.IsNullOrWhiteSpace(visitNumber))
            request.AdmissionNumber = visitNumber;

        // Location display produced by FhirR4Generator: "Unit=X Room=Y Bed=Z Floor=W".
        foreach (var location in encounter.Location)
        {
            var display = location.Location?.Display;
            if (!string.IsNullOrWhiteSpace(display))
                ApplyLocationDisplay(request, display);
        }

        // Patient subject display may carry "Family, Given".
        var subjectDisplay = encounter.Subject?.Display;
        if (!string.IsNullOrWhiteSpace(subjectDisplay) && subjectDisplay.Contains(','))
        {
            var parts = subjectDisplay.Split(',', 2);
            request.PatientFamilyName = parts[0].Trim();
            if (parts.Length > 1)
                request.PatientGivenName = parts[1].Trim();
        }
    }

    // ── ServiceRequest (ORM) ──────────────────────────────────────────────────

    private static AdtMessageRequest ConvertServiceRequest(ServiceRequest serviceRequest)
    {
        var request = new AdtMessageRequest { MessageTypeCode = "ORM O01" };

        var identifier = serviceRequest.Identifier.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(identifier?.Value))
            request.OrmPlacerOrderNumber = identifier!.Value;

        if (serviceRequest.Code?.Coding is { Count: > 0 })
        {
            var coding = serviceRequest.Code.Coding[0];
            request.OrmUniversalServiceId = string.IsNullOrWhiteSpace(coding.Display)
                ? coding.Code ?? string.Empty
                : $"{coding.Code}^{coding.Display}";
        }
        else if (!string.IsNullOrWhiteSpace(serviceRequest.Code?.Text))
        {
            request.OrmUniversalServiceId = serviceRequest.Code!.Text!;
        }

        if (!string.IsNullOrWhiteSpace(serviceRequest.Subject?.Display) &&
            serviceRequest.Subject!.Display!.Contains(','))
        {
            var parts = serviceRequest.Subject.Display!.Split(',', 2);
            request.PatientFamilyName = parts[0].Trim();
            if (parts.Length > 1)
                request.PatientGivenName = parts[1].Trim();
        }

        var requesterDisplay = serviceRequest.Requester?.Display;
        if (!string.IsNullOrWhiteSpace(requesterDisplay) && requesterDisplay.Contains(','))
        {
            var parts = requesterDisplay.Split(',', 2);
            request.OrmOrderingProviderFamilyName = parts[0].Trim();
            if (parts.Length > 1)
                request.OrmOrderingProviderGivenName = parts[1].Trim();
        }

        return request;
    }

    // ── Appointment (SIU) ─────────────────────────────────────────────────────

    private static AdtMessageRequest ConvertAppointment(Appointment appointment)
    {
        var request = new AdtMessageRequest
        {
            MessageTypeCode = appointment.Status == Appointment.AppointmentStatus.Cancelled
                ? "SIU S15"
                : "SIU S12"
        };

        var identifier = appointment.Identifier.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(identifier?.Value))
            request.SiuFillerAppointmentId = identifier!.Value;

        if (appointment.Start != null)
            request.SiuAppointmentStartDateTime = appointment.Start.Value.ToString("yyyyMMddHHmmss");
        if (appointment.End != null)
            request.SiuAppointmentEndDateTime = appointment.End.Value.ToString("yyyyMMddHHmmss");

        if (!string.IsNullOrWhiteSpace(appointment.Description))
            request.SiuAppointmentReason = appointment.Description;

        // Participants: pat → patient display, ATND → practitioner, loc → location.
        foreach (var participant in appointment.Participant)
        {
            var display = participant.Actor?.Display;
            var typeCode = participant.Type?.FirstOrDefault()?.Coding?.FirstOrDefault()?.Code;

            switch (typeCode)
            {
                case "pat" when !string.IsNullOrWhiteSpace(display):
                    ApplyNameDisplay(request, display);
                    break;
                case "ATND" when !string.IsNullOrWhiteSpace(display):
                    var parts = display!.Split(',', 2);
                    request.SiuPersonnelFamilyName = parts[0].Trim();
                    if (parts.Length > 1)
                        request.SiuPersonnelGivenName = parts[1].Trim();
                    break;
                case "loc" when !string.IsNullOrWhiteSpace(display):
                    request.SiuLocationDescription = display!;
                    break;
            }
        }

        return request;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>Parse "Unit=X Room=Y Bed=Z Floor=W" back into request location fields.</summary>
    private static void ApplyLocationDisplay(AdtMessageRequest request, string display)
    {
        foreach (var token in display.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = token.IndexOf('=');
            if (eq <= 0)
                continue;

            var key = token[..eq];
            var value = token[(eq + 1)..];

            switch (key)
            {
                case "Unit": request.Unit = value; break;
                case "Room": request.Room = value; break;
                case "Bed": request.Bed = value; break;
                case "Floor": request.Floor = value; break;
            }
        }
    }

    /// <summary>Parse "Family, Given" into patient name fields.</summary>
    private static void ApplyNameDisplay(AdtMessageRequest request, string display)
    {
        var parts = display.Split(',', 2);
        request.PatientFamilyName = parts[0].Trim();
        if (parts.Length > 1)
            request.PatientGivenName = parts[1].Trim();
    }

    /// <summary>FHIR date (yyyy-MM-dd) → HL7 TS (yyyyMMdd).</summary>
    private static string ToHl7Date(string fhirDate)
    {
        var compact = new string(fhirDate.Where(char.IsDigit).ToArray());
        return compact.Length >= 8 ? compact[..8] : fhirDate;
    }
}
