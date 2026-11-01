namespace DigitalStreamDeck.Services;

public static class DeckPagination
{
    public const int PageSize = 6;

    public static int GetPageCount(int itemCount) => itemCount <= 0 ? 1 : (itemCount - 1) / PageSize + 1;

    public static int ClampPage(int requestedPage, int itemCount) => Math.Clamp(requestedPage, 0, GetPageCount(itemCount) - 1);

    public static IReadOnlyList<T> GetPage<T>(IReadOnlyList<T> items, int pageIndex) =>
        items.Skip(ClampPage(pageIndex, items.Count) * PageSize).Take(PageSize).ToArray();
}
