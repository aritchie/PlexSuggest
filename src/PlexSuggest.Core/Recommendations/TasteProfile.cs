using PlexSuggest.Core.Plex.Models;

namespace PlexSuggest.Core.Recommendations;

public class TasteProfile
{
    public Dictionary<string, double> GenreWeights { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, double> GenrePairWeights { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, double> DirectorWeights { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, double> ActorWeights { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<int, double> DecadeWeights { get; } = [];
    public List<Metadata> TopWatchedItems { get; } = [];

    public static TasteProfile Build(List<Metadata> watchedItems, List<Metadata> historyEntries)
    {
        var profile = new TasteProfile();
        var now = DateTimeOffset.UtcNow;

        // Build lookup for last-viewed timestamps from history
        var lastViewed = new Dictionary<string, DateTimeOffset>();
        foreach (var entry in historyEntries)
        {
            var key = entry.GrandparentRatingKey ?? entry.RatingKey;
            if (string.IsNullOrEmpty(key)) continue;
            if (entry.ViewedAt is > 0)
            {
                var viewedAt = DateTimeOffset.FromUnixTimeSeconds(entry.ViewedAt.Value);
                if (!lastViewed.TryGetValue(key, out var existing) || viewedAt > existing)
                    lastViewed[key] = viewedAt;
            }
        }

        foreach (var item in watchedItems)
        {
            var viewedAt = GetViewedAt(item, lastViewed);
            var daysSince = Math.Max(0, (now - viewedAt).TotalDays);
            var recency = 1.0 / (1.0 + daysSince / 180.0);

            // Genre weights
            foreach (var genre in item.Genre)
            {
                AddWeight(profile.GenreWeights, genre.Name, recency);
            }

            // Genre pair weights
            var genres = item.Genre.Select(g => g.Name).Where(n => !string.IsNullOrEmpty(n)).OrderBy(g => g).ToList();
            for (var i = 0; i < genres.Count; i++)
            {
                for (var j = i + 1; j < genres.Count; j++)
                {
                    var pair = $"{genres[i]} + {genres[j]}";
                    AddWeight(profile.GenrePairWeights, pair, recency);
                }
            }

            // Director weights
            foreach (var director in item.Director)
            {
                AddWeight(profile.DirectorWeights, director.Name, recency);
            }

            // Actor weights
            foreach (var actor in item.Role.Take(5)) // top-billed actors
            {
                AddWeight(profile.ActorWeights, actor.Name, recency);
            }

            // Decade weights
            if (item.Decade > 0)
            {
                if (!profile.DecadeWeights.TryGetValue(item.Decade, out var dw))
                    dw = 0;
                profile.DecadeWeights[item.Decade] = dw + recency;
            }
        }

        // Trim actors to top 50
        if (profile.ActorWeights.Count > 50)
        {
            var topActors = profile.ActorWeights
                .OrderByDescending(kv => kv.Value)
                .Take(50)
                .Select(kv => kv.Key)
                .ToHashSet();
            foreach (var key in profile.ActorWeights.Keys.Where(k => !topActors.Contains(k)).ToList())
                profile.ActorWeights.Remove(key);
        }

        // Top watched items — scored by viewCount * recency * rating
        profile.TopWatchedItems.AddRange(
            watchedItems
                .Select(item =>
                {
                    var viewedAt = GetViewedAt(item, lastViewed);
                    var daysSince = Math.Max(0, (now - viewedAt).TotalDays);
                    var recency = 1.0 / (1.0 + daysSince / 180.0);
                    var score = (item.ViewCount ?? 1) * recency * (item.EffectiveRating / 10.0 + 0.5);
                    return (item, score);
                })
                .OrderByDescending(x => x.score)
                .Take(10)
                .Select(x => x.item)
        );

        // Normalize weights
        Normalize(profile.GenreWeights);
        Normalize(profile.GenrePairWeights);
        Normalize(profile.DirectorWeights);
        Normalize(profile.ActorWeights);
        NormalizeDecades(profile.DecadeWeights);

        return profile;
    }

    static DateTimeOffset GetViewedAt(Metadata item, Dictionary<string, DateTimeOffset> lastViewed)
    {
        if (!string.IsNullOrEmpty(item.RatingKey) && lastViewed.TryGetValue(item.RatingKey, out var dt))
            return dt;
        if (item.LastViewedAt > 0)
            return DateTimeOffset.FromUnixTimeSeconds(item.LastViewedAt.Value);
        return DateTimeOffset.UtcNow.AddDays(-365);
    }

    static void AddWeight(Dictionary<string, double> dict, string? key, double weight)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        if (!dict.TryGetValue(key, out var existing))
            existing = 0;
        dict[key] = existing + weight;
    }

    static void Normalize(Dictionary<string, double> dict)
    {
        if (dict.Count == 0) return;
        var max = dict.Values.Max();
        if (max <= 0) return;
        foreach (var key in dict.Keys.ToList())
            dict[key] /= max;
    }

    static void NormalizeDecades(Dictionary<int, double> dict)
    {
        if (dict.Count == 0) return;
        var max = dict.Values.Max();
        if (max <= 0) return;
        foreach (var key in dict.Keys.ToList())
            dict[key] /= max;
    }
}
