using System.Net.Sockets;
using System.Text;
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
    public string? AckMessage { get; init; }
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
    Task<SendResult> SendAsync(string hl7Message, string ipAddress, int port, CancellationToken cancellationToken = default, string? encodingName = null);
}

public sealed class Hl7NetworkSender : IHL7NetworkSender
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

    public async Task<SendResult> SendAsync(string hl7Message, string ipAddress, int port, CancellationToken cancellationToken = default, string? encodingName = null)
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

        using var client = new TcpClient();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            // Extraire le Message Code (MSH-9) pour l'affichage dans l'UI
            string? messageCode = ExtractMessageCode(hl7Message);

            // Log le contenu du message HL7 avant l'envoi
            var msgContent = hl7Message.Replace("\r\n", Environment.NewLine).Replace("\n", Environment.NewLine);
            _logger.LogInformation("Sending HL7 message to {Ip}:{Port} (Message Code: {MessageCode}):\n{Message}", ipAddress, port, messageCode, msgContent);

            await client.ConnectAsync(ipAddress, port, cts.Token).ConfigureAwait(false);

            using NetworkStream stream = client.GetStream();
            await stream.WriteAsync(bytes, 0, bytes.Length, cts.Token).ConfigureAwait(false);
            await stream.FlushAsync(cts.Token).ConfigureAwait(false);

            _logger.LogInformation("HL7 message sent to {Ip}:{Port} ({Bytes} bytes) with encoding {Encoding}", ipAddress, port, bytes.Length, encoding.EncodingName);

            // Optionnel : lecture d'un ACK en retour (non parsé pour l'instant)
            string? ackMessage = null;
            
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
                        // Formater l'ACK avec des sauts de ligne pour une meilleure lisibilité
                        var ackClean = CleanAckMessage(ackMessage);
                        _logger.LogInformation("Received ACK from {Ip}:{Port}:\n{Ack}", ipAddress, port, ackClean);
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

            return new SendResult
            {
                Success = true,
                MessageCode = messageCode,
                AckMessage = ackMessage != null ? CleanAckMessage(ackMessage) : null
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
    }

    /// <summary>
    /// Extrait le Message Code (MSH-9) du message HL7.
    /// MSH est le segment 1, MSH-9 contient le type de message au format MessageType^SubType.
    /// </summary>
    private static string? ExtractMessageCode(string hl7Message)
    {
        // Trouver le segment MSH (premier segment non vide)
        var lines = hl7Message.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            if (line.StartsWith("MSH"))
            {
                // Le séparateur de champs est '|' par défaut
                // MSH-1: |, MSH-2: ^~\&, MSH-3: SenderApp, MSH-4: ReceiverApp, 
                // MSH-5: ReceiverFacility, MSH-6: SenderFacility, MSH-7: DateTime,
                // MSH-8: Security (souvent vide), MSH-9: MessageType (ex: ADT^A01)
                // Donc MSH-9 est après le 8ème | et avant le 9ème |
                
                int pipeCount = 0;
                int startPos = -1;
                
                for (int i = 0; i < line.Length && pipeCount < 9; i++)
                {
                    if (line[i] == '|')
                    {
                        pipeCount++;
                        if (pipeCount == 8)
                        {
                            startPos = i + 1; // Commence après le 8ème |
                        }
                        else if (pipeCount == 9)
                        {
                            // Trouver le prochain | ou la fin du segment
                            int endPos = line.IndexOf('|', i + 1);
                            if (endPos < 0)
                                endPos = line.Length;
                            
                            return line.Substring(startPos, endPos - startPos);
                        }
                    }
                }
            }
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
