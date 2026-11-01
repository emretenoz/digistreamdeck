using DigitalStreamDeck.Windows;

namespace DigitalStreamDeck.Tests;

[TestClass]
public sealed class EditButtonWindowTests
{
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
