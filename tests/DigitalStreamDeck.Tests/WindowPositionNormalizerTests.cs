using System.Windows;
using DigitalStreamDeck.Services;

namespace DigitalStreamDeck.Tests;

[TestClass]
public sealed class WindowPositionNormalizerTests
{
    private static readonly Rect Overlay = new(0, 0, 440, 320);
    private static readonly Rect Primary = new(0, 0, 1920, 1040);

    [TestMethod]
    public void Normalize_FullyVisibleCoordinates_PreservesPosition()
    {
        Assert.AreEqual(new Point(120, 80), WindowPositionNormalizer.Normalize(120, 80, Overlay, [Primary]));
    }

    [TestMethod]
    public void Normalize_NullCoordinates_CentersInPrimaryWorkingArea()
    {
        Assert.AreEqual(new Point(740, 360), WindowPositionNormalizer.Normalize(null, null, Overlay, [Primary]));
    }

    [TestMethod]
    public void Normalize_DisconnectedMonitor_ClampsIntoPrimaryWorkingArea()
    {
        Assert.AreEqual(new Point(1480, 720), WindowPositionNormalizer.Normalize(2500, 1200, Overlay, [Primary]));
    }

    [TestMethod]
    public void Normalize_TopLeftVisibleButWindowClipped_ClampsEntireRectangle()
    {
        Assert.AreEqual(new Point(1480, 720), WindowPositionNormalizer.Normalize(1850, 1000, Overlay, [Primary]));
    }

    [TestMethod]
    public void Normalize_FullyVisibleOnNegativeCoordinateMonitor_PreservesPosition()
    {
        Assert.AreEqual(new Point(-1200, 40), WindowPositionNormalizer.Normalize(-1200, 40, Overlay,
            [Primary, new Rect(-1920, 0, 1920, 1040)]));
    }

    [TestMethod]
    public void Normalize_NonFiniteCoordinates_CentersWindow()
    {
        Assert.AreEqual(new Point(740, 360), WindowPositionNormalizer.Normalize(double.NaN, double.PositiveInfinity, Overlay, [Primary]));
    }

    [TestMethod]
    public void Normalize_WorkAreaSmallerThanWindow_KeepsTitleBarAccessible()
    {
        Assert.AreEqual(new Point(10, 20), WindowPositionNormalizer.Normalize(900, 900, Overlay, [new Rect(10, 20, 300, 200)]));
    }
}
