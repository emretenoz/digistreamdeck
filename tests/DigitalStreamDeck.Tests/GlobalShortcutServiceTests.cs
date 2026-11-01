using DigitalStreamDeck.Services;

namespace DigitalStreamDeck.Tests;

[TestClass]
public sealed class GlobalShortcutServiceTests
{
    [TestMethod]
    public void ToggleSubscriberThrows_GestureRemainsConsumed()
    {
        using var service = new GlobalShortcutService();
        service.ToggleRequested += (_, _) => throw new InvalidOperationException("subscriber failed");

        Assert.AreEqual((nint)1, service.DispatchDecision(ShortcutDecision.ToggleAndConsume));
    }
}
