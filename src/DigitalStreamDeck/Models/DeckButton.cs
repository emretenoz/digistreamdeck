namespace DigitalStreamDeck.Models;

public sealed class DeckButton
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Color { get; set; } = "";
    public DeckActionType ActionType { get; set; }
    public string Target { get; set; } = "";

    public DeckButton Clone() => (DeckButton)MemberwiseClone();
}
