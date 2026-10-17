using HL7Tester.Core.Fhir;

namespace HL7Tester.Core.Sending;

/// <summary>
/// Protocol-agnostic send request. Carries the payload (HL7 v2 string or FHIR JSON)
/// plus the transport-specific parameters needed by the selected sender.
/// </summary>
public sealed class SendRequest
{
    /// <summary>"MLLP" or "FHIR".</summary>
    public string Protocol { get; init; } = "MLLP";

    /// <summary>HL7 v2 message string or FHIR JSON document.</summary>
    public string Payload { get; init; } = string.Empty;

    // ---- MLLP ----
    public string? Ip { get; init; }
    public int? Port { get; init; }
    public string? EncodingName { get; init; }

    // ---- FHIR ----
    public FhirProfile? Profile { get; init; }
    public string? ResourceType { get; init; }
    public string? ResourceId { get; init; }
    public FhirHttpMethod HttpMethod { get; init; } = FhirHttpMethod.Post;
}

/// <summary>
/// HTTP verb used for a FHIR REST interaction.
/// </summary>
public enum FhirHttpMethod
{
    Get,
    Post,
    Put,
    Patch,
    Delete
}
