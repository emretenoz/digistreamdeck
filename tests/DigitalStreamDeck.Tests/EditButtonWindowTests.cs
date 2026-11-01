using System.IO;
using DigitalStreamDeck.Models;
using DigitalStreamDeck.Services;
using DigitalStreamDeck.Windows;

namespace DigitalStreamDeck.Tests;

[TestClass]
public sealed class EditButtonWindowTests
{
    [TestMethod]
    public void CreateValidatedCandidate_FailedSave_PreservesOriginalDeleteTitle()
    {
        var draft = new DeckButton { Title = "Original", Icon = "😀", Target = "https://example.com", ActionType = DeckActionType.Url };

        var candidate = EditButtonWindow.CreateValidatedCandidate(draft, "Unsaved", "😀", "#2563EB", DeckActionType.Url, "invalid", new ActionLauncher(), out var error);

        Assert.IsNull(candidate);
        Assert.IsNotNull(error);
        Assert.AreEqual("Original", draft.Title);
        Assert.AreEqual("https://example.com", draft.Target);
    }

    [TestMethod]
    public void CreateValidatedCandidate_ValidSave_ReturnsEditedClone()
    {
        var draft = new DeckButton { Title = "Original", Icon = "😀", Target = "https://old.example", ActionType = DeckActionType.Url };

        var candidate = EditButtonWindow.CreateValidatedCandidate(draft, " Updated ", "😀", "#2563EB", DeckActionType.Url, "https://example.com", new ActionLauncher(), out var error);

        Assert.IsNull(error);
        Assert.IsNotNull(candidate);
        Assert.AreEqual("Updated", candidate.Title);
        Assert.AreEqual("Original", draft.Title);
        Assert.AreEqual("", candidate.IconPath);
    }

    [TestMethod]
    public void ApplyBrowsedApplication_UpdatesDraftTargetTypeAndIconPath()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.lnk");
        try
        {
            File.WriteAllText(path, "");
            var draft = new DeckButton { ActionType = DeckActionType.Url, Target = "https://example.com" };
            EditButtonWindow.ApplyBrowsedApplication(draft, path);
            Assert.AreEqual(DeckActionType.Application, draft.ActionType);
            Assert.AreEqual(path, draft.Target);
            Assert.AreEqual(path, draft.IconPath);
        }
        finally { File.Delete(path); }
    }

    [TestMethod]
    [DataRow(".exe")]
    [DataRow(".lnk")]
    [DataRow(".EXE")]
    public void ResolveIconPath_ExistingApplication_ReturnsExactTarget(string extension)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");
        try
        {
            File.WriteAllText(path, "");
            Assert.AreEqual(path, EditButtonWindow.ResolveIconPath(DeckActionType.Application, path));
        }
        finally { File.Delete(path); }
    }

    [TestMethod]
    [DataRow(DeckActionType.Url, "https://example.com/test.exe")]
    [DataRow(DeckActionType.Application, "")]
    [DataRow(DeckActionType.Application, "missing.exe")]
    [DataRow(DeckActionType.Application, "unsupported.txt")]
    public void ResolveIconPath_NonApplicationOrInvalidTarget_ReturnsEmpty(DeckActionType type, string target)
    {
        Assert.AreEqual("", EditButtonWindow.ResolveIconPath(type, target));
    }

    [TestMethod]
    public void ResolveIconPath_ExistingUnsupportedFile_ReturnsEmpty()
    {
        var path = Path.GetTempFileName();
        try
        {
            Assert.AreEqual("", EditButtonWindow.ResolveIconPath(DeckActionType.Application, path));
        }
        finally { File.Delete(path); }
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("#123456")]
    public void ValidateColorSelection_MissingOrOutsidePalette_ReturnsTurkishError(string? selectedColor)
    {
        Assert.AreEqual("Paletten bir renk seçin.", EditButtonWindow.ValidateColorSelection(selectedColor));
    }

    [TestMethod]
    [DataRow("#2563EB")]
    [DataRow("#7C3AED")]
    [DataRow("#0891B2")]
    [DataRow("#EA580C")]
    [DataRow("#059669")]
    [DataRow("#DB2777")]
    public void ValidateColorSelection_AllowedColor_AcceptsSelection(string selectedColor)
    {
        Assert.IsNull(EditButtonWindow.ValidateColorSelection(selectedColor));
    }
}
