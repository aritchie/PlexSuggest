# PlexSuggest

Personalized recommendations from your Plex library. Analyzes your viewing history to surface unwatched movies and TV shows you'll love — organized into dynamically generated categories based on your taste.

Available as a **CLI tool** (published as a dotnet tool) and a **.NET MAUI app** for iOS and Android.

## Features

- Connects directly to your Plex Media Server (no third-party services)
- Builds a taste profile from your watch history with recency-weighted analysis
- Generates personalized recommendation categories like "Top Picks for You", genre combos, "Because You Watched", and "Hidden Gems"
- Scores every unwatched item on a 0-100 scale with an explanation of why it was recommended
- Supports both movie and TV show libraries
- Configuration saved locally for repeat use
- AOT/trim-safe JSON serialization via source generation

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- .NET MAUI workload (for the mobile app): `dotnet workload install maui`
- A Plex Media Server with a valid authentication token

### Finding Your Plex Token

1. Sign in to Plex Web App
2. Browse to any media item and click "Get Info" > "View XML"
3. The URL will contain `X-Plex-Token=<your-token>`

Or visit: https://support.plex.tv/articles/204059436-finding-an-authentication-token-x-plex-token/

## Project Structure

```
PlexSuggest/
├── PlexSuggest.slnx
└── src/
    ├── PlexSuggest.Core/              # Shared library (net10.0 / ios / android)
    │   ├── Configuration/
    │   │   ├── AppConfig.cs           # Config record: ServerUrl + Token
    │   │   ├── AppConfigJsonContext.cs # Source-generated JSON serialization
    │   │   └── ConfigManager.cs       # Load/Save/Delete from ~/.plexsuggest/config.json
    │   ├── Plex/
    │   │   ├── PlexClient.cs          # HttpClient wrapper for Plex API
    │   │   └── Models/
    │   │       └── PlexResponse.cs    # All Plex API response models
    │   └── Recommendations/
    │       ├── TasteProfile.cs        # Weighted taste analysis with recency decay
    │       ├── RecommendationEngine.cs# Category generation + item scoring
    │       ├── Category.cs            # Category model + CategoryType enum
    │       └── ScoredItem.cs          # Metadata + score + reason
    │
    ├── PlexSuggest.Cli/               # CLI tool (net10.0, dotnet tool)
    │   ├── Program.cs                 # Arg parsing
    │   └── UI/
    │       └── AppUI.cs               # XenoAtom.Terminal.UI interactive flows
    │
    └── PlexSuggest.Maui/              # MAUI app (iOS + Android)
        ├── MauiProgram.cs             # DI + service registration
        ├── AppShell.xaml               # Shell navigation with routes
        ├── Converters.cs              # Value converters for XAML bindings
        ├── ViewModels/                # MVVM ViewModels (CommunityToolkit.Mvvm)
        │   ├── ConfigViewModel.cs
        │   ├── LibraryPickerViewModel.cs
        │   ├── CategoriesViewModel.cs
        │   └── RecommendationsViewModel.cs
        ├── Pages/                     # XAML pages
        │   ├── ConfigPage.xaml
        │   ├── LibraryPickerPage.xaml
        │   ├── CategoriesPage.xaml
        │   └── RecommendationsPage.xaml
        ├── Resources/Styles/          # Dark theme colors + global styles
        └── Platforms/                 # iOS + Android entry points
```

## Getting Started

### Build Everything

```bash
dotnet build
```

### CLI Tool

```bash
# Run directly
dotnet run --project src/PlexSuggest.Cli -- --server-url http://192.168.1.100:32400 --token YOUR_TOKEN

# Or install as a global tool
dotnet pack src/PlexSuggest.Cli -o ./nupkg
dotnet tool install --global --add-source ./nupkg PlexSuggest
plexsuggest
```

### MAUI App

```bash
# iOS (requires Mac with Xcode)
dotnet build src/PlexSuggest.Maui -f net10.0-ios

# Android
dotnet build src/PlexSuggest.Maui -f net10.0-android
```

## CLI Usage

```
Usage: plexsuggest [options]

Options:
  --server-url <url>   Plex server URL (e.g. http://192.168.1.100:32400)
  --token <token>      Plex authentication token
  --reset              Clear saved configuration
  --help               Show help message
```

All arguments are optional. If no server URL and token are provided, the tool checks for a saved configuration at `~/.plexsuggest/config.json`, then falls back to an interactive prompt.

### CLI Flow

1. **Configure** — provide credentials via args, saved config, or interactive prompt
2. **Validate** — tests connection to your Plex server
3. **Pick library** — select from your movie/TV show libraries
4. **Analyze** — fetches all items and watch history concurrently, builds taste profile
5. **Browse categories** — choose from personalized recommendation categories
6. **Explore recommendations** — view scored items with detail drill-down
7. **Loop** — go back to categories or quit

## MAUI App Flow

The mobile app follows the same logical flow with a dark-themed UI:

1. **Config Page** — enter server URL and token, connect
2. **Library Picker** — tap a library card to select it
3. **Categories** — browse generated categories with item counts
4. **Recommendations** — scroll items, tap for detail overlay with title, year, rating, genres, director, cast, score, reason, and summary

Navigation uses Shell routing: `config → library → categories → recommendations`

## Configuration

Stored at `~/.plexsuggest/config.json`:

```json
{
  "serverUrl": "http://192.168.1.100:32400",
  "token": "your-plex-token"
}
```

Both the CLI and MAUI app share this configuration file.

## Plex API Integration

All communication with Plex is direct HTTP using `System.Text.Json` — no third-party Plex NuGet packages.

| Purpose | Endpoint | Notes |
|---------|----------|-------|
| Validate connection | `GET /` | Returns server identity and friendly name |
| List libraries | `GET /library/sections` | Filtered to movie + show types |
| All library items | `GET /library/sections/{id}/all` | Paginated (100/page) via `X-Plex-Container-Start/Size` headers |
| Watch history | `GET /status/sessions/history/all` | Paginated, includes `viewedAt` timestamps |
| Item detail | `GET /library/metadata/{ratingKey}` | Full metadata for a single item |

All requests include `Accept: application/json` and `X-Plex-Token` headers.

## Recommendation Algorithm

### Taste Profile

Built from your watched items with **recency decay**:

```
recency = 1 / (1 + daysSinceWatched / 180)
```

Items watched within 6 months get strong weighting; older watches gradually decay.

**Tracked dimensions:**

| Dimension | Source | Notes |
|-----------|--------|-------|
| Genre weights | All genres per item | Frequency weighted by recency |
| Genre pair weights | Sorted pairs of co-occurring genres | E.g., "Action + Comedy" |
| Director weights | All directors per item | Frequency weighted by recency |
| Actor weights | Top 5 billed actors per item | Trimmed to top 50 overall |
| Decade weights | `(year / 10) * 10` | Weighted by recency |
| Top watched items | Top 10 by engagement | `viewCount * recency * (rating/10 + 0.5)` |

All weights are normalized to a 0–1 scale.

### Category Generation

Up to 7 categories are dynamically generated:

| Category | Type | Selection Logic |
|----------|------|-----------------|
| **Top Picks for You** | TopPicks | Highest general score against full profile. Always included. |
| **[Genre] [Genre]** (x2) | GenreCombo | Top genre pairs → items matching both genres. Min 3 items. |
| **Because You Watched [X]** | BecauseYouWatched | Similarity scoring against your top watched item. Min 3 items. |
| **Hidden Gems** | HiddenGems | Rating >= 7.0 but low genre-match score. Min 3 items. |
| **Director Spotlight: [Name]** | DirectorSpotlight | Top director with 3+ unwatched items. |
| **[Decade]s Deep Dive** | DecadeDeepDive | Top decade with 3+ unwatched items. |
| **Recently Added** | RecentlyAdded | Newest additions to library. Min 3 items. |

### Item Scoring (0–100)

Each unwatched item is scored against your taste profile:

| Component | Max Points | Calculation |
|-----------|-----------|-------------|
| Genre match | 40 | Average of matching genre weights × 40 |
| Director match | 15 | Best matching director weight × 15 |
| Actor match | 15 | Best matching actor weight × 15 |
| Rating bonus | 15 | `clamp(rating / 10, 0, 1) × 15` |
| Decade match | 10 | Decade weight × 10 |
| Content rating | 5 | 5 if content rating exists |

Each recommendation includes a human-readable reason explaining why it was suggested (e.g., "Genres you enjoy (Sci-Fi, Thriller) • Director: Denis Villeneuve").

### Similarity Scoring (for "Because You Watched")

| Component | Max Points | Calculation |
|-----------|-----------|-------------|
| Shared genres | 40 | `sharedCount / seedGenreCount × 40` |
| Same director | 20 | Flat bonus |
| Shared actors | 15 | `min(sharedCount × 5, 15)` |
| Same decade | 10 | Flat bonus |
| Rating bonus | 15 | `clamp(rating / 10, 0, 1) × 15` |

## TV Show Handling

- A show is **watched** (included in taste profiling) when `viewedLeafCount > 0` (at least one episode watched)
- A show is **unwatched** (eligible for recommendations) when `viewedLeafCount == 0`
- History entries are grouped by the parent show's `grandparentRatingKey` for deduplication
- Shows are treated as single units for scoring (genres, director, cast aggregated at series level)

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Shared library | .NET 10, C# 14, System.Text.Json (source-generated) |
| CLI | XenoAtom.Terminal.UI 1.1.1 |
| MAUI | .NET MAUI 10.0.30, CommunityToolkit.Mvvm 8.4.0, CommunityToolkit.Maui 14.0.0 |
| HTTP | HttpClient (direct Plex API, no Plex NuGet wrapper) |
| Architecture | Shared core library referenced by both CLI and MAUI projects |

## Dependencies

### PlexSuggest.Core
- No external NuGet dependencies (System.Text.Json is in-box)

### PlexSuggest.Cli
- [XenoAtom.Terminal.UI](https://github.com/XenoAtom/XenoAtom.Terminal.UI) 1.1.1 — reactive terminal UI framework

### PlexSuggest.Maui
- [Microsoft.Maui.Controls](https://github.com/dotnet/maui) 10.0.30
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) 8.4.0 — source-generated MVVM
- [CommunityToolkit.Maui](https://github.com/CommunityToolkit/Maui) 14.0.0 — MAUI extensions

## License

MIT
