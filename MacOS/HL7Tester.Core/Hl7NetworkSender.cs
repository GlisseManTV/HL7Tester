using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HL7Tester.Core;

/// <summary>
/// Service d'envoi de messages HL7 sur TCP avec framing MLLP.
/// </summary>
public interface IHL7NetworkSender
{
    Task SendAsync(string hl7Message, string ipAddress, int port, CancellationToken cancellationToken = default);
}

public sealed class Hl7NetworkSender : IHL7NetworkSender
{
    private readonly ILogger<Hl7NetworkSender> _logger;

    public Hl7NetworkSender(ILogger<Hl7NetworkSender>? logger = null)
    {
        _logger = logger ?? NullLogger<Hl7NetworkSender>.Instance;
    }

    public async Task SendAsync(string hl7Message, string ipAddress, int port, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hl7Message))
            throw new ArgumentException("HL7 message cannot be null or empty.", nameof(hl7Message));
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address cannot be null or empty.", nameof(ipAddress));
        if (port < 1 || port > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");

        // Framing MLLP : <VT>message<FS><CR>
        var payload = $"\x0B{hl7Message}\x1C\r";
        // Pour rester cross‑platform sans dépendre de windows-1252, on utilise Latin1,
        // suffisant pour nos besoins (principalement ASCII en pratique).
        var bytes = Encoding.Latin1.GetBytes(payload);

        using var client = new TcpClient();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            _logger.LogInformation("Connecting to {Ip}:{Port} for HL7 send", ipAddress, port);

            await client.ConnectAsync(ipAddress, port, cts.Token).ConfigureAwait(false);

            using NetworkStream stream = client.GetStream();
            await stream.WriteAsync(bytes, 0, bytes.Length, cts.Token).ConfigureAwait(false);
            await stream.FlushAsync(cts.Token).ConfigureAwait(false);

            _logger.LogInformation("HL7 message sent to {Ip}:{Port} (length={Length} bytes)", ipAddress, port, bytes.Length);

            // Optionnel : lecture d'un ACK en retour (non parsé pour l'instant)
            // On lit de manière best-effort, sans faire échouer l'envoi si rien n'arrive.
            if (stream.CanRead)
            {
                var buffer = new byte[4096];
                stream.ReadTimeout = 3000; // 3 secondes
                try
                {
                    int read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cts.Token).ConfigureAwait(false);
                    if (read > 0)
                    {
                        var ack = Encoding.Latin1.GetString(buffer, 0, read);
                        _logger.LogInformation("Received ACK from {Ip}:{Port}: {Ack}", ipAddress, port, ack);
                    }
                    else
                    {
                        _logger.LogInformation("No ACK received from {Ip}:{Port}", ipAddress, port);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while reading ACK from {Ip}:{Port}", ipAddress, port);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending HL7 message to {Ip}:{Port}", ipAddress, port);
            throw;
        }
    }
}
