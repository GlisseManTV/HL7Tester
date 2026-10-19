using Hl7.Fhir.Model;
using HL7Tester.Core;

namespace HL7Tester.Core.Fhir;

/// <summary>
/// Generates a FHIR resource (or Bundle) from the shared HL7 form model.
/// Implemented per FHIR version: <see cref="FhirR4Generator"/> (R4, Phase 2);
/// DSTU2/R5 implementations can be plugged in later (decision D2).
/// </summary>
public interface IFhirResourceGenerator
{
    /// <summary>
    /// Generate a FHIR resource (or Bundle) from the HL7 message request.
    /// </summary>
    Resource Generate(AdtMessageRequest request);

    /// <summary>
    /// Serialize a FHIR resource to pretty-printed JSON.
    /// </summary>
    string ToJson(Resource resource);
}
