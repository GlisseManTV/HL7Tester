using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HL7Tester.Core;

/// <summary>
/// Result of a passive TCP endpoint probe.
/// </summary>
public sealed record EndpointProbeResult
{
    /// <summary>True when the TCP connection (3-way handshake) succeeded.</summary>
    public bool Reachable { get; init; }

    /// <summary>Human-readable, English description of the outcome.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Round-trip time of the successful connection (zero on failure).</summary>
    public TimeSpan Latency { get; init; }
}

/// <summary>
/// Passive reachability probe for MLLP/TCP endpoints. Performs a plain TCP
/// connection (3-way handshake) only — it never sends an HL7/MLLP payload, so
/// it does not pollute production HL7 receivers.
/// </summary>
public interface IEndpointProbe
{
    /// <summary>
    /// Tries to open a TCP connection to the given endpoint within the timeout.
    /// </summary>
    /// <param name="ip">IP address or host name of the endpoint.</param>
    /// <param name="port">TCP port of the endpoint.</param>
    /// <param name="timeoutMs">Connection timeout in milliseconds (default 3000).</param>
    /// <param name="cancellationToken">Optional external cancellation token.</param>
    Task<EndpointProbeResult> TestTcpAsync(string ip, int port, int timeoutMs = 3000, CancellationToken cancellationToken = default);
}

/// <summary>
/// Ephemeral reachability state of an endpoint, shown on each Saved Connections row.
/// </summary>
public enum EndpointTestState
{
    Unknown,
    Testing,
    Reachable,
    Unreachable
}

/// <summary>
/// Default <see cref="IEndpointProbe"/> implementation using <see cref="TcpClient"/>.
/// </summary>
public sealed class EndpointProbe : IEndpointProbe
{
    private readonly ILogger<EndpointProbe> _logger;

    public EndpointProbe(ILogger<EndpointProbe>? logger = null)
    {
        _logger = logger ?? NullLogger<EndpointProbe>.Instance;
    }

    public async Task<EndpointProbeResult> TestTcpAsync(string ip, int port, int timeoutMs = 3000, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            _logger.LogWarning("Endpoint probe skipped: IP address is empty.");
            return new EndpointProbeResult { Reachable = false, Message = "Invalid IP address." };
        }

        if (port < 1 || port > 65535)
        {
            _logger.LogWarning("Endpoint probe skipped for {Ip}: invalid port {Port}.", ip, port);
            return new EndpointProbeResult { Reachable = false, Message = "Invalid port." };
        }

        ip = ip.Trim();

        var stopwatch = Stopwatch.StartNew();
        using var client = new TcpClient();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeoutMs);

        try
        {
            await client.ConnectAsync(ip, port, cts.Token).ConfigureAwait(false);
            stopwatch.Stop();

            var latency = stopwatch.Elapsed;
            _logger.LogInformation("Endpoint probe succeeded for {Ip}:{Port} in {Elapsed} ms.", ip, port, latency.TotalMilliseconds);

            return new EndpointProbeResult
            {
                Reachable = true,
                Message = $"TCP connection succeeded in {latency.TotalMilliseconds:F0} ms.",
                Latency = latency
            };
        }
        catch (SocketException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning("Endpoint probe failed for {Ip}:{Port}: {Reason}", ip, port, ex.Message);

            return new EndpointProbeResult { Reachable = false, Message = ex.Message };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("Endpoint probe timed out for {Ip}:{Port} after {Timeout} ms.", ip, port, timeoutMs);

            return new EndpointProbeResult { Reachable = false, Message = $"Timeout after {timeoutMs} ms." };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Endpoint probe error for {Ip}:{Port}.", ip, port);

            return new EndpointProbeResult { Reachable = false, Message = ex.Message };
        }
    }
}
