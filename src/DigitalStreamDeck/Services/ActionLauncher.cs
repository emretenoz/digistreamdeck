using System.Diagnostics;
using System.Globalization;
using System.IO;
using DigitalStreamDeck.Models;

namespace DigitalStreamDeck.Services;

public sealed record LaunchResult(bool Succeeded, string? Error);

public sealed class ActionLauncher(Func<ProcessStartInfo, Process?>? processStarter = null)
{
    private readonly Func<ProcessStartInfo, Process?> processStarter = processStarter ?? Process.Start;

    public string? Validate(DeckButton button)
    {
        if (string.IsNullOrWhiteSpace(button.Title) || button.Title.Length > 24)
            return "Başlık 1–24 karakter olmalıdır.";

        if (string.IsNullOrWhiteSpace(button.Icon) || new StringInfo(button.Icon).LengthInTextElements > 4)
            return "Simge 1–4 metin öğesi olmalıdır.";

        return button.ActionType switch
        {
            DeckActionType.Application => File.Exists(button.Target)
                && string.Equals(Path.GetExtension(button.Target), ".exe", StringComparison.OrdinalIgnoreCase)
                    ? null : "Mevcut bir .exe dosyası seçin.",
            DeckActionType.Url => Uri.TryCreate(button.Target, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                    ? null : "Geçerli bir http veya https adresi girin.",
            _ => "Bir eylem türü seçin."
        };
    }

    public LaunchResult Launch(DeckButton button)
    {
        var error = Validate(button);
        if (error is not null) return new LaunchResult(false, error);

        try
        {
            using var process = processStarter(new ProcessStartInfo(button.Target) { UseShellExecute = true });
            return new LaunchResult(true, null);
        }
        catch (Exception)
        {
            return new LaunchResult(false, "Hedef açılamadı. Dosya veya adresi kontrol edin.");
        }
    }
}
