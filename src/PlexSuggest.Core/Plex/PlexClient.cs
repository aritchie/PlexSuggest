using System.Text.Json;
using PlexSuggest.Core.Configuration;
using PlexSuggest.Core.Plex.Models;

namespace PlexSuggest.Core.Plex;

public class PlexClient : IDisposable
{
    readonly HttpClient _http;
    readonly string _token;
    const int PageSize = 100;

    public PlexClient(AppConfig config)
    {
        _token = config.Token;
        var baseUrl = config.ServerUrl.TrimEnd('/') + "/";
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _http.DefaultRequestHeaders.Add("Accept", "application/json");
        _http.DefaultRequestHeaders.Add("X-Plex-Token", _token);
        _http.DefaultRequestHeaders.Add("X-Plex-Client-Identifier", "PlexSuggest");
        _http.DefaultRequestHeaders.Add("X-Plex-Product", "PlexSuggest");
        _http.DefaultRequestHeaders.Add("X-Plex-Version", "1.0.0");
    }

    public async Task<string?> ValidateConnectionAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetAsync("?accept=json", ct);
        resp.EnsureSuccessStatusCode();
        var data = await DeserializeAsync(resp, ct);
        return data?.MediaContainer.FriendlyName;
    }

    public async Task<List<LibrarySection>> GetLibrarySectionsAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetAsync("library/sections?accept=json", ct);
        resp.EnsureSuccessStatusCode();
        var data = await DeserializeAsync(resp, ct);
        return data?.MediaContainer.Directory
            .Where(d => d.Type is "movie" or "show")
            .ToList() ?? [];
    }

    public async Task<List<Metadata>> GetAllItemsAsync(
        string sectionKey,
        IProgress<(int current, int total)>? progress = null,
        CancellationToken ct = default)
    {
        var items = new List<Metadata>();
        var start = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"library/sections/{sectionKey}/all?accept=json");
            request.Headers.Add("X-Plex-Container-Start", start.ToString());
            request.Headers.Add("X-Plex-Container-Size", PageSize.ToString());

            var resp = await _http.SendAsync(request, ct);
            resp.EnsureSuccessStatusCode();
            var data = await DeserializeAsync(resp, ct);
            if (data is null) break;

            var container = data.MediaContainer;
            items.AddRange(container.Metadata);
            progress?.Report((items.Count, container.TotalSize));

            if (items.Count >= container.TotalSize || container.Metadata.Count == 0)
                break;

            start += PageSize;
        }

        return items;
    }

    public async Task<List<Metadata>> GetWatchHistoryAsync(
        IProgress<(int current, int total)>? progress = null,
        CancellationToken ct = default)
    {
        var entries = new List<Metadata>();
        var start = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            using var request = new HttpRequestMessage(HttpMethod.Get,
                "status/sessions/history/all?accept=json");
            request.Headers.Add("X-Plex-Container-Start", start.ToString());
            request.Headers.Add("X-Plex-Container-Size", PageSize.ToString());

            var resp = await _http.SendAsync(request, ct);
            resp.EnsureSuccessStatusCode();
            var data = await DeserializeAsync(resp, ct);
            if (data is null) break;

            var container = data.MediaContainer;
            entries.AddRange(container.Metadata);
            progress?.Report((entries.Count, container.TotalSize));

            if (entries.Count >= container.TotalSize || container.Metadata.Count == 0)
                break;

            start += PageSize;
        }

        return entries;
    }

    public async Task<Metadata?> GetMetadataAsync(string ratingKey, CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"library/metadata/{ratingKey}?accept=json", ct);
        resp.EnsureSuccessStatusCode();
        var data = await DeserializeAsync(resp, ct);
        return data?.MediaContainer.Metadata.FirstOrDefault();
    }

    static async Task<PlexResponse?> DeserializeAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var json = await response.Content.ReadAsStringAsync(ct);

        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("Plex returned an empty response.");

        if (json.TrimStart().StartsWith('<'))
            throw new InvalidOperationException(
                "Plex returned XML instead of JSON. Verify the server URL and token are correct.");

        return JsonSerializer.Deserialize(json, AppJsonContext.Default.PlexResponse);
    }

    public void Dispose() => _http.Dispose();
}
