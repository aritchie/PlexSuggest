using System.Text.Json;

namespace PlexSuggest.Core.Configuration;

public static class ConfigManager
{
    const int CurrentVersion = 2;

    static string? _configDir;

    public static void SetConfigDirectory(string dir) => _configDir = dir;

    static string DefaultConfigDir => _configDir
        ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "plexsuggest"
        );

    static string DefaultConfigPath => Path.Combine(DefaultConfigDir, "config.json");

    // ── Full config operations ──────────────────────────────────────

    public static PlexSuggestConfig LoadConfig(string? configPath = null)
    {
        var path = configPath ?? DefaultConfigPath;
        if (!File.Exists(path))
            return new PlexSuggestConfig(CurrentVersion, [], null);

        var json = File.ReadAllText(path);

        // Try v2 format first
        var config = JsonSerializer.Deserialize(json, AppJsonContext.Default.PlexSuggestConfig);
        if (config is not null && config.Version >= CurrentVersion)
            return config;

        // Fall back to old single-server format (v1)
        var legacy = JsonSerializer.Deserialize(json, AppJsonContext.Default.AppConfig);
        if (legacy is not null && !string.IsNullOrWhiteSpace(legacy.ServerUrl))
        {
            var entry = new ServerEntry(
                Guid.NewGuid().ToString(),
                legacy.ServerUrl,
                legacy.Token,
                "",
                ""
            );
            var migrated = new PlexSuggestConfig(CurrentVersion, [entry], entry.Id);
            SaveConfig(migrated, configPath);
            return migrated;
        }

        return new PlexSuggestConfig(CurrentVersion, [], null);
    }

    public static void SaveConfig(PlexSuggestConfig config, string? configPath = null)
    {
        var path = configPath ?? DefaultConfigPath;
        var dir = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(config, AppJsonContext.Default.PlexSuggestConfig);
        File.WriteAllText(path, json);
    }

    // ── Server management ───────────────────────────────────────────

    public static ServerEntry AddServer(string serverUrl, string token, string name, string description, string? configPath = null)
    {
        var config = LoadConfig(configPath);
        var entry = new ServerEntry(Guid.NewGuid().ToString(), serverUrl, token, name, description);
        var servers = new List<ServerEntry>(config.Servers) { entry };
        SaveConfig(config with { Servers = servers, LastServerId = entry.Id }, configPath);
        return entry;
    }

    public static void UpdateServer(ServerEntry updated, string? configPath = null)
    {
        var config = LoadConfig(configPath);
        var servers = config.Servers.Select(s => s.Id == updated.Id ? updated : s).ToList();
        SaveConfig(config with { Servers = servers }, configPath);
    }

    public static void RemoveServer(string serverId, string? configPath = null)
    {
        var config = LoadConfig(configPath);
        var servers = config.Servers.Where(s => s.Id != serverId).ToList();
        var lastId = config.LastServerId == serverId ? null : config.LastServerId;
        SaveConfig(config with { Servers = servers, LastServerId = lastId }, configPath);
    }

    public static void SetLastServer(string serverId, string? configPath = null)
    {
        var config = LoadConfig(configPath);
        SaveConfig(config with { LastServerId = serverId }, configPath);
    }

    public static ServerEntry? GetLastServer(string? configPath = null)
    {
        var config = LoadConfig(configPath);
        if (config.LastServerId is not null)
            return config.Servers.FirstOrDefault(s => s.Id == config.LastServerId);

        return config.Servers.FirstOrDefault();
    }

    // ── Backward-compatible API (used by LibraryPickerViewModel, etc.) ──

    public static AppConfig? Load(string? configPath = null)
        => GetLastServer(configPath)?.ToAppConfig();

    public static void Save(AppConfig appConfig, string? configPath = null)
    {
        var config = LoadConfig(configPath);
        var existing = config.Servers.FirstOrDefault(s =>
            s.ServerUrl.Equals(appConfig.ServerUrl, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            var updated = existing with { Token = appConfig.Token };
            var servers = config.Servers.Select(s => s.Id == updated.Id ? updated : s).ToList();
            SaveConfig(config with { Servers = servers, LastServerId = updated.Id }, configPath);
        }
        else
        {
            AddServer(appConfig.ServerUrl, appConfig.Token, "", "", configPath);
        }
    }

    public static void Delete(string? configPath = null)
    {
        var path = configPath ?? DefaultConfigPath;
        if (File.Exists(path))
            File.Delete(path);
    }
}
