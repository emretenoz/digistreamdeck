using System.Collections.ObjectModel;
using System.Windows;
using DigitalStreamDeck.Models;
using DigitalStreamDeck.Services;
using Microsoft.Win32;

namespace DigitalStreamDeck.Windows;

public partial class EditButtonWindow : Window
{
    public static IReadOnlyDictionary<string, string> Palette { get; } = new ReadOnlyDictionary<string, string>(
        new Dictionary<string, string>
        {
            ["Mavi"] = "#2563EB",
            ["Mor"] = "#7C3AED",
            ["Turkuaz"] = "#0891B2",
            ["Turuncu"] = "#EA580C",
            ["Yeşil"] = "#059669",
            ["Pembe"] = "#DB2777"
        });

    private readonly DeckButton draft;
    private readonly ActionLauncher validator;

    public DeckButton? SavedButton { get; private set; }

    public EditButtonWindow(DeckButton draft, ActionLauncher validator)
    {
        this.draft = draft.Clone();
        this.validator = validator;
        InitializeComponent();
        TitleInput.Text = draft.Title;
        IconInput.Text = draft.Icon;
        TargetInput.Text = draft.Target;
        ColorInput.SelectedValue = draft.Color;
        if (ColorInput.SelectedIndex < 0) ColorInput.SelectedIndex = 0;
        UrlChoice.IsChecked = draft.ActionType == DeckActionType.Url;
        ApplicationChoice.IsChecked = draft.ActionType != DeckActionType.Url;
        Loaded += (_, _) => { TitleInput.Focus(); TitleInput.SelectAll(); };
    }

    private void Action_Checked(object sender, RoutedEventArgs e)
    {
        if (BrowseButton is null || TargetHint is null) return;
        var isApplication = ApplicationChoice.IsChecked == true;
        BrowseButton.IsEnabled = isApplication;
        TargetHint.Text = isApplication ? "Mevcut bir .exe dosyası seçin." : "https:// ile başlayan tam web adresini yazın.";
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog
        {
            Title = "Uygulama seç", Filter = "Uygulamalar (*.exe)|*.exe", CheckFileExists = true, Multiselect = false
        };
        if (picker.ShowDialog(this) == true) TargetInput.Text = picker.FileName;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var selectedColor = ColorInput.SelectedValue as string;
        var colorError = ValidateColorSelection(selectedColor);
        if (colorError is not null)
        {
            ValidationMessage.Text = colorError;
            return;
        }

        draft.Title = TitleInput.Text.Trim();
        draft.Icon = IconInput.Text.Trim();
        draft.Color = selectedColor!;
        draft.ActionType = UrlChoice.IsChecked == true ? DeckActionType.Url : DeckActionType.Application;
        draft.Target = TargetInput.Text.Trim();
        var error = validator.Validate(draft);
        ValidationMessage.Text = error ?? "";
        if (error is not null) return;
        SavedButton = draft.Clone();
        DialogResult = true;
    }

    internal static string? ValidateColorSelection(string? selectedColor) =>
        selectedColor is not null && Palette.Values.Contains(selectedColor) ? null : "Paletten bir renk seçin.";
}
