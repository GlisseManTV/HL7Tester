using HL7Tester.Core.Fhir;

namespace HL7Tester.Core.Sending;

/// <summary>
/// <see cref="IMessageSender"/> implementation that routes a <see cref="SendRequest"/>
/// to the MLLP transport (<see cref="IHL7NetworkSender"/>) or the FHIR transport
/// (<see cref="FhirRestClient"/>) based on <see cref="SendRequest.Protocol"/>.
/// </summary>
public sealed class SenderRouter : IMessageSender
{
    private readonly IHL7NetworkSender _hl7Sender;
    private readonly FhirRestClient _fhirClient;

    public SenderRouter(IHL7NetworkSender hl7Sender, FhirRestClient fhirClient)
    {
        _hl7Sender = hl7Sender ?? throw new ArgumentNullException(nameof(hl7Sender));
        _fhirClient = fhirClient ?? throw new ArgumentNullException(nameof(fhirClient));
    }

    public async Task<MessageSendResult> SendAsync(SendRequest request, CancellationToken ct = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        if (string.Equals(request.Protocol, "FHIR", StringComparison.OrdinalIgnoreCase))
        {
            var fhir = await _fhirClient.SendAsync(request, ct).ConfigureAwait(false);
            return new MessageSendResult
            {
                Success = fhir.Success,
                Protocol = "FHIR",
                HttpStatusCode = fhir.HttpStatusCode,
                ResponseBody = fhir.ResponseBody,
                OperationOutcomeJson = fhir.OperationOutcomeJson,
                ErrorDescription = fhir.ErrorDescription,
                ErrorMessage = fhir.ErrorMessage
            };
        }

        // Default: MLLP.
        if (string.IsNullOrWhiteSpace(request.Ip) || request.Port is null)
        {
            return new MessageSendResult
            {
                Success = false,
                Protocol = "MLLP",
                ErrorMessage = "MLLP send requires an IP address and a port.",
                ErrorDescription = "Missing IP or port."
            };
        }

        var sendResult = await _hl7Sender
            .SendAsync(request.Payload, request.Ip, request.Port.Value, ct, request.EncodingName, request.ExistingClient)
            .ConfigureAwait(false);

        return new MessageSendResult
        {
            Success = sendResult.Success,
            Protocol = "MLLP",
            MsaStatus = sendResult.MsaStatus,
            AckMessage = sendResult.AckMessage,
            MessageCode = sendResult.MessageCode,
            ControlId = sendResult.ControlId,
            ErrorDescription = sendResult.ErrorDescription,
            ErrorMessage = sendResult.ErrorMessage
        };
    }
}
