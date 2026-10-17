namespace HL7Tester.Core.Sending;

/// <summary>
/// Unified, protocol-agnostic result of a send operation. MLLP-specific fields
/// (MSA status, ACK) and FHIR-specific fields (HTTP status, response body,
/// OperationOutcome) are both present; only the relevant subset is populated
/// depending on the protocol used.
/// </summary>
public sealed class MessageSendResult
{
    public bool Success { get; init; }

    /// <summary>"MLLP" or "FHIR".</summary>
    public string Protocol { get; init; } = "MLLP";

    // ---- MLLP fields ----
    public string? MsaStatus { get; init; }
    public string? AckMessage { get; init; }

    // ---- FHIR fields ----
    public int? HttpStatusCode { get; init; }
    public string? ResponseBody { get; init; }
    public string? OperationOutcomeJson { get; init; }

    // ---- Common ----
    public string? ErrorDescription { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
}
