using PlexSuggest.Core.Plex.Models;

namespace PlexSuggest.Core.Recommendations;

public static class RecommendationEngine
{
    const int MaxItemsPerCategory = 20;

    public static List<Category> GenerateCategories(
        TasteProfile profile,
        List<Metadata> unwatchedItems)
    {
        var categories = new List<Category>();

        // 1. Top Picks — always included
        var topPicks = unwatchedItems
            .Select(item => ScoreItem(item, profile))
            .OrderByDescending(s => s.Score)
            .Take(MaxItemsPerCategory)
            .ToList();

        if (topPicks.Count > 0)
        {
            categories.Add(new Category(
                "Top Picks for You",
                "Best matches based on everything you watch",
                CategoryType.TopPicks,
                topPicks));
        }

        // 2. Genre Combos — top genre pairs (up to 2)
        var topPairs = profile.GenrePairWeights
            .OrderByDescending(kv => kv.Value)
            .Take(2)
            .ToList();

        foreach (var pair in topPairs)
        {
            var genres = pair.Key.Split(" + ");
            if (genres.Length != 2) continue;

            var matching = unwatchedItems
                .Where(item => item.Genre.Any(g => string.Equals(g.Name, genres[0], StringComparison.OrdinalIgnoreCase))
                            && item.Genre.Any(g => string.Equals(g.Name, genres[1], StringComparison.OrdinalIgnoreCase)))
                .Select(item => ScoreItem(item, profile, $"Matches your love of {genres[0]} and {genres[1]}"))
                .OrderByDescending(s => s.Score)
                .Take(MaxItemsPerCategory)
                .ToList();

            if (matching.Count >= 3)
            {
                categories.Add(new Category(
                    $"{genres[0]} {genres[1]}",
                    $"Because you love {genres[0].ToLower()} mixed with {genres[1].ToLower()}",
                    CategoryType.GenreCombo,
                    matching));
            }
        }

        // 3. Because You Watched [X] — pick best seed item
        if (profile.TopWatchedItems.Count > 0)
        {
            var seed = profile.TopWatchedItems[0];
            var similar = unwatchedItems
                .Select(item => ScoreSimilarity(item, seed))
                .Where(s => s.Score > 20)
                .OrderByDescending(s => s.Score)
                .Take(MaxItemsPerCategory)
                .ToList();

            if (similar.Count >= 3)
            {
                categories.Add(new Category(
                    $"Because You Watched {seed.Title}",
                    $"Similar titles based on your top watch",
                    CategoryType.BecauseYouWatched,
                    similar));
            }
        }

        // 4. Hidden Gems — high rating but low genre-match
        var hiddenGems = unwatchedItems
            .Where(item => item.EffectiveRating >= 7.0)
            .Select(item =>
            {
                var genreScore = GenreMatchScore(item, profile);
                return new ScoredItem(item,
                    item.EffectiveRating * 10 - genreScore * 30,
                    "Highly rated but outside your usual genres");
            })
            .Where(s => s.Score > 40)
            .OrderByDescending(s => s.Score)
            .Take(MaxItemsPerCategory)
            .ToList();

        if (hiddenGems.Count >= 3)
        {
            categories.Add(new Category(
                "Hidden Gems",
                "Highly rated titles outside your usual picks",
                CategoryType.HiddenGems,
                hiddenGems));
        }

        // 5. Director Spotlight — if a director has 3+ unwatched
        var topDirector = profile.DirectorWeights
            .OrderByDescending(kv => kv.Value)
            .Select(kv => kv.Key)
            .FirstOrDefault(dir =>
                unwatchedItems.Count(i => i.Director.Any(d => string.Equals(d.Name, dir, StringComparison.OrdinalIgnoreCase))) >= 3);

        if (topDirector != null)
        {
            var dirItems = unwatchedItems
                .Where(item => item.Director.Any(d => string.Equals(d.Name, topDirector, StringComparison.OrdinalIgnoreCase)))
                .Select(item => ScoreItem(item, profile, $"Directed by {topDirector}"))
                .OrderByDescending(s => s.Score)
                .Take(MaxItemsPerCategory)
                .ToList();

            categories.Add(new Category(
                $"Director Spotlight: {topDirector}",
                $"More from a director you enjoy",
                CategoryType.DirectorSpotlight,
                dirItems));
        }

        // 6. Decade Deep Dive — top decade with enough items
        var topDecade = profile.DecadeWeights
            .OrderByDescending(kv => kv.Value)
            .Select(kv => kv.Key)
            .FirstOrDefault(decade =>
                unwatchedItems.Count(i => i.Decade == decade) >= 3);

        if (topDecade > 0)
        {
            var decadeItems = unwatchedItems
                .Where(item => item.Decade == topDecade)
                .Select(item => ScoreItem(item, profile, $"From the {topDecade}s — a decade you love"))
                .OrderByDescending(s => s.Score)
                .Take(MaxItemsPerCategory)
                .ToList();

            categories.Add(new Category(
                $"{topDecade}s Deep Dive",
                $"Explore more from the {topDecade}s",
                CategoryType.DecadeDeepDive,
                decadeItems));
        }

        // 7. Recently Added
        var recentlyAdded = unwatchedItems
            .Where(item => item.AddedAt > 0)
            .OrderByDescending(item => item.AddedAt)
            .Take(MaxItemsPerCategory)
            .Select(item => ScoreItem(item, profile, "Recently added to your library"))
            .ToList();

        if (recentlyAdded.Count >= 3)
        {
            categories.Add(new Category(
                "Recently Added",
                "Fresh additions to your library",
                CategoryType.RecentlyAdded,
                recentlyAdded));
        }

        return categories;
    }

    public static ScoredItem ScoreItem(Metadata item, TasteProfile profile, string? reasonOverride = null)
    {
        double score = 0;
        var reasons = new List<string>();

        // Genre match: 40pts
        var genreScore = GenreMatchScore(item, profile);
        score += genreScore * 40;
        if (genreScore > 0.5)
            reasons.Add($"Genres you enjoy ({string.Join(", ", item.Genre.Where(g => !string.IsNullOrEmpty(g.Name)).Select(g => g.Name).Take(3))})");

        // Director match: 15pts
        var dirScore = item.Director
            .Where(d => !string.IsNullOrEmpty(d.Name))
            .Select(d => profile.DirectorWeights.GetValueOrDefault(d.Name))
            .DefaultIfEmpty(0)
            .Max();
        score += dirScore * 15;
        if (dirScore > 0.3 && item.Director.Any(d => !string.IsNullOrEmpty(d.Name)))
            reasons.Add($"Director: {item.Director.First(d => !string.IsNullOrEmpty(d.Name)).Name}");

        // Actor match: 15pts
        var actorScore = item.Role
            .Where(a => !string.IsNullOrEmpty(a.Name))
            .Select(a => profile.ActorWeights.GetValueOrDefault(a.Name))
            .DefaultIfEmpty(0)
            .Max();
        score += actorScore * 15;
        if (actorScore > 0.3)
        {
            var topActor = item.Role
                .Where(a => !string.IsNullOrEmpty(a.Name))
                .OrderByDescending(a => profile.ActorWeights.GetValueOrDefault(a.Name))
                .FirstOrDefault();
            if (topActor != null)
                reasons.Add($"Stars {topActor.Name}");
        }

        // Rating bonus: 15pts
        var ratingScore = Math.Clamp(item.EffectiveRating / 10.0, 0, 1);
        score += ratingScore * 15;

        // Decade match: 10pts
        var decadeScore = item.Decade > 0
            ? profile.DecadeWeights.GetValueOrDefault(item.Decade)
            : 0;
        score += decadeScore * 10;

        // Content rating: 5pts (prefer non-null)
        if (!string.IsNullOrEmpty(item.ContentRating))
            score += 5;

        var reason = reasonOverride ?? (reasons.Count > 0 ? string.Join(" • ", reasons) : "Matches your taste profile");
        return new ScoredItem(item, Math.Round(score, 1), reason);
    }

    static ScoredItem ScoreSimilarity(Metadata item, Metadata seed)
    {
        double score = 0;
        var reasons = new List<string>();

        // Shared genres
        var seedGenres = seed.Genre.Where(g => !string.IsNullOrEmpty(g.Name)).Select(g => g.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sharedGenres = item.Genre.Where(g => !string.IsNullOrEmpty(g.Name) && seedGenres.Contains(g.Name)).Select(g => g.Name).ToList();
        if (sharedGenres.Count > 0 && seedGenres.Count > 0)
        {
            score += (double)sharedGenres.Count / seedGenres.Count * 40;
            reasons.Add($"Shared genres: {string.Join(", ", sharedGenres)}");
        }

        // Shared director
        var seedDirs = seed.Director.Where(d => !string.IsNullOrEmpty(d.Name)).Select(d => d.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (item.Director.Any(d => !string.IsNullOrEmpty(d.Name) && seedDirs.Contains(d.Name)))
        {
            score += 20;
            reasons.Add("Same director");
        }

        // Shared actors
        var seedActors = seed.Role.Where(a => !string.IsNullOrEmpty(a.Name)).Select(a => a.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sharedActors = item.Role.Where(a => !string.IsNullOrEmpty(a.Name) && seedActors.Contains(a.Name)).Select(a => a.Name).Take(3).ToList();
        if (sharedActors.Count > 0)
        {
            score += Math.Min(sharedActors.Count * 5, 15);
            reasons.Add($"Stars {string.Join(", ", sharedActors)}");
        }

        // Same decade
        if (item.Decade > 0 && item.Decade == seed.Decade)
        {
            score += 10;
            reasons.Add($"Also from the {item.Decade}s");
        }

        // Rating bonus
        score += Math.Clamp(item.EffectiveRating / 10.0, 0, 1) * 15;

        var reason = reasons.Count > 0
            ? $"Similar to {seed.Title}: {string.Join(" • ", reasons)}"
            : $"Similar to {seed.Title}";

        return new ScoredItem(item, Math.Round(score, 1), reason);
    }

    static double GenreMatchScore(Metadata item, TasteProfile profile)
    {
        var genres = item.Genre.Where(g => !string.IsNullOrEmpty(g.Name)).ToList();
        if (genres.Count == 0) return 0;
        return genres
            .Select(g => profile.GenreWeights.GetValueOrDefault(g.Name))
            .Average();
    }
}
