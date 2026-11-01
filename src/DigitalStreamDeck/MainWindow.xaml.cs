using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DigitalStreamDeck.Models;
using DigitalStreamDeck.Services;
using DigitalStreamDeck.Windows;

namespace DigitalStreamDeck;

public partial class MainWindow : Window
{
    private readonly DeckSettings settings;
    private readonly SettingsService settingsService;
    private readonly ActionLauncher launcher;
    private readonly SemaphoreSlim saveGate = new(1, 1);
    private bool editing;
    private bool exiting;

    public MainWindow(DeckSettings settings, SettingsService settingsService, ActionLauncher launcher)
    {
        this.settings = settings;
        this.settingsService = settingsService;
        this.launcher = launcher;
        settings.Normalize();
        InitializeComponent();
        Tiles.ItemsSource = settings.Buttons;
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
        if (sender is not Button { DataContext: DeckButton button }) return;
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
        if (sender is Button { DataContext: DeckButton button }) await EditAsync(button);
    }

    private async void Tile_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Apps && !(e.Key == Key.F10 && Keyboard.Modifiers == ModifierKeys.Shift)) return;
        e.Handled = true;
        if (sender is Button { DataContext: DeckButton button }) await EditAsync(button);
    }

    private async Task EditAsync(DeckButton button)
    {
        if (editing || exiting) return;
        editing = true;
        try
        {
            var editor = new EditButtonWindow(button.Clone(), launcher) { Owner = this };
            if (editor.ShowDialog() == true && editor.SavedButton is { } saved)
                await SaveAsync(saved);
        }
        finally { editing = false; }
    }

    private async Task SaveAsync(DeckButton? editedButton = null)
    {
        await saveGate.WaitAsync();
        try
        {
            var snapshot = new DeckSettings
            {
                WindowLeft = Left, WindowTop = Top,
                Buttons = settings.Buttons.Select(button => button.Id == editedButton?.Id ? editedButton.Clone() : button.Clone()).ToList()
            };
            await settingsService.SaveAsync(snapshot);
            settings.WindowLeft = snapshot.WindowLeft;
            settings.WindowTop = snapshot.WindowTop;
            if (editedButton is not null)
            {
                settings.Buttons = snapshot.Buttons;
                Tiles.ItemsSource = settings.Buttons;
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowError("Ayarlar kaydedilemedi. Klasör izinlerini ve kullanılabilir disk alanını kontrol edin. Son değişiklik kaydedilmedi.", "Kaydetme hatası");
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
