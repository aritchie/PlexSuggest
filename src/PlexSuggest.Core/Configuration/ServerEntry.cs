namespace PlexSuggest.Core.Configuration;

public record ServerEntry(string Id, string ServerUrl, string Token, string Name, string Description)
{
    public AppConfig ToAppConfig() => new(ServerUrl, Token);
}
