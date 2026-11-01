using DigitalStreamDeck.Models;

namespace DigitalStreamDeck.Tests;

[TestClass]
public sealed class DeckSettingsTests
{
    [TestMethod]
    public void CreateDefault_ReturnsSixDistinctButtons()
    {
        var settings = DeckSettings.CreateDefault();

        AssertUsable(settings);
        CollectionAssert.AreEqual(settings.Buttons.Select(button => button.Id).ToArray(),
            DeckSettings.CreateDefault().Buttons.Select(button => button.Id).ToArray());
        Assert.AreEqual(6, settings.Buttons.Distinct().Count());
    }

    [TestMethod]
    public void Normalize_NullButtons_ReturnsSixUsableButtons()
    {
        var settings = new DeckSettings { Buttons = null! };

        settings.Normalize();

        AssertUsable(settings);
    }

    [TestMethod]
    public void Normalize_TrimsExtraButtonsToSix()
    {
        var settings = DeckSettings.CreateDefault();
        var firstSix = settings.Buttons.ToArray();
        settings.Buttons.Add(new DeckButton { Id = "extra" });

        settings.Normalize();

        AssertUsable(settings);
        CollectionAssert.AreEqual(firstSix, settings.Buttons);
    }

    [TestMethod]
    public void Normalize_RepairsNullEntriesAndMissingFields()
    {
        var settings = new DeckSettings
        {
            Buttons = [null!, new DeckButton { Id = "", Title = " ", Icon = null!, Color = null!, Target = null! }]
        };

        settings.Normalize();

        AssertUsable(settings);
        Assert.IsFalse(string.IsNullOrWhiteSpace(settings.Buttons[1].Icon));
        Assert.IsNotNull(settings.Buttons[1].Target);
    }

    [TestMethod]
    public void Normalize_PreservesValidUserEntriesAndWindowPosition()
    {
        var button = new DeckButton
        {
            Id = "custom", Title = "Belgeler", Icon = "D", Color = "#123456",
            ActionType = DeckActionType.Application, Target = @"C:\Apps\example.exe"
        };
        var settings = new DeckSettings { Buttons = [button], WindowLeft = 120, WindowTop = 45 };

        settings.Normalize();
        var ids = settings.Buttons.Select(item => item.Id).ToArray();
        settings.Normalize();

        AssertUsable(settings);
        Assert.AreSame(button, settings.Buttons[0]);
        Assert.AreEqual("Belgeler", button.Title);
        Assert.AreEqual("D", button.Icon);
        Assert.AreEqual("#123456", button.Color);
        Assert.AreEqual(DeckActionType.Application, button.ActionType);
        Assert.AreEqual(@"C:\Apps\example.exe", button.Target);
        Assert.AreEqual(120d, settings.WindowLeft);
        Assert.AreEqual(45d, settings.WindowTop);
        CollectionAssert.AreEqual(ids, settings.Buttons.Select(item => item.Id).ToArray());
    }

    [TestMethod]
    public void Normalize_RepairsDuplicateIdsAndKeepsThemStable()
    {
        var settings = new DeckSettings
        {
            Buttons = [new DeckButton { Id = "button-3" }, new DeckButton { Id = "button-3" }]
        };

        settings.Normalize();
        var ids = settings.Buttons.Select(button => button.Id).ToArray();
        settings.Normalize();

        AssertUsable(settings);
        Assert.AreEqual("button-3", settings.Buttons[0].Id);
        CollectionAssert.AreEqual(ids, settings.Buttons.Select(button => button.Id).ToArray());
    }

    [TestMethod]
    public void Normalize_MissingEntryDoesNotStealExistingUserId()
    {
        var custom = new DeckButton { Id = "button-1", Title = "Custom", Icon = "C", Color = "#123456" };
        var settings = new DeckSettings { Buttons = [null!, custom] };

        settings.Normalize();

        AssertUsable(settings);
        Assert.AreEqual("button-1", custom.Id);
    }

    [TestMethod]
    public void Clone_ReturnsIndependentCopyOfAllFields()
    {
        var button = new DeckButton
        {
            Id = "custom", Title = "Web", Icon = "W", Color = "#123456",
            ActionType = DeckActionType.Url, Target = "https://example.com"
        };

        var clone = button.Clone();

        Assert.AreNotSame(button, clone);
        Assert.AreEqual(button.Id, clone.Id);
        Assert.AreEqual(button.Title, clone.Title);
        Assert.AreEqual(button.Icon, clone.Icon);
        Assert.AreEqual(button.Color, clone.Color);
        Assert.AreEqual(button.ActionType, clone.ActionType);
        Assert.AreEqual(button.Target, clone.Target);
        clone.Title = "Changed";
        Assert.AreEqual("Web", button.Title);
    }

    private static void AssertUsable(DeckSettings settings)
    {
        Assert.HasCount(6, settings.Buttons);
        foreach (var button in settings.Buttons)
        {
            Assert.IsNotNull(button);
            Assert.IsFalse(string.IsNullOrWhiteSpace(button.Id));
            Assert.IsFalse(string.IsNullOrWhiteSpace(button.Title));
            Assert.IsFalse(string.IsNullOrWhiteSpace(button.Color));
        }
        Assert.AreEqual(6, settings.Buttons.Select(button => button.Id).Distinct().Count());
    }
}
