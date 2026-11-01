using System.Windows;

namespace DigitalStreamDeck.Services;

public static class WindowPositionNormalizer
{
    public static Point Normalize(double? left, double? top, Rect windowBounds, IReadOnlyList<Rect> workingAreas)
    {
        ArgumentNullException.ThrowIfNull(workingAreas);
        if (workingAreas.Count == 0) throw new ArgumentException("At least one working area is required.", nameof(workingAreas));

        var primary = workingAreas[0];
        var maxLeft = Math.Max(primary.Left, primary.Right - windowBounds.Width);
        var maxTop = Math.Max(primary.Top, primary.Bottom - windowBounds.Height);
        if (left is not double x || top is not double y || !double.IsFinite(x) || !double.IsFinite(y))
            return new Point((primary.Left + maxLeft) / 2, (primary.Top + maxTop) / 2);

        var candidate = new Rect(x, y, windowBounds.Width, windowBounds.Height);
        if (workingAreas.Any(area => area.Contains(candidate))) return new Point(x, y);

        return new Point(Math.Clamp(x, primary.Left, maxLeft), Math.Clamp(y, primary.Top, maxTop));
    }
}
