using DigitalStreamDeck.Services;

namespace DigitalStreamDeck.Tests;

[TestClass]
public sealed class DeckPaginationTests
{
    [TestMethod]
    [DataRow(0, 1, 0)]
    [DataRow(1, 1, 1)]
    [DataRow(6, 1, 6)]
    [DataRow(7, 2, 1)]
    [DataRow(12, 2, 6)]
    [DataRow(13, 3, 1)]
    public void GetPage_ReturnsOrderedClampedSliceWithoutChangingSource(int count, int pageCount, int lastCount)
    {
        var items = Enumerable.Range(0, count).ToArray();
        var original = items.ToArray();

        Assert.AreEqual(pageCount, DeckPagination.GetPageCount(count));
        var page = DeckPagination.GetPage(items, pageCount - 1);

        Assert.HasCount(lastCount, page);
        CollectionAssert.AreEqual(items.Skip((pageCount - 1) * 6).ToArray(), page.ToArray());
        CollectionAssert.AreEqual(original, items);
        CollectionAssert.AreEqual(items.Take(6).ToArray(), DeckPagination.GetPage(items, -10).ToArray());
        CollectionAssert.AreEqual(page.ToArray(), DeckPagination.GetPage(items, int.MaxValue).ToArray());
    }

    [TestMethod]
    [DataRow(-10, 13, 0)]
    [DataRow(10, 13, 2)]
    [DataRow(1, 0, 0)]
    [DataRow(1, 7, 1)]
    [DataRow(1, 6, 0)]
    public void ClampPage_ReturnsValidPage(int requested, int count, int expected)
    {
        Assert.AreEqual(expected, DeckPagination.ClampPage(requested, count));
    }

    [TestMethod]
    public void DeleteFromSevenItems_ClampsSecondPageBackToFirst()
    {
        var items = Enumerable.Range(0, 7).ToList();
        Assert.AreEqual(1, DeckPagination.ClampPage(1, items.Count));
        items.RemoveAt(6);
        Assert.AreEqual(0, DeckPagination.ClampPage(1, items.Count));
        CollectionAssert.AreEqual(items, DeckPagination.GetPage(items, 1).ToArray());
    }
}
