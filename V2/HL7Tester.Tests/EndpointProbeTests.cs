using System.Net;
using System.Net.Sockets;
using HL7Tester.Core;

namespace HL7Tester.Tests;

[TestClass]
public sealed class EndpointProbeTests
{
    private static EndpointProbe Probe() => new();

    [TestMethod]
    public async Task TestTcpAsync_LocalListener_IsReachable()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var result = await Probe().TestTcpAsync("127.0.0.1", port);

        Assert.IsTrue(result.Reachable, "Connecting to a local listener should succeed.");
        Assert.IsTrue(result.Latency >= TimeSpan.Zero);
        Assert.Contains("succeeded", result.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task TestTcpAsync_ClosedPort_IsUnreachable()
    {
        // Bind a listener on an ephemeral port, then stop it so the port is closed.
        int port;
        using (var listener = new TcpListener(IPAddress.Loopback, 0))
        {
            listener.Start();
            port = ((IPEndPoint)listener.LocalEndpoint).Port;
        }

        var result = await Probe().TestTcpAsync("127.0.0.1", port, timeoutMs: 3000);

        Assert.IsFalse(result.Reachable, "A closed port must be reported as unreachable.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message), "Failure must carry a message.");
    }

    [TestMethod]
    public async Task TestTcpAsync_UnroutableHost_TimesOut()
    {
        // 192.0.2.0/24 (TEST-NET-1) is reserved for documentation and unroutable:
        // the connection either times out or fails fast — both must be Unreachable.
        var result = await Probe().TestTcpAsync("192.0.2.1", 65500, timeoutMs: 500);

        Assert.IsFalse(result.Reachable, "An unroutable host must be reported as unreachable.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message), "Failure must carry a message.");
    }

    [TestMethod]
    public async Task TestTcpAsync_InvalidPort_IsUnreachable()
    {
        var result = await Probe().TestTcpAsync("127.0.0.1", 99999);

        Assert.IsFalse(result.Reachable);
        Assert.AreEqual("Invalid port.", result.Message);
    }

    [TestMethod]
    public async Task TestTcpAsync_EmptyIp_IsUnreachable()
    {
        var result = await Probe().TestTcpAsync(" ", 70);

        Assert.IsFalse(result.Reachable);
        Assert.AreEqual("Invalid IP address.", result.Message);
    }
}
