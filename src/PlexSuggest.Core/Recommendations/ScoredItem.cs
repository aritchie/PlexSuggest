using PlexSuggest.Core.Plex.Models;

namespace PlexSuggest.Core.Recommendations;

public record ScoredItem(Metadata Item, double Score, string Reason);
