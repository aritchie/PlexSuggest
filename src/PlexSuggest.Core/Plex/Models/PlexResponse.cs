using System.Text.Json.Serialization;

namespace PlexSuggest.Core.Plex.Models;

public record PlexResponse
{
    [JsonPropertyName("MediaContainer")]
    public MediaContainer MediaContainer { get; init; } = new();
}

public record MediaContainer
{
    [JsonPropertyName("size")]
    public int Size { get; init; }

    [JsonPropertyName("totalSize")]
    public int TotalSize { get; init; }

    [JsonPropertyName("title1")]
    public string? Title1 { get; init; }

    [JsonPropertyName("friendlyName")]
    public string? FriendlyName { get; init; }

    [JsonPropertyName("machineIdentifier")]
    public string? MachineIdentifier { get; init; }

    [JsonPropertyName("Directory")]
    public List<LibrarySection> Directory { get; init; } = [];

    [JsonPropertyName("Metadata")]
    public List<Metadata> Metadata { get; init; } = [];
}

public record LibrarySection
{
    [JsonPropertyName("key")]
    public string Key { get; init; } = "";

    [JsonPropertyName("title")]
    public string Title { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("agent")]
    public string? Agent { get; init; }
}

public record Metadata
{
    [JsonPropertyName("ratingKey")]
    public string RatingKey { get; init; } = "";

    [JsonPropertyName("parentRatingKey")]
    public string? ParentRatingKey { get; init; }

    [JsonPropertyName("grandparentRatingKey")]
    public string? GrandparentRatingKey { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = "";

    [JsonPropertyName("grandparentTitle")]
    public string? GrandparentTitle { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("year")]
    public int? Year { get; init; }

    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("rating")]
    public double? Rating { get; init; }

    [JsonPropertyName("audienceRating")]
    public double? AudienceRating { get; init; }

    [JsonPropertyName("contentRating")]
    public string? ContentRating { get; init; }

    [JsonPropertyName("duration")]
    public long? Duration { get; init; }

    [JsonPropertyName("thumb")]
    public string? Thumb { get; init; }

    [JsonPropertyName("addedAt")]
    public long? AddedAt { get; init; }

    [JsonPropertyName("viewCount")]
    public int? ViewCount { get; init; }

    [JsonPropertyName("lastViewedAt")]
    public long? LastViewedAt { get; init; }

    [JsonPropertyName("viewedLeafCount")]
    public int? ViewedLeafCount { get; init; }

    [JsonPropertyName("leafCount")]
    public int? LeafCount { get; init; }

    [JsonPropertyName("Genre")]
    public List<Tag>? Genre { get; init; }

    [JsonPropertyName("Director")]
    public List<Tag>? Director { get; init; }

    [JsonPropertyName("Role")]
    public List<Tag>? Role { get; init; }

    [JsonIgnore] public IReadOnlyList<Tag> Genres => Genre ?? [];
    [JsonIgnore] public IReadOnlyList<Tag> Directors => Director ?? [];
    [JsonIgnore] public IReadOnlyList<Tag> Roles => Role ?? [];

    [JsonPropertyName("viewedAt")]
    public long? ViewedAt { get; init; }

    [JsonPropertyName("accountID")]
    public int? AccountID { get; init; }

    public bool IsWatched => Type == "show"
        ? (ViewedLeafCount ?? 0) > 0
        : (ViewCount ?? 0) > 0;

    public bool IsUnwatched => Type == "show"
        ? (ViewedLeafCount ?? 0) == 0
        : (ViewCount ?? 0) == 0;

    public double EffectiveRating => AudienceRating ?? Rating ?? 0;

    public int Decade => Year.HasValue ? (Year.Value / 10) * 10 : 0;
}

public record Tag
{
    [JsonPropertyName("tag")]
    public string Name { get; init; } = "";
}

public record HistoryEntry
{
    [JsonPropertyName("historyKey")]
    public string? HistoryKey { get; init; }

    [JsonPropertyName("ratingKey")]
    public string RatingKey { get; init; } = "";

    [JsonPropertyName("parentRatingKey")]
    public string? ParentRatingKey { get; init; }

    [JsonPropertyName("grandparentRatingKey")]
    public string? GrandparentRatingKey { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = "";

    [JsonPropertyName("grandparentTitle")]
    public string? GrandparentTitle { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("viewedAt")]
    public long ViewedAt { get; init; }

    [JsonPropertyName("accountID")]
    public int? AccountID { get; init; }

    public string EffectiveKey => GrandparentRatingKey ?? RatingKey;
}
