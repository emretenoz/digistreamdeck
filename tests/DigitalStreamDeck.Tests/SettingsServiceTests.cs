using System.IO;
using DigitalStreamDeck.Models;
using DigitalStreamDeck.Services;

namespace DigitalStreamDeck.Tests;

[TestClass]
public sealed class SettingsServiceTests
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "DigitalStreamDeck.Tests", Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    [TestMethod]
    public async Task LoadAsync_MissingFile_ReturnsDefaults()
    {
        var result = await new SettingsService(directory).LoadAsync();

        Assert.IsNull(result.Warning);
        Assert.HasCount(6, result.Settings.Buttons);
        Assert.AreEqual("Tarayıcı", result.Settings.Buttons[0].Title);
        Assert.IsFalse(File.Exists(Path.Combine(directory, "settings.json")));
    }

    [TestMethod]
    public async Task SaveThenLoad_PreservesButtonsAndPosition()
    {
        var service = new SettingsService(directory);
        await service.SaveAsync(DeckSettings.CreateDefault());
        var settings = DeckSettings.CreateDefault();
        settings.WindowLeft = -120.5;
        settings.WindowTop = 45.25;
        settings.Buttons[0] = new DeckButton
        {
            Id = "custom", Title = "Belgeler", Icon = "📁", Color = "#123456",
            ActionType = DeckActionType.Application, Target = @"C:\Apps\example.exe", IconPath = @"C:\Apps\example.exe"
        };
        settings.Buttons[1].ActionType = DeckActionType.Url;
        settings.Buttons[1].Target = "https://example.com";

        await service.SaveAsync(settings);
        var result = await service.LoadAsync();

        Assert.IsNull(result.Warning);
        Assert.HasCount(6, result.Settings.Buttons);
        Assert.AreEqual(settings.WindowLeft, result.Settings.WindowLeft);
        Assert.AreEqual(settings.WindowTop, result.Settings.WindowTop);
        for (var index = 0; index < 6; index++)
        {
            var expected = settings.Buttons[index];
            var actual = result.Settings.Buttons[index];
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Title, actual.Title);
            Assert.AreEqual(expected.Icon, actual.Icon);
            Assert.AreEqual(expected.Color, actual.Color);
            Assert.AreEqual(expected.ActionType, actual.ActionType);
            Assert.AreEqual(expected.Target, actual.Target);
            Assert.AreEqual(expected.IconPath, actual.IconPath);
        }
        Assert.IsFalse(File.Exists(Path.Combine(directory, "settings.json.tmp")));
    }

    [TestMethod]
    [DataRow("{ invalid json")]
    [DataRow("null")]
    [DataRow("{\"Buttons\":false}")]
    public async Task LoadAsync_CorruptJson_ReturnsDefaultsWithTurkishWarningAndPreservesFile(string corrupt)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        await File.WriteAllTextAsync(path, corrupt);

        var result = await new SettingsService(directory).LoadAsync();

        Assert.AreEqual("Ayarlar yüklenemedi; varsayılan düzen açıldı.", result.Warning);
        Assert.HasCount(6, result.Settings.Buttons);
        Assert.AreEqual("Tarayıcı", result.Settings.Buttons[0].Title);
        Assert.AreEqual(corrupt, await File.ReadAllTextAsync(path));
    }

    [TestMethod]
    public async Task SaveAfterCorruptLoad_PreservesOriginalBytesOnceAndWritesValidDefaults()
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        byte[] original = [0xEF, 0xBB, 0xBF, 0x7B, 0xFF, 0x7D];
        await File.WriteAllBytesAsync(path, original);
        var service = new SettingsService(directory);
        var loaded = await service.LoadAsync();

        Assert.IsNotNull(loaded.Warning);
        await service.SaveAsync(loaded.Settings);
        await service.SaveAsync(loaded.Settings);

        var backups = Directory.GetFiles(directory, "settings.json.corrupt-*");
        Assert.HasCount(1, backups);
        CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(backups[0]));
        var reloaded = await service.LoadAsync();
        Assert.IsNull(reloaded.Warning);
        Assert.AreEqual("Tarayıcı", reloaded.Settings.Buttons[0].Title);
        Assert.HasCount(6, reloaded.Settings.Buttons);
    }

    [TestMethod]
    public async Task SaveAfterCorruptLoad_WhenOriginalCannotBeCopied_DoesNotReplaceIt()
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        byte[] original = [0x7B, 0xFF, 0x7D];
        await File.WriteAllBytesAsync(path, original);
        var service = new SettingsService(directory);
        var loaded = await service.LoadAsync();

        using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            try
            {
                await service.SaveAsync(loaded.Settings);
                Assert.Fail("Save must fail when the original cannot be copied.");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
        }

        CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(path));
    }

    [TestMethod]
    public async Task LoadAsync_UnreadableFile_ReturnsDefaultsWithWarning()
    {
        Directory.CreateDirectory(Path.Combine(directory, "settings.json"));

        var result = await new SettingsService(directory).LoadAsync();

        Assert.AreEqual("Ayarlar yüklenemedi; varsayılan düzen açıldı.", result.Warning);
        Assert.HasCount(6, result.Settings.Buttons);
        Assert.IsTrue(Directory.Exists(Path.Combine(directory, "settings.json")));
    }

    [TestMethod]
    public async Task LoadAndSave_Cancelled_PropagateCancellationAndPreserveSavedFile()
    {
        var service = new SettingsService(directory);
        await service.SaveAsync(DeckSettings.CreateDefault());
        var path = Path.Combine(directory, "settings.json");
        var original = await File.ReadAllTextAsync(path);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.LoadAsync(cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.SaveAsync(new DeckSettings(), cancellation.Token));

        Assert.AreEqual(original, await File.ReadAllTextAsync(path));
    }

    [TestMethod]
    public async Task LoadAsync_LegacyJsonWithoutIconPath_UsesEmptyFallback()
    {
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "settings.json"),
            """{"Buttons":[{"Id":"custom","Title":"Özel","Icon":"X","Color":"#123456"}],"WindowLeft":20}""");

        var result = await new SettingsService(directory).LoadAsync();

        Assert.IsNull(result.Warning);
        Assert.HasCount(1, result.Settings.Buttons);
        Assert.AreEqual("custom", result.Settings.Buttons[0].Id);
        Assert.AreEqual("Özel", result.Settings.Buttons[0].Title);
        Assert.AreEqual(20d, result.Settings.WindowLeft);
        Assert.AreEqual("", result.Settings.Buttons[0].IconPath);
    }

    [TestMethod]
    public async Task SaveThenLoad_EmptyButtons_RemainsEmpty()
    {
        var service = new SettingsService(directory);
        await service.SaveAsync(new DeckSettings());
        var result = await service.LoadAsync();
        Assert.IsNull(result.Warning);
        Assert.HasCount(0, result.Settings.Buttons);
    }
}
