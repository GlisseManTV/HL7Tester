using System.Net.Sockets;
using System.Text;
using HL7Tester.Core.Sending;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HL7Tester.Core;

/// <summary>
/// Resultat de l'envoi d'un message HL7.
/// </summary>
public sealed class SendResult
{
    public bool Success { get; init; }
    public string? MessageCode { get; init; }
    public string? ControlId { get; init; }
    public string? AckMessage { get; init; }
    public string? MsaStatus { get; init; }
    public string? ErrorDescription { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
}

/// <summary>
/// Service d'envoi de messages HL7 sur TCP avec framing MLLP.
/// </summary>
public interface IHL7NetworkSender
{
    Task<SendResult> SendAsync(string hl7Message, string ipAddress, int port, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Envoie un message HL7 avec un encodage spécifique.
    /// </summary>
    /// <param name="hl7Message">Le message HL7 à envoyer.</param>
    /// <param name="ipAddress">L'adresse IP du serveur cible.</param>
    /// <param name="port">Le port du serveur cible.</param>
    /// <param name="cancellationToken">Token d'annulation.</param>
    /// <param name="encodingName">Nom de l'encodage à utiliser (ex: "UTF-8", "CP1252", "ISO-8859-1"). Si null, utilise UTF-8 par défaut.</param>
    /// <param name="existingClient">Optionnel : un TcpClient déjà connecté à réutiliser (Keep Connection Open).</param>
    Task<SendResult> SendAsync(string hl7Message, string ipAddress, int port, CancellationToken cancellationToken = default, string? encodingName = null, TcpClient? existingClient = null);
}

public sealed class Hl7NetworkSender : IHL7NetworkSender, IMessageSender
{
    private readonly ILogger<Hl7NetworkSender> _logger;

    public Hl7NetworkSender(ILogger<Hl7NetworkSender>? logger = null)
    {
        _logger = logger ?? NullLogger<Hl7NetworkSender>.Instance;
    }

    public async Task<SendResult> SendAsync(string hl7Message, string ipAddress, int port, CancellationToken cancellationToken = default)
    {
        return await SendAsync(hl7Message, ipAddress, port, cancellationToken, encodingName: null).ConfigureAwait(false);
    }

    public async Task<SendResult> SendAsync(string hl7Message, string ipAddress, int port, CancellationToken cancellationToken = default, string? encodingName = null, TcpClient? existingClient = null)
    {
        if (string.IsNullOrWhiteSpace(hl7Message))
            throw new ArgumentException("HL7 message cannot be null or empty.", nameof(hl7Message));
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address cannot be null or empty.", nameof(ipAddress));
        if (port < 1 || port > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");

        // Framing MLLP : <VT>message<FS><CR>
        var payload = $"\x0B{hl7Message}\x1C\r";
        var encoding = GetEncodingFromString(encodingName);
        var bytes = encoding.GetBytes(payload);

        var ownsClient = existingClient is null;
        var client = existingClient ?? new TcpClient();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            // Extract MSH-9 (message code) and MSH-10 (control ID) for UI display
            string? messageCode = ExtractMessageCode(hl7Message);
            string? controlId = ExtractControlId(hl7Message);

            // Log le contenu du message HL7 avant l'envoi
            var msgContent = hl7Message.Replace("\r\n", Environment.NewLine).Replace("\n", Environment.NewLine);
            _logger.LogInformation("Sending HL7 message to {Ip}:{Port} (Message Code: {MessageCode}):\n{Message}", ipAddress, port, messageCode, msgContent);

            if (existingClient is null)
            {
                await client.ConnectAsync(ipAddress, port, cts.Token).ConfigureAwait(false);
            }

            var stream = client.GetStream();
            await stream.WriteAsync(bytes, 0, bytes.Length, cts.Token).ConfigureAwait(false);
            await stream.FlushAsync(cts.Token).ConfigureAwait(false);

            _logger.LogInformation("HL7 message sent to {Ip}:{Port} ({Bytes} bytes) with encoding {Encoding}", ipAddress, port, bytes.Length, encoding.EncodingName);

            // Lecture d'un ACK en retour
            string? ackMessage = null;
            string? msaStatus = null;

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
                        ackMessage = encoding.GetString(buffer, 0, read);
                        var ackClean = CleanAckMessage(ackMessage);
                        msaStatus = ExtractMsaStatus(ackClean);
                        _logger.LogInformation("Received ACK from {Ip}:{Port} (MSA-1: {MsaStatus}):\n{Ack}", ipAddress, port, msaStatus ?? "N/A", ackClean);
                    }
                    else
                    {
                        _logger.LogInformation("No ACK received from {Ip}:{Port}", ipAddress, port);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while reading ACK from {Ip}:{Port}.", ipAddress, port);
                }
            }

            // Déterminer le succès basé sur le statut MSA-1
            // AA = Accept, AE = Error accept, AR = Reject
            bool isSuccess = msaStatus == null || msaStatus == "AA";
            string errorMessage = string.Empty;
            string? errorDescription = null;
            if (!isSuccess)
            {
                errorDescription = ExtractMsaErrorDescription(ackMessage);
                errorMessage = msaStatus == "AR"
                    ? $"Message rejected by receiver (MSA-1: AR)"
                    : $"Message accepted with errors (MSA-1: AE)";
                _logger.LogWarning("Message {MsaStatus} by receiver at {Ip}:{Port}. Description: {Desc}", msaStatus, ipAddress, port, errorDescription ?? "(none)");
            }

            return new SendResult
            {
                Success = isSuccess,
                MessageCode = messageCode,
                ControlId = controlId,
                AckMessage = ackMessage != null ? CleanAckMessage(ackMessage) : null,
                MsaStatus = msaStatus,
                ErrorDescription = errorDescription,
                ErrorMessage = errorMessage
            };
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error while sending HL7 message to {ipAddress}:{port}";
            _logger.LogError(ex, "{ErrorMsg}\n{ExMessage}", errorMsg, ex.Message);
            return new SendResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            if (ownsClient)
            {
                client.Dispose();
            }
        }
    }

    /// <summary>
    /// Protocol-agnostic entry point (<see cref="IMessageSender"/>). Adapts a
    /// <see cref="SendRequest"/> to the existing MLLP API and maps the legacy
    /// <see cref="SendResult"/> onto a unified <see cref="MessageSendResult"/>.
    /// The original <c>SendAsync(string, string, int, ...)</c> API is preserved so
    /// existing callers (BatchSendEngine / MainViewModel) keep working unchanged.
    /// </summary>
    public async Task<MessageSendResult> SendAsync(SendRequest request, CancellationToken ct = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

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

        var result = await SendAsync(
            request.Payload,
            request.Ip,
            request.Port.Value,
            ct,
            request.EncodingName).ConfigureAwait(false);

        return new MessageSendResult
        {
            Success = result.Success,
            Protocol = "MLLP",
            MsaStatus = result.MsaStatus,
            AckMessage = result.AckMessage,
            MessageCode = result.MessageCode,
            ControlId = result.ControlId,
            ErrorDescription = result.ErrorDescription,
            ErrorMessage = result.ErrorMessage
        };
    }

    /// <summary>
    /// Extracts the Message Code (MSH-9) from the HL7 message.
    /// MSH-9 holds the message type in the form MessageType^SubType.
    /// </summary>
    internal static string? ExtractMessageCode(string hl7Message)
    {
        var line = FindMshLine(hl7Message);
        if (line is null)
            return null;

        // For the MSH line, Split('|') yields index 0 = "MSH", index 1 = MSH-2
        // (the encoding characters), and in general MSH-n (n >= 2) = split[n - 1].
        // Therefore MSH-9 (message type) = split[8].
        var fields = line.Split('|');
        if (fields.Length > 8)
        {
            var value = fields[8].Trim();
            return value.Length > 0 ? value : null;
        }

        return null;
    }

    /// <summary>
    /// Extracts the ControlID (MSH-10) from the HL7 message.
    /// For the MSH line, MSH-n (n >= 2) maps to split[n - 1], so MSH-10 = split[9].
    /// </summary>
    internal static string? ExtractControlId(string hl7Message)
    {
        var line = FindMshLine(hl7Message);
        if (line is null)
            return null;

        var fields = line.Split('|');
        if (fields.Length > 9)
        {
            var value = fields[9].Trim();
            return value.Length > 0 ? value : null;
        }

        return null;
    }

    /// <summary>
    /// Returns the first non-empty line starting with "MSH", or null if none is found.
    /// </summary>
    private static string? FindMshLine(string hl7Message)
    {
        var lines = hl7Message.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.StartsWith("MSH"))
                return line;
        }

        return null;
    }

    /// <summary>
    /// Nettoie un message ACK en retirant les caractères de framing MLLP.
    /// </summary>
    private static string CleanAckMessage(string ack)
    {
        return ack.Replace("\x0B", "").Replace("\x1C", "").Replace("\r", "\n").TrimEnd('\n');
    }

    /// <summary>
    /// Extrait le statut MSA-1 (Acknowledge type) d'une réponse HL7.
    /// MSA-1: AA = Accept, AE = Error accept, AR = Reject.
    /// </summary>
    /// <param name="ackMessage">Le message ACK nettoyé.</param>
    /// <returns>Le code de statut (ex: "AA", "AE", "AR") ou null si non trouvé.</returns>
    private static string? ExtractMsaStatus(string ackMessage)
    {
        var lines = ackMessage.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.StartsWith("MSA|"))
            {
                // MSA|AA|controlId|...
                // MSA-1 est le premier champ après "MSA|"
                var fields = line.Split('|');
                if (fields.Length >= 2)
                {
                    return fields[1].Trim();
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Extrait le texte d'erreur MSA-3 d'une réponse HL7.
    /// MSA-3 contient la description de l'erreur (ex: "ARTrack HL7Service is unable to upload...").
    /// </summary>
    /// <param name="ackMessage">Le message ACK brut (non nettoyé).</param>
    /// <returns>Le texte d'erreur ou null si non trouvé.</returns>
    private static string? ExtractMsaErrorDescription(string? ackMessage)
    {
        if (string.IsNullOrWhiteSpace(ackMessage))
            return null;

        var clean = CleanAckMessage(ackMessage);
        var lines = clean.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.StartsWith("MSA|"))
            {
                // MSA|AR|controlId|ErrorDescription
                var fields = line.Split('|');
                if (fields.Length >= 4)
                {
                    var desc = fields[3].Trim();
                    return string.IsNullOrEmpty(desc) ? null : desc;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Obtient un objet Encoding à partir d'une chaîne de caractères.
    /// </summary>
    /// <param name="encodingName">Nom de l'encodage (ex: "UTF-8", "CP1252", "ISO-8859-1"). Si null ou vide, retourne UTF-8.</param>
    private static Encoding GetEncodingFromString(string? encodingName)
    {
        if (string.IsNullOrWhiteSpace(encodingName))
            return Encoding.UTF8;

        return encodingName.Trim() switch
        {
            "UTF-8" or "utf-8" => Encoding.UTF8,
            "CP1252" or "Cp1252" or "windows-1252" or "Windows-1252" => Encoding.GetEncoding("Cp1252"),
            "ISO-8859-1" or "Iso-8859-1" or "iso8859-1" or "ISO8859-1" => Encoding.GetEncoding("Iso-8859-1"),
            "Latin1" or "latin1" => Encoding.Latin1,
            _ => Encoding.GetEncoding(encodingName.Trim())
        };
    }
}
