using System.Globalization;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using HL7Tester.Core;

namespace HL7Tester.Core.Fhir;

/// <summary>
/// FHIR R4 implementation of <see cref="IFhirResourceGenerator"/> built on the
/// Firely SDK (decision D1). Consumes the shared <see cref="AdtMessageRequest"/>
/// form model and produces either a single resource or a transaction Bundle,
/// driven by <see cref="AdtMessageRequest.MessageTypeCode"/> (design doc section 2).
///
/// Mapping (message type -> resource):
/// - ADT A01/A04/A05/A28 -> transaction Bundle (Patient POST + Encounter POST, urn:uuid cross-reference)
/// - ADT A08/A31         -> Patient (PUT)
/// - ADT A03             -> Encounter with status=finished (PATCH)
/// - ADT A02/A09/A10/A21/A22 -> Encounter with status=in-progress + location update
/// - ADT A18/A40         -> Parameters for $merge (names configurable via FhirMergeConfig, D10)
/// - ORM O01             -> ServiceRequest
/// - SIU S12/S13/S14     -> Appointment
/// - SIU S15             -> Appointment with status=cancelled (PATCH)
///
/// Cross-resource links use conditional references ("Type?identifier=value",
/// decision D7) outside Bundles, and "urn:uuid" fullUrl references inside Bundles.
/// </summary>
public sealed class FhirR4Generator : IFhirResourceGenerator
{
    /// <summary>Identifier system used for the HL7 PatientId in FHIR resources.</summary>
    public const string PatientIdSystem = "urn:hl7tester:patient-id";

    /// <summary>Identifier system used for the HL7 AdmissionNumber on Encounter.</summary>
    public const string AdmissionNumberSystem = "urn:hl7tester:admission-number";

    private readonly FhirMergeConfig _mergeConfig;

    public FhirR4Generator(FhirMergeConfig? mergeConfig = null)
    {
        _mergeConfig = mergeConfig ?? new FhirMergeConfig();
    }

    public Resource Generate(AdtMessageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var code = (request.MessageTypeCode ?? string.Empty).ToUpperInvariant();

        // Admission-like events: atomic Patient + Encounter creation.
        if (ContainsAny(code, "ADT A01", "ADT A04", "ADT A05", "ADT A28"))
            return BuildAdmissionBundle(request);

        // Patient identity updates: full Patient for PUT.
        if (ContainsAny(code, "ADT A08", "ADT A31"))
            return BuildPatient(request, includeId: true);

        // Discharge: partial Encounter for PATCH with status finished.
        if (code.Contains("ADT A03", StringComparison.Ordinal))
            return BuildEncounterStatusUpdate(request, Encounter.EncounterStatus.Finished, includeLocation: false);

        // Transfer/move-like events: Encounter status + location update.
        if (ContainsAny(code, "ADT A02", "ADT A09", "ADT A10", "ADT A21", "ADT A22"))
            return BuildEncounterStatusUpdate(request, Encounter.EncounterStatus.InProgress, includeLocation: true);

        // Patient merge: $merge Parameters (names configurable, decision D10).
        if (ContainsAny(code, "ADT A18", "ADT A40"))
            return BuildMergeParameters(request);

        // Orders: ServiceRequest.
        if (code.Contains("ORM O01", StringComparison.Ordinal))
            return BuildServiceRequest(request);

        // Appointments: create / reschedule / modify.
        if (ContainsAny(code, "SIU S12", "SIU S13", "SIU S14"))
            return BuildAppointment(request, cancelled: false);

        // Appointment cancellation: partial Appointment for PATCH.
        if (code.Contains("SIU S15", StringComparison.Ordinal))
            return BuildAppointment(request, cancelled: true);

        throw new NotSupportedException($"Unsupported HL7 message type for FHIR generation: '{request.MessageTypeCode}'.");
    }

    public string ToJson(Resource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        // Firely SDK 6.x: serialization goes through FhirJsonSerializer with the
        // SerializeToString extension (pretty = true for indented output).
        var serializer = new FhirJsonSerializer();
        return serializer.SerializeToString(resource, pretty: true);
    }

    // ------------------------------------------------------------------
    // Bundle (transaction) — ADT A01/A04/A05/A28
    // ------------------------------------------------------------------

    private Bundle BuildAdmissionBundle(AdtMessageRequest request)
    {
        var patientFullUrl = $"urn:uuid:{Guid.NewGuid():N}";

        var bundle = new Bundle { Type = Bundle.BundleType.Transaction };

        bundle.Entry.Add(new Bundle.EntryComponent
        {
            FullUrl = patientFullUrl,
            Resource = BuildPatient(request, includeId: false),
            Request = new Bundle.RequestComponent
            {
                Method = Bundle.HTTPVerb.POST,
                Url = "Patient"
            }
        });

        var encounter = BuildEncounter(request, Encounter.EncounterStatus.InProgress, includeLocation: true);
        encounter.Subject = new ResourceReference(patientFullUrl) { Display = BuildPatientDisplay(request) };

        bundle.Entry.Add(new Bundle.EntryComponent
        {
            Resource = encounter,
            Request = new Bundle.RequestComponent
            {
                Method = Bundle.HTTPVerb.POST,
                Url = "Encounter"
            }
        });

        return bundle;
    }

    // ------------------------------------------------------------------
    // Patient — ADT A08/A31 (and inside the admission Bundle)
    // ------------------------------------------------------------------

    private Patient BuildPatient(AdtMessageRequest request, bool includeId)
    {
        var patient = new Patient();

        if (includeId && !string.IsNullOrWhiteSpace(request.PatientId))
            patient.Id = request.PatientId;

        if (!string.IsNullOrWhiteSpace(request.PatientId))
        {
            patient.Identifier.Add(new Identifier
            {
                System = PatientIdSystem,
                Value = request.PatientId
            });
        }

        var name = new HumanName();
        var hasName = false;
        if (!string.IsNullOrWhiteSpace(request.PatientFamilyName))
        {
            name.Family = request.PatientFamilyName;
            hasName = true;
        }
        if (!string.IsNullOrWhiteSpace(request.PatientGivenName))
        {
            name.Given = new List<string> { request.PatientGivenName };
            hasName = true;
        }
        if (hasName)
            patient.Name.Add(name);

        var gender = FhirTerminologyMapper.MapSex(request.Sex);
        if (gender.HasValue)
            patient.Gender = gender.Value;

        var birthDate = ToFhirDate(request.BirthDate);
        if (birthDate != null)
            patient.BirthDate = birthDate;

        return patient;
    }

    // ------------------------------------------------------------------
    // Encounter — ADT A02/A03/A09/A10/A21/A22 and inside the admission Bundle
    // ------------------------------------------------------------------

    private Encounter BuildEncounter(AdtMessageRequest request, Encounter.EncounterStatus status, bool includeLocation)
    {
        var encounter = new Encounter
        {
            Status = status,
            Class = FhirTerminologyMapper.MapEncounterClass(patientClass: null) // ADT always inpatient in this app
        };

        if (!string.IsNullOrWhiteSpace(request.AdmissionNumber))
        {
            encounter.Identifier.Add(new Identifier
            {
                System = AdmissionNumberSystem,
                Value = request.AdmissionNumber
            });
        }

        // Outside a Bundle, link to the patient with a conditional reference (decision D7).
        if (!string.IsNullOrWhiteSpace(request.PatientId))
        {
            encounter.Subject = new ResourceReference($"Patient?identifier={Uri.EscapeDataString(request.PatientId)}")
            {
                Display = BuildPatientDisplay(request)
            };
        }

        if (includeLocation)
        {
            var locationDisplay = BuildLocationDisplay(request);
            if (locationDisplay != null)
            {
                encounter.Location.Add(new Encounter.LocationComponent
                {
                    Location = new ResourceReference { Display = locationDisplay }
                });
            }
        }

        return encounter;
    }

    private Encounter BuildEncounterStatusUpdate(AdtMessageRequest request, Encounter.EncounterStatus status, bool includeLocation)
    {
        var encounter = BuildEncounter(request, status, includeLocation);

        if (status == Encounter.EncounterStatus.Finished)
            encounter.Period = new Period { End = FormatInstant(DateTime.Now) };

        return encounter;
    }

    // ------------------------------------------------------------------
    // $merge Parameters — ADT A18/A40 (decision D10)
    // ------------------------------------------------------------------

    private Parameters BuildMergeParameters(AdtMessageRequest request)
    {
        // In HL7 A40/A18, PID carries the surviving (target) patient and MRG the
        // merged (source) patient: PatientId = source, NewPatientId = target.
        var sourceId = request.PatientId;
        var targetId = !string.IsNullOrWhiteSpace(request.NewPatientId) ? request.NewPatientId : request.PatientId;

        var parameters = new Parameters();

        parameters.Parameter.Add(new Parameters.ParameterComponent
        {
            Name = _mergeConfig.SourceParamName,
            Value = new FhirString(sourceId)
        });

        parameters.Parameter.Add(new Parameters.ParameterComponent
        {
            Name = _mergeConfig.TargetParamName,
            Value = new FhirString(targetId)
        });

        if (!string.IsNullOrWhiteSpace(_mergeConfig.OperationParamName))
        {
            parameters.Parameter.Add(new Parameters.ParameterComponent
            {
                Name = _mergeConfig.OperationParamName,
                Value = new FhirString("merge")
            });
        }

        if (!string.IsNullOrWhiteSpace(_mergeConfig.ReferenceParamName))
        {
            parameters.Parameter.Add(new Parameters.ParameterComponent
            {
                Name = _mergeConfig.ReferenceParamName,
                Value = new FhirString($"Patient/{targetId}")
            });
        }

        return parameters;
    }

    // ------------------------------------------------------------------
    // ServiceRequest — ORM O01
    // ------------------------------------------------------------------

    private ServiceRequest BuildServiceRequest(AdtMessageRequest request)
    {
        var serviceRequest = new ServiceRequest
        {
            Status = FhirTerminologyMapper.MapServiceRequestStatus(request.OrmOrderStatus)
                     ?? RequestStatus.Active,
            Intent = RequestIntent.Order,
            Code = BuildCodeableConcept(request.OrmUniversalServiceId)
        };

        if (!string.IsNullOrWhiteSpace(request.PatientId))
        {
            serviceRequest.Subject = new ResourceReference($"Patient?identifier={Uri.EscapeDataString(request.PatientId)}")
            {
                Display = BuildPatientDisplay(request)
            };
        }

        if (!string.IsNullOrWhiteSpace(request.OrmOrderingProviderId))
        {
            var display = JoinNames(request.OrmOrderingProviderFamilyName, request.OrmOrderingProviderGivenName);
            serviceRequest.Requester = new ResourceReference($"Practitioner?identifier={Uri.EscapeDataString(request.OrmOrderingProviderId)}")
            {
                Display = display
            };
        }

        if (!string.IsNullOrWhiteSpace(request.OrmPlacerOrderNumber))
        {
            serviceRequest.Identifier.Add(new Identifier
            {
                System = "urn:hl7tester:placer-order-number",
                Value = request.OrmPlacerOrderNumber
            });
        }

        return serviceRequest;
    }

    // ------------------------------------------------------------------
    // Appointment — SIU S12/S13/S14/S15
    // ------------------------------------------------------------------

    private Appointment BuildAppointment(AdtMessageRequest request, bool cancelled)
    {
        var appointment = new Appointment
        {
            Status = cancelled
                ? Appointment.AppointmentStatus.Cancelled
                : FhirTerminologyMapper.MapAppointmentStatus(request.SiuAppointmentStatus)
                  ?? Appointment.AppointmentStatus.Booked
        };

        // S13/S14/S15 target an existing appointment: carry the filler/placer id.
        var existingId = !string.IsNullOrWhiteSpace(request.SiuFillerAppointmentId)
            ? request.SiuFillerAppointmentId
            : request.SiuPlacerAppointmentId;
        if (cancelled || (!string.IsNullOrWhiteSpace(existingId) && !string.IsNullOrWhiteSpace(request.SiuPlacerAppointmentId)))
        {
            // For S12 (new appointment) we keep the placer id only as an identifier.
            if (!string.IsNullOrWhiteSpace(existingId))
            {
                appointment.Identifier.Add(new Identifier
                {
                    System = "urn:hl7tester:appointment-id",
                    Value = existingId
                });
            }
        }

        var start = ToFhirInstant(request.SiuAppointmentStartDateTime);
        var end = ToFhirInstant(request.SiuAppointmentEndDateTime);
        if (start.HasValue)
            appointment.Start = start;
        if (end.HasValue)
            appointment.End = end;

        if (!string.IsNullOrWhiteSpace(request.SiuAppointmentReason))
            appointment.Description = request.SiuAppointmentReason;

        // Participants: patient, practitioner, resource (HealthcareService), location.
        if (!string.IsNullOrWhiteSpace(request.PatientId))
        {
            appointment.Participant.Add(BuildParticipant(
                $"Patient?identifier={Uri.EscapeDataString(request.PatientId)}",
                BuildPatientDisplay(request),
                "pat"));
        }

        if (!string.IsNullOrWhiteSpace(request.SiuPersonnelId))
        {
            appointment.Participant.Add(BuildParticipant(
                $"Practitioner?identifier={Uri.EscapeDataString(request.SiuPersonnelId)}",
                JoinNames(request.SiuPersonnelFamilyName, request.SiuPersonnelGivenName),
                "ATND"));
        }

        if (!string.IsNullOrWhiteSpace(request.SiuGeneralResourceId))
        {
            appointment.Participant.Add(BuildParticipant(
                $"HealthcareService?identifier={Uri.EscapeDataString(request.SiuGeneralResourceId)}",
                request.SiuGeneralResourceName,
                "dev"));
        }

        if (!string.IsNullOrWhiteSpace(request.SiuLocationCode))
        {
            appointment.Participant.Add(BuildParticipant(
                $"Location?identifier={Uri.EscapeDataString(request.SiuLocationCode)}",
                request.SiuLocationDescription,
                "loc"));
        }

        return appointment;
    }

    private static Appointment.ParticipantComponent BuildParticipant(string reference, string? display, string participationTypeCode)
    {
        return new Appointment.ParticipantComponent
        {
            Actor = new ResourceReference(reference) { Display = display },
            Status = ParticipationStatus.Accepted,
            Type = new List<CodeableConcept>
            {
                new CodeableConcept
                {
                    Coding = new List<Coding>
                    {
                        new Coding(FhirTerminologyMapper.ParticipationTypeSystem, participationTypeCode, participationTypeCode)
                    }
                }
            }
        };
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static bool ContainsAny(string haystack, params string[] needles)
    {
        foreach (var needle in needles)
        {
            if (haystack.Contains(needle, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private static CodeableConcept BuildCodeableConcept(string caretValue)
    {
        var concept = new CodeableConcept();
        if (string.IsNullOrWhiteSpace(caretValue))
            return concept;

        concept.Text = caretValue;

        // HL7 CWE-like value "CODE^Display" -> FHIR Coding.
        var parts = caretValue.Split('^', 2);
        if (parts[0].Length > 0)
        {
            concept.Coding.Add(new Coding
            {
                Code = parts[0],
                Display = parts.Length > 1 && parts[1].Length > 0 ? parts[1] : null
            });
        }

        return concept;
    }

    private static string? BuildPatientDisplay(AdtMessageRequest request)
    {
        var display = JoinNames(request.PatientFamilyName, request.PatientGivenName);
        return string.IsNullOrWhiteSpace(display) ? null : display;
    }

    private static string? BuildLocationDisplay(AdtMessageRequest request)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Unit)) parts.Add($"Unit={request.Unit}");
        if (!string.IsNullOrWhiteSpace(request.Room)) parts.Add($"Room={request.Room}");
        if (!string.IsNullOrWhiteSpace(request.Bed)) parts.Add($"Bed={request.Bed}");
        if (!string.IsNullOrWhiteSpace(request.Floor)) parts.Add($"Floor={request.Floor}");
        return parts.Count == 0 ? null : string.Join(" ", parts);
    }

    private static string JoinNames(string? family, string? given)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(family)) parts.Add(family);
        if (!string.IsNullOrWhiteSpace(given)) parts.Add(given);
        return string.Join(", ", parts);
    }

    /// <summary>
    /// HL7 TS date (yyyyMMdd, possibly with time part) -> FHIR date (yyyy-MM-dd).
    /// Returns null when the value cannot be parsed.
    /// </summary>
    private static string? ToFhirDate(string? hl7Date)
    {
        if (string.IsNullOrWhiteSpace(hl7Date))
            return null;

        var value = hl7Date.Trim();

        if (DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        // Already ISO or unparseable: pass through only if it looks like a FHIR date.
        return value.Length >= 8 && value[4] == '-' ? value : null;
    }

    /// <summary>
    /// HL7 TS timestamp (yyyyMMddHHmmss / yyyyMMddHHmm / yyyyMMdd) -> FHIR instant
    /// as a DateTimeOffset (Firely SDK 6.x typed instant). Values without an explicit
    /// offset are interpreted as local time. Returns null when unparseable.
    /// </summary>
    private static DateTimeOffset? ToFhirInstant(string? hl7DateTime)
    {
        if (string.IsNullOrWhiteSpace(hl7DateTime))
            return null;

        var value = hl7DateTime.Trim();

        string[] formats =
        {
            "yyyyMMddHHmmss",
            "yyyyMMddHHmm",
            "yyyyMMddHH",
            "yyyyMMdd"
        };

        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return new DateTimeOffset(parsed);

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var loose))
            return new DateTimeOffset(loose);

        return null;
    }

    private static string FormatInstant(DateTime value) =>
        value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
}
