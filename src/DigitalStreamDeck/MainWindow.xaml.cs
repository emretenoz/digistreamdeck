using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DigitalStreamDeck.Models;
using DigitalStreamDeck.Presentation;
using DigitalStreamDeck.Services;
using DigitalStreamDeck.Windows;

namespace DigitalStreamDeck;

public partial class MainWindow : Window
{
    private readonly DeckSettings settings;
    private readonly SettingsService settingsService;
    private readonly ActionLauncher launcher;
    private readonly ShellIconService iconService;
    private readonly SemaphoreSlim saveGate = new(1, 1);
    private int currentPageIndex;
    private bool editing;
    private bool exiting;

    public MainWindow(DeckSettings settings, SettingsService settingsService, ActionLauncher launcher, ShellIconService iconService)
    {
        this.settings = settings;
        this.settingsService = settingsService;
        this.launcher = launcher;
        this.iconService = iconService;
        settings.Normalize();
        InitializeComponent();
        RefreshPage();
    }

    private void RefreshPage()
    {
        currentPageIndex = DeckPagination.ClampPage(currentPageIndex, settings.Buttons.Count);
        var pageCount = DeckPagination.GetPageCount(settings.Buttons.Count);
        Tiles.ItemsSource = DeckPagination.GetPage(settings.Buttons, currentPageIndex)
            .Select(button => new DeckTileViewModel(button, iconService.GetIcon(button.IconPath))).ToArray();
        PreviousPageButton.IsEnabled = currentPageIndex > 0;
        NextPageButton.IsEnabled = currentPageIndex < pageCount - 1;
        PageIndicator.Text = $"{currentPageIndex + 1} / {pageCount}";
        EmptyState.Visibility = settings.Buttons.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PreviousPage_Click(object sender, RoutedEventArgs e)
    {
        currentPageIndex--;
        RefreshPage();
    }

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        currentPageIndex++;
        RefreshPage();
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        if (editing || exiting) return;
        editing = true;
        try
        {
            var editor = new EditButtonWindow(DeckSettings.CreateNewButton(), launcher, iconService, allowDelete: false) { Owner = this };
            if (editor.ShowDialog() == true && editor.SavedButton is { } saved)
            {
                var candidates = settings.Buttons.Select(button => button.Clone()).Append(saved.Clone()).ToArray();
                if (await PersistButtonsAsync(candidates))
                {
                    currentPageIndex = DeckPagination.GetPageCount(settings.Buttons.Count) - 1;
                    RefreshPage();
                }
            }
        }
        finally { editing = false; }
    }

    private void Window_SourceInitialized(object? sender, EventArgs e) => NormalizePosition();

    private void NormalizePosition()
    {
        var transform = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var areas = System.Windows.Forms.Screen.AllScreens.OrderByDescending(screen => screen.Primary)
            .Select(screen =>
            {
                var area = screen.WorkingArea;
                return Rect.Transform(new Rect(area.X, area.Y, area.Width, area.Height), transform);
            }).ToArray();
        var point = WindowPositionNormalizer.Normalize(settings.WindowLeft, settings.WindowTop,
            new Rect(0, 0, Width, Height), areas);
        Left = point.X;
        Top = point.Y;
    }

    public void ToggleVisibility()
    {
        if (editing || exiting || !IsEnabled) return;
        if (IsVisible) Hide();
        else
        {
            NormalizePosition();
            Show();
            Activate();
        }
    }

    private async void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || exiting) return;
        DragMove();
        await SaveAsync();
    }

    private async void Tile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: DeckTileViewModel tile }) return;
        var button = tile.Button;
        if (button.ActionType == DeckActionType.None) await EditAsync(button);
        else
        {
            var result = launcher.Launch(button);
            if (!result.Succeeded) ShowError(result.Error!, "Hedef açılamadı");
        }
    }

    private async void Tile_RightClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (sender is Button { DataContext: DeckTileViewModel tile }) await EditAsync(tile.Button);
    }

    private async void Tile_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Apps && !(e.Key == Key.F10 && Keyboard.Modifiers == ModifierKeys.Shift)) return;
        e.Handled = true;
        if (sender is Button { DataContext: DeckTileViewModel tile }) await EditAsync(tile.Button);
    }

    private async Task EditAsync(DeckButton button)
    {
        if (editing || exiting) return;
        editing = true;
        try
        {
            var editor = new EditButtonWindow(button.Clone(), launcher, iconService, allowDelete: true) { Owner = this };
            if (editor.ShowDialog() != true) return;
            if (editor.DeleteRequested)
                await PersistButtonsAsync(settings.Buttons.Where(candidate => candidate.Id != button.Id).Select(candidate => candidate.Clone()).ToArray());
            else if (editor.SavedButton is { } saved)
                await PersistButtonsAsync(settings.Buttons.Select(candidate => candidate.Id == button.Id ? saved.Clone() : candidate.Clone()).ToArray());
        }
        finally { editing = false; }
    }

    private Task<bool> PersistButtonsAsync(IReadOnlyList<DeckButton> candidateButtons) => SaveAsync(candidateButtons);

    private async Task<bool> SaveAsync(IReadOnlyList<DeckButton>? candidateButtons = null)
    {
        await saveGate.WaitAsync();
        try
        {
            var snapshot = new DeckSettings
            {
                WindowLeft = Left, WindowTop = Top,
                Buttons = (candidateButtons ?? settings.Buttons).Select(button => button.Clone()).ToList()
            };
            await settingsService.SaveAsync(snapshot);
            settings.WindowLeft = snapshot.WindowLeft;
            settings.WindowTop = snapshot.WindowTop;
            if (candidateButtons is not null)
            {
                foreach (var path in settings.Buttons.Select(button => button.IconPath).Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase))
                    if (!snapshot.Buttons.Any(button => string.Equals(button.IconPath, path, StringComparison.OrdinalIgnoreCase)))
                        iconService.Clear(path);
                settings.Buttons = snapshot.Buttons;
                RefreshPage();
            }
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowError("Ayarlar kaydedilemedi. Klasör izinlerini ve kullanılabilir disk alanını kontrol edin. Son değişiklik kaydedilmedi.", "Kaydetme hatası");
            return false;
        }
        finally { saveGate.Release(); }
    }

    private void ShowError(string message, string title) => MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || editing || exiting) return;
        e.Handled = true;
        Hide();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        if (exiting) return;
        exiting = true;
        IsEnabled = false;
        await SaveAsync();
        Closing -= Window_Closing;
        Application.Current.Shutdown();
    }
}
