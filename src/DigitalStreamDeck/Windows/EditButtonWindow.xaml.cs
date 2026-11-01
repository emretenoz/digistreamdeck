using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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
    private readonly ShellIconService iconService;

    public DeckButton? SavedButton { get; private set; }
    public bool DeleteRequested { get; private set; }

    public EditButtonWindow(DeckButton draft, ActionLauncher validator)
        : this(draft, validator, new ShellIconService(), true)
    {
        DeleteButton.Visibility = Visibility.Collapsed;
    }

    public EditButtonWindow(DeckButton draft, ActionLauncher validator, ShellIconService iconService, bool allowDelete)
    {
        this.draft = draft.Clone();
        this.validator = validator;
        this.iconService = iconService;
        InitializeComponent();
        DeleteButton.Visibility = allowDelete ? Visibility.Visible : Visibility.Collapsed;
        if (!allowDelete) Title = "Tuş ekle";
        EditorHeading.Text = Title;
        TitleInput.Text = draft.Title;
        IconInput.Text = draft.Icon;
        TargetInput.Text = draft.Target;
        ColorInput.SelectedValue = draft.Color;
        if (ColorInput.SelectedIndex < 0) ColorInput.SelectedIndex = 0;
        UrlChoice.IsChecked = draft.ActionType == DeckActionType.Url;
        ApplicationChoice.IsChecked = draft.ActionType != DeckActionType.Url;
        RefreshPreview();
        Loaded += (_, _) => { TitleInput.Focus(); TitleInput.SelectAll(); };
    }

    private void Action_Checked(object sender, RoutedEventArgs e)
    {
        if (BrowseButton is null || TargetHint is null) return;
        var isApplication = ApplicationChoice.IsChecked == true;
        BrowseButton.IsEnabled = isApplication;
        TargetHint.Text = isApplication ? "Mevcut bir .exe veya .lnk dosyası seçin." : "http veya https ile başlayan tam web adresini yazın.";
        RefreshPreview();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog
        {
            Title = "Uygulama seç", Filter = "Uygulamalar ve kısayollar (*.exe;*.lnk)|*.exe;*.lnk", CheckFileExists = true, Multiselect = false
        };
        if (picker.ShowDialog(this) == true)
        {
            ApplicationChoice.IsChecked = true;
            TargetInput.Text = picker.FileName;
            RefreshPreview();
        }
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
        draft.IconPath = ResolveIconPath(draft.ActionType, draft.Target);
        SavedButton = draft.Clone();
        DialogResult = true;
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, $"“{draft.Title}” tuşunu silmek istiyor musunuz?", "Tuşu sil",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        DeleteRequested = true;
        SavedButton = null;
        DialogResult = true;
    }

    private void PreviewInput_Changed(object sender, TextChangedEventArgs e) => RefreshPreview();

    private void RefreshPreview()
    {
        if (IconPreview is null || EmojiPreview is null || TargetInput is null || IconInput is null || ApplicationChoice is null) return;
        var type = ApplicationChoice.IsChecked == true ? DeckActionType.Application : DeckActionType.Url;
        IconPreview.Source = iconService.GetIcon(ResolveIconPath(type, TargetInput.Text.Trim()));
        IconPreview.Visibility = IconPreview.Source is null ? Visibility.Collapsed : Visibility.Visible;
        EmojiPreview.Visibility = IconPreview.Source is null ? Visibility.Visible : Visibility.Collapsed;
        EmojiPreview.Text = IconInput.Text;
    }

    internal static string ResolveIconPath(DeckActionType actionType, string target) =>
        actionType == DeckActionType.Application && File.Exists(target)
        && (string.Equals(Path.GetExtension(target), ".exe", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Path.GetExtension(target), ".lnk", StringComparison.OrdinalIgnoreCase))
            ? target : "";

    internal static string? ValidateColorSelection(string? selectedColor) =>
        selectedColor is not null && Palette.Values.Contains(selectedColor) ? null : "Paletten bir renk seçin.";
}
