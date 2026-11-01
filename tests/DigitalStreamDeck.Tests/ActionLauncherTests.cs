using System.Diagnostics;
using System.IO;
using DigitalStreamDeck.Models;
using DigitalStreamDeck.Services;

namespace DigitalStreamDeck.Tests;

[TestClass]
public sealed class ActionLauncherTests
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "DigitalStreamDeck.Tests", Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    [TestMethod]
    [DataRow("sample.exe")]
    [DataRow("sample.EXE")]
    [DataRow("sample.lnk")]
    [DataRow("sample.LNK")]
    public void Validate_ExistingApplication_AcceptsCaseInsensitiveExtension(string name)
    {
        var button = ValidButton();
        button.ActionType = DeckActionType.Application;
        button.Target = ExistingFile(name);
        Assert.IsNull(new ActionLauncher().Validate(button));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("missing.exe")]
    [DataRow("missing.lnk")]
    public void Validate_MissingApplication_ReturnsFriendlyError(string target)
    {
        var button = ValidButton();
        button.ActionType = DeckActionType.Application;
        button.Target = Path.Combine(directory, target);
        Assert.AreEqual("Mevcut bir .exe veya .lnk dosyası seçin.", new ActionLauncher().Validate(button));
    }

    [TestMethod]
    [DataRow("sample.txt")]
    [DataRow("sample.url")]
    [DataRow("sample")]
    public void Validate_ExistingUnsupportedFile_RejectsTarget(string name)
    {
        var button = ValidButton();
        button.ActionType = DeckActionType.Application;
        button.Target = ExistingFile(name);
        Assert.AreEqual("Mevcut bir .exe veya .lnk dosyası seçin.", new ActionLauncher().Validate(button));
    }

    [TestMethod]
    [DataRow("folder.exe")]
    [DataRow("folder.lnk")]
    public void Validate_Directory_RejectsTarget(string name)
    {
        var button = ValidButton();
        button.ActionType = DeckActionType.Application;
        button.Target = Path.Combine(directory, name);
        Directory.CreateDirectory(button.Target);
        Assert.AreEqual("Mevcut bir .exe veya .lnk dosyası seçin.", new ActionLauncher().Validate(button));
    }

    [TestMethod]
    [DataRow("https://example.com/path?q=hello#section")]
    [DataRow("http://example.com")]
    public void Validate_AbsoluteHttpUrl_AcceptsTarget(string target)
    {
        var button = ValidButton();
        button.Target = target;
        Assert.IsNull(new ActionLauncher().Validate(button));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("example.com")]
    [DataRow("/relative")]
    [DataRow("file:///C:/sample.exe")]
    [DataRow("ftp://example.com")]
    [DataRow("javascript:alert(1)")]
    public void Validate_UnsupportedUrl_ReturnsFriendlyError(string target)
    {
        var button = ValidButton();
        button.Target = target;
        Assert.AreEqual("Geçerli bir http veya https adresi girin.", new ActionLauncher().Validate(button));
    }

    [TestMethod]
    [DataRow(DeckActionType.None)]
    [DataRow((DeckActionType)99)]
    public void Validate_UnconfiguredAction_ReturnsFriendlyError(DeckActionType actionType)
    {
        var button = ValidButton();
        button.ActionType = actionType;
        Assert.AreEqual("Bir eylem türü seçin.", new ActionLauncher().Validate(button));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("1234567890123456789012345")]
    public void Validate_InvalidTitle_ReturnsFriendlyError(string title)
    {
        var button = ValidButton();
        button.Title = title;
        Assert.AreEqual("Başlık 1–24 karakter olmalıdır.", new ActionLauncher().Validate(button));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("ABCDE")]
    [DataRow("👨‍👩‍👧‍👦👨‍👩‍👧‍👦👨‍👩‍👧‍👦👨‍👩‍👧‍👦👨‍👩‍👧‍👦")]
    public void Validate_InvalidIcon_ReturnsFriendlyError(string icon)
    {
        var button = ValidButton();
        button.Icon = icon;
        Assert.AreEqual("Simge 1–4 metin öğesi olmalıdır.", new ActionLauncher().Validate(button));
    }

    [TestMethod]
    [DataRow("X", "A")]
    [DataRow("123456789012345678901234", "👨‍👩‍👧‍👦🇹🇷é🎮")]
    public void Validate_TitleAndUnicodeIconAtLimits_AcceptsConfiguration(string title, string icon)
    {
        var button = ValidButton();
        button.Title = title;
        button.Icon = icon;
        Assert.IsNull(new ActionLauncher().Validate(button));
    }

    [TestMethod]
    [DataRow(DeckActionType.Application, "sample.exe")]
    [DataRow(DeckActionType.Application, "sample.lnk")]
    [DataRow(DeckActionType.Application, "sample.LNK")]
    [DataRow(DeckActionType.Url, "")]
    public void Launch_ValidTarget_StartsExactTargetThroughShell(DeckActionType actionType, string name)
    {
        var button = ValidButton();
        button.ActionType = actionType;
        if (actionType == DeckActionType.Application) button.Target = ExistingFile(name);
        ProcessStartInfo? captured = null;
        var launcher = new ActionLauncher(info => { captured = info; return null; });

        var result = launcher.Launch(button);

        Assert.IsTrue(result.Succeeded);
        Assert.IsNull(result.Error);
        Assert.IsNotNull(captured);
        Assert.AreEqual(button.Target, captured.FileName);
        Assert.IsTrue(captured.UseShellExecute);
        Assert.AreEqual("", captured.Arguments);
    }

    [TestMethod]
    public void Launch_InvalidConfiguration_ReturnsValidationErrorWithoutStarting()
    {
        var started = false;
        var launcher = new ActionLauncher(_ => { started = true; return null; });
        var button = ValidButton();
        button.Target = "file:///C:/sample.exe";

        var result = launcher.Launch(button);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Geçerli bir http veya https adresi girin.", result.Error);
        Assert.IsFalse(started);
    }

    [TestMethod]
    public void Launch_StarterThrows_ReturnsFriendlyErrorWithoutEscaping()
    {
        var launcher = new ActionLauncher(_ => throw new InvalidOperationException("private details"));
        var result = launcher.Launch(ValidButton());
        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Hedef açılamadı. Dosya veya adresi kontrol edin.", result.Error);
    }

    private static DeckButton ValidButton() => new()
    {
        Title = "Tarayıcı", Icon = "🌐", ActionType = DeckActionType.Url, Target = "https://example.com"
    };

    private string ExistingFile(string name)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, name);
        File.WriteAllText(path, "test fixture; never executed");
        return path;
    }
}
