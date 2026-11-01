using DigitalStreamDeck.Services;

namespace DigitalStreamDeck.Tests;

[TestClass]
public sealed class ShortcutGestureFilterTests
{
    private const int Down = 0x020B;
    private const int Up = 0x020C;
    private const uint XButton1 = 0x00010000;
    private const uint XButton2 = 0x00020000;

    [TestMethod]
    public void AltXButton1Down_TogglesAndConsumes()
    {
        var filter = new ShortcutGestureFilter();
        Assert.AreEqual(ShortcutDecision.ToggleAndConsume, filter.Process(Down, XButton1, true, 1000));
    }

    [TestMethod]
    public void CapturedUp_IsConsumedAfterAltReleased()
    {
        var filter = new ShortcutGestureFilter();
        filter.Process(Down, XButton1, true, 1000);
        Assert.AreEqual(ShortcutDecision.Consume, filter.Process(Up, XButton1, false, 1010));
        Assert.AreEqual(ShortcutDecision.PassThrough, filter.Process(Up, XButton1, false, 1020));
    }

    [TestMethod]
    public void RepeatedDownWithinDebounce_IsConsumedWithoutToggling()
    {
        var filter = new ShortcutGestureFilter();
        filter.Process(Down, XButton1, true, 1000);
        filter.Process(Up, XButton1, true, 1010);
        Assert.AreEqual(ShortcutDecision.Consume, filter.Process(Down, XButton1, true, 1249));
        Assert.AreEqual(ShortcutDecision.Consume, filter.Process(Up, XButton1, false, 1250));
        Assert.AreEqual(ShortcutDecision.ToggleAndConsume, filter.Process(Down, XButton1, true, 1250));
    }

    [TestMethod]
    public void RepeatedDownWhileCaptured_DoesNotToggleEvenAfterDebounce()
    {
        var filter = new ShortcutGestureFilter();
        filter.Process(Down, XButton1, true, 1000);
        Assert.AreEqual(ShortcutDecision.Consume, filter.Process(Down, XButton1, false, 1300));
    }

    [TestMethod]
    public void XButton1WithoutAlt_PassesThrough()
    {
        var filter = new ShortcutGestureFilter();
        Assert.AreEqual(ShortcutDecision.PassThrough, filter.Process(Down, XButton1, false, 1000));
        Assert.AreEqual(ShortcutDecision.PassThrough, filter.Process(Up, XButton1, true, 1010));
    }

    [TestMethod]
    public void OtherButtonsAndMouseMessages_PassThrough()
    {
        var filter = new ShortcutGestureFilter();
        Assert.AreEqual(ShortcutDecision.PassThrough, filter.Process(Down, XButton2, true, 1000));
        Assert.AreEqual(ShortcutDecision.PassThrough, filter.Process(Up, XButton2, true, 1010));
        Assert.AreEqual(ShortcutDecision.PassThrough, filter.Process(0x0201, XButton1, true, 1020));
    }
}
