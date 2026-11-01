using System.IO;
using System.Text.Json;
using DigitalStreamDeck.Models;

namespace DigitalStreamDeck.Services;

public sealed record SettingsLoadResult(DeckSettings Settings, string? Warning);

public sealed class SettingsService
{
    private readonly string directory;
    private readonly string settingsPath;

    public SettingsService(string? settingsDirectory = null)
    {
        directory = settingsDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DigitalStreamDeck");
        settingsPath = Path.Combine(directory, "settings.json");
    }

    public async Task<SettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await using var stream = File.OpenRead(settingsPath);
            var settings = await JsonSerializer.DeserializeAsync<DeckSettings>(stream, cancellationToken: cancellationToken)
                ?? throw new JsonException("Settings must be a JSON object.");
            settings.Normalize();
            return new SettingsLoadResult(settings, null);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return Defaults(null);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return Defaults("Ayarlar yüklenemedi; varsayılan düzen açıldı.");
        }
    }

    public async Task SaveAsync(DeckSettings settings, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(directory);
        var temporaryPath = settingsPath + ".tmp";
        await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(stream, settings, cancellationToken: cancellationToken);
            await stream.FlushAsync(cancellationToken);
            stream.Flush(flushToDisk: true);
        }

        cancellationToken.ThrowIfCancellationRequested();
        File.Move(temporaryPath, settingsPath, overwrite: true);
    }

    private static SettingsLoadResult Defaults(string? warning)
    {
        var settings = DeckSettings.CreateDefault();
        settings.Normalize();
        return new SettingsLoadResult(settings, warning);
    }
}
