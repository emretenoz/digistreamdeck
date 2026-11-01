using System.Windows.Media;
using DigitalStreamDeck.Models;

namespace DigitalStreamDeck.Presentation;

public sealed record DeckTileViewModel(DeckButton Button, ImageSource? IconImage)
{
    public string Title => Button.Title;
    public string Icon => Button.Icon;
    public string Color => Button.Color;
}
