using System.Windows;
using DigitalStreamDeck.Services;

namespace DigitalStreamDeck;

public partial class App : Application
{
    private readonly GlobalShortcutService shortcut = new();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var settingsService = new SettingsService();
        var loaded = await settingsService.LoadAsync();
        var panel = new MainWindow(loaded.Settings, settingsService, new ActionLauncher(), new ShellIconService());
        MainWindow = panel;
        panel.Show();

        // WH_MOUSE_LL requires the dispatcher thread's pumping message loop.
        shortcut.ToggleRequested += OnToggleRequested;
        try { shortcut.Start(); }
        catch (InvalidOperationException exception)
        {
            MessageBox.Show(panel, exception.Message, "Kısayol kullanılamıyor", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        if (loaded.Warning is not null)
            MessageBox.Show(panel, loaded.Warning, "Ayarlar", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void OnToggleRequested(object? sender, EventArgs e)
    {
        if (!Dispatcher.HasShutdownStarted)
            Dispatcher.BeginInvoke(new Action(() => (MainWindow as MainWindow)?.ToggleVisibility()));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        shortcut.ToggleRequested -= OnToggleRequested;
        shortcut.Dispose();
        base.OnExit(e);
    }
}
