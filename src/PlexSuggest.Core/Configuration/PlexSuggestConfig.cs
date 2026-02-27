namespace PlexSuggest.Core.Configuration;

public record PlexSuggestConfig(int Version, List<ServerEntry> Servers, string? LastServerId);
