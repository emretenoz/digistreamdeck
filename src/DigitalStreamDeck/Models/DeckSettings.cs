namespace DigitalStreamDeck.Models;

public sealed class DeckSettings
{
    public List<DeckButton> Buttons { get; set; } = [];
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }

    public static DeckSettings CreateDefault() => new()
    {
        Buttons =
        [
            new() { Id = "button-1", Title = "Tarayıcı", Icon = "🌐", Color = "#2563EB" },
            new() { Id = "button-2", Title = "Müzik", Icon = "♫", Color = "#7C3AED" },
            new() { Id = "button-3", Title = "Sohbet", Icon = "💬", Color = "#0891B2" },
            new() { Id = "button-4", Title = "Oyun", Icon = "🎮", Color = "#EA580C" },
            new() { Id = "button-5", Title = "Notlar", Icon = "✎", Color = "#059669" },
            new() { Id = "button-6", Title = "Araçlar", Icon = "⚙", Color = "#DB2777" }
        ]
    };

    public void Normalize()
    {
        var defaults = CreateDefault().Buttons;
        Buttons ??= [];
        if (Buttons.Count > 6)
        {
            Buttons.RemoveRange(6, Buttons.Count - 6);
        }

        var existingIds = Buttons.Where(button => button is not null && !string.IsNullOrWhiteSpace(button.Id))
            .Select(button => button.Id).ToHashSet(StringComparer.Ordinal);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < 6; index++)
        {
            var fallback = defaults[index];
            var isNew = index == Buttons.Count || Buttons[index] is null;
            if (index == Buttons.Count)
            {
                Buttons.Add(fallback.Clone());
            }

            var button = Buttons[index] ??= fallback.Clone();
            if (string.IsNullOrWhiteSpace(button.Id) || (isNew && existingIds.Contains(button.Id)) || !ids.Add(button.Id))
            {
                button.Id = fallback.Id;
                while (existingIds.Contains(button.Id) || !ids.Add(button.Id))
                {
                    button.Id = Guid.NewGuid().ToString("N");
                }
            }

            if (string.IsNullOrWhiteSpace(button.Title)) button.Title = fallback.Title;
            if (string.IsNullOrWhiteSpace(button.Icon)) button.Icon = fallback.Icon;
            if (string.IsNullOrWhiteSpace(button.Color)) button.Color = fallback.Color;
            button.Target ??= "";
        }
    }
}
