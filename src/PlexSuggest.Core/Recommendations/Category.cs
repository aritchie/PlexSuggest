namespace PlexSuggest.Core.Recommendations;

public record Category(string Name, string Description, CategoryType Type, List<ScoredItem> Items);

public enum CategoryType
{
    TopPicks,
    GenreCombo,
    BecauseYouWatched,
    HiddenGems,
    DirectorSpotlight,
    DecadeDeepDive,
    RecentlyAdded
}
