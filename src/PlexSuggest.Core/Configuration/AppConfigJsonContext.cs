using System.Text.Json.Serialization;
using PlexSuggest.Core.Plex.Models;

namespace PlexSuggest.Core.Configuration;

[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(ServerEntry))]
[JsonSerializable(typeof(PlexSuggestConfig))]
[JsonSerializable(typeof(PlexResponse))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class AppJsonContext : JsonSerializerContext;
