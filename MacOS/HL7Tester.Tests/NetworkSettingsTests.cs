using HL7Tester.Core;

namespace HL7Tester.Tests;

[TestClass]
public sealed class NetworkSettingsTests
{
    [TestMethod]
    public async Task FileNetworkSettingsService_CanPersistAndRestoreSettingsWithHistory()
    {
        // Arrange
        string tempFile = Path.Combine(Path.GetTempPath(), $"hl7tester_network_{Guid.NewGuid():N}.json");
        try
        {
            var service = new FileNetworkSettingsService(tempFile);

            var settings = new NetworkSettings();
            service.AddToHistory(settings, "127.0.0.1", "2575", maxEntries: 3);
            service.AddToHistory(settings, "10.0.0.1", "1234", maxEntries: 3);

            await service.SaveAsync(settings);

            // Act
            var reloaded = await service.LoadAsync();

            // Assert
            Assert.AreEqual("10.0.0.1", reloaded.LastIpAddress);
            Assert.AreEqual("1234", reloaded.LastPort);
            Assert.AreEqual(2, reloaded.History.Count);
            Assert.AreEqual("10.0.0.1", reloaded.History[0].Ip);
            Assert.AreEqual("1234", reloaded.History[0].Port);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
