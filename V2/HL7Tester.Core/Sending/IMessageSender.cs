namespace HL7Tester.Core.Sending;

/// <summary>
/// Protocol-agnostic send abstraction. Both the MLLP (HL7 v2) transport and the
/// FHIR REST transport implement this interface so callers (MainPage, BatchSendEngine)
/// depend on the abstraction rather than on a concrete protocol.
/// </summary>
public interface IMessageSender
{
    Task<MessageSendResult> SendAsync(SendRequest request, CancellationToken ct = default);
}
