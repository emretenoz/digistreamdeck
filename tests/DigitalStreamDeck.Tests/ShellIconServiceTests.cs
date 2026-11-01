using DigitalStreamDeck.Services;

namespace DigitalStreamDeck.Tests;

[TestClass]
public sealed class ShellIconServiceTests
{
    [TestMethod]
    public void GetIcon_NullMissingOrDirectory_ReturnsNull()
    {
        var service = new ShellIconService();

        Assert.IsNull(service.GetIcon(null));
        Assert.IsNull(service.GetIcon(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));
        Assert.IsNull(service.GetIcon(Path.GetTempPath()));
    }

    [TestMethod]
    public void GetIcon_KnownWindowsExecutable_ReturnsFrozenImage()
    {
        var icon = new ShellIconService().GetIcon(KnownExecutable());

        Assert.IsNotNull(icon);
        Assert.IsTrue(icon.IsFrozen);
    }

    [TestMethod]
    public void GetIcon_RepeatedPath_ReturnsCachedInstance()
    {
        var service = new ShellIconService();
        var path = KnownExecutable();
        var icon = service.GetIcon(path);

        Assert.IsNotNull(icon);
        Assert.AreSame(icon, service.GetIcon(path.ToUpperInvariant()));
        service.Clear(path);
        var refreshedIcon = service.GetIcon(path);
        Assert.IsNotNull(refreshedIcon);
        Assert.AreNotSame(icon, refreshedIcon);
    }

    [TestMethod]
    public void GetIcon_MissingFile_CachesNullUntilCleared()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.exe");
        var service = new ShellIconService();
        try
        {
            Assert.IsNull(service.GetIcon(path));
            File.Copy(KnownExecutable(), path);
            Assert.IsNull(new ShellIconService().GetIcon(path));
            service.Clear(path);
            Assert.IsNotNull(service.GetIcon(path));
        }
        finally
        {
            service.Clear(path);
            File.Delete(path);
        }
    }
    private static string KnownExecutable()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "notepad.exe");
        if (!File.Exists(path))
        {
            Assert.Inconclusive($"Windows fixture does not exist: {path}");
        }

        return path;
    }
}
