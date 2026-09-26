using System.Text.Json;
using InsManager.Core.Models;
using InsManager.Core.Services;

namespace InsManager.Infrastructure;

public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _settingsPath;
    private readonly string _legacySettingsPath;

    public JsonSettingsService()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(
            localAppData,
            "INS Manager");
        _settingsPath = Path.Combine(directory, "settings.json");
        _legacySettingsPath = Path.Combine(localAppData, "727 INS Manager", "settings.json");
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var path = File.Exists(_settingsPath) ? _settingsPath : _legacySettingsPath;
        if (!File.Exists(path)) return new AppSettings();

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken)
            ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
    }
}
