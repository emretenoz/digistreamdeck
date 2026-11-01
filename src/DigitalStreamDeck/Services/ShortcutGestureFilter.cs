namespace DigitalStreamDeck.Services;

public enum ShortcutDecision { PassThrough, Consume, ToggleAndConsume }

public sealed class ShortcutGestureFilter
{
    private const int WmXButtonDown = 0x020B;
    private const int WmXButtonUp = 0x020C;
    private bool capturedDown;
    private long? lastToggleMilliseconds;

    public ShortcutDecision Process(int message, uint mouseData, bool altDown, long timestampMilliseconds)
    {
        if ((mouseData >> 16) != 1) return ShortcutDecision.PassThrough;

        if (message == WmXButtonUp && capturedDown)
        {
            capturedDown = false;
            return ShortcutDecision.Consume;
        }

        if (message != WmXButtonDown) return ShortcutDecision.PassThrough;
        if (capturedDown) return ShortcutDecision.Consume;
        if (!altDown) return ShortcutDecision.PassThrough;

        capturedDown = true;
        if (lastToggleMilliseconds is long last && timestampMilliseconds - last < 250)
            return ShortcutDecision.Consume;

        lastToggleMilliseconds = timestampMilliseconds;
        return ShortcutDecision.ToggleAndConsume;
    }
}
