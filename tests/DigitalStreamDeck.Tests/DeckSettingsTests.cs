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
        Assert.HasCount(6, settings.Buttons);
        CollectionAssert.AreEqual(settings.Buttons.Select(button => button.Id).ToArray(),
            DeckSettings.CreateDefault().Buttons.Select(button => button.Id).ToArray());
        Assert.AreEqual(6, settings.Buttons.Distinct().Count());
    }

    [TestMethod]
    public void Normalize_NullButtons_RemainsEmpty()
    {
        var settings = new DeckSettings { Buttons = null! };

        settings.Normalize();

        Assert.HasCount(0, settings.Buttons);
    }

    [TestMethod]
    public void Normalize_MoreThanSix_PreservesAllInOrder()
    {
        var settings = DeckSettings.CreateDefault();
        settings.Buttons.AddRange(Enumerable.Range(6, 7).Select(index => new DeckButton { Id = $"extra-{index}" }));
        var original = settings.Buttons.ToArray();

        settings.Normalize();

        AssertUsable(settings);
        CollectionAssert.AreEqual(original, settings.Buttons);
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
        Assert.HasCount(1, settings.Buttons);
        Assert.AreEqual("Tarayıcı", settings.Buttons[0].Title);
        Assert.IsFalse(string.IsNullOrWhiteSpace(settings.Buttons[0].Icon));
        Assert.IsNotNull(settings.Buttons[0].Target);
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

    [TestMethod]
    public void Normalize_EmptyList_RemainsEmpty()
    {
        var settings = new DeckSettings();
        settings.Normalize();
        Assert.HasCount(0, settings.Buttons);
    }

    [TestMethod]
    public void Normalize_ArbitraryEntries_RepairsStableUniqueIds()
    {
        var entries = Enumerable.Range(0, 13).Select(_ => new DeckButton
        {
            Id = "duplicate", Title = null!, Icon = null!, Color = null!, Target = null!, IconPath = null!
        }).ToList();
        entries[0].Id = "";
        entries[2].Id = "button-1";
        var settings = new DeckSettings { Buttons = entries };

        settings.Normalize();
        var ids = settings.Buttons.Select(button => button.Id).ToArray();
        settings.Normalize();

        AssertUsable(settings);
        Assert.HasCount(13, settings.Buttons);
        Assert.AreEqual("button-1", entries[2].Id);
        Assert.AreEqual("duplicate", entries[1].Id);
        CollectionAssert.AreEqual(ids, settings.Buttons.Select(button => button.Id).ToArray());
        for (var index = 0; index < entries.Count; index++)
        {
            Assert.AreEqual(index < 6 ? DeckSettings.CreateDefault().Buttons[index].Title : "Yeni tuş", entries[index].Title);
            Assert.AreEqual(index < 6 ? DeckSettings.CreateDefault().Buttons[index].Icon : "+", entries[index].Icon);
            Assert.AreEqual(index < 6 ? DeckSettings.CreateDefault().Buttons[index].Color : "#2563EB", entries[index].Color);
            Assert.AreEqual("", entries[index].Target);
            Assert.AreEqual("", entries[index].IconPath);
        }
    }

    [TestMethod]
    public void Clone_PreservesIconPath()
    {
        var button = new DeckButton { IconPath = @"C:\Apps\example.exe" };
        var clone = button.Clone();
        Assert.AreEqual(button.IconPath, clone.IconPath);
        clone.IconPath = "changed";
        Assert.AreEqual(@"C:\Apps\example.exe", button.IconPath);
    }

    [TestMethod]
    public void CreateNewButton_ReturnsUniqueUsableDraft()
    {
        var button = DeckSettings.CreateNewButton();
        var other = DeckSettings.CreateNewButton();
        Assert.IsTrue(Guid.TryParseExact(button.Id, "N", out _));
        Assert.AreNotEqual(button.Id, other.Id);
        Assert.AreEqual("Yeni tuş", button.Title);
        Assert.AreEqual("+", button.Icon);
        Assert.AreEqual("#2563EB", button.Color);
        Assert.AreEqual(DeckActionType.None, button.ActionType);
        Assert.AreEqual("", button.Target);
        Assert.AreEqual("", button.IconPath);
    }

    private static void AssertUsable(DeckSettings settings)
    {
        foreach (var button in settings.Buttons)
        {
            Assert.IsNotNull(button);
            Assert.IsFalse(string.IsNullOrWhiteSpace(button.Id));
            Assert.IsFalse(string.IsNullOrWhiteSpace(button.Title));
            Assert.IsFalse(string.IsNullOrWhiteSpace(button.Color));
        }
        Assert.AreEqual(settings.Buttons.Count, settings.Buttons.Select(button => button.Id).Distinct().Count());
    }
}
