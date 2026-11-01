using System.IO;
using DigitalStreamDeck.Models;
using DigitalStreamDeck.Windows;

namespace DigitalStreamDeck.Tests;

[TestClass]
public sealed class EditButtonWindowTests
{
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
