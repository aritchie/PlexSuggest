using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Prompts;
using XenoAtom.Terminal.UI.Styling;
using PlexSuggest.Core.Configuration;
using PlexSuggest.Core.Plex;
using PlexSuggest.Core.Plex.Models;
using PlexSuggest.Core.Recommendations;

namespace PlexSuggest.Cli.UI;

public static class AppUI
{
    static readonly Color Accent = Color.Rgb(0, 191, 255);
    static readonly Color SuccessColor = Color.Rgb(0, 200, 83);
    static readonly Color GoldColor = Color.Rgb(255, 215, 0);
    static readonly Color MutedColor = Color.Rgb(128, 128, 128);
    static readonly Color ErrColor = Color.Rgb(255, 80, 80);

    static TerminalInstance T => Terminal.Instance;

    public static async Task RunAsync(string? serverUrl, string? token, bool reset)
    {
        WriteBanner();

        if (reset)
        {
            ConfigManager.Delete();
            WriteColored("Configuration reset.", SuccessColor);
            return;
        }

        var config = ResolveConfig(serverUrl, token);
        if (config is null)
        {
            WriteColored("No configuration provided. Exiting.", ErrColor);
            return;
        }

        WriteColored("Connecting to Plex server...", MutedColor);

        using var client = new PlexClient(config);
        string? serverName;
        try
        {
            serverName = await client.ValidateConnectionAsync();
        }
        catch (Exception ex)
        {
            WriteColored($"Failed to connect: {ex.Message}", ErrColor);
            return;
        }

        WriteColored($"Connected to: {serverName}", SuccessColor);

        var sections = await client.GetLibrarySectionsAsync();
        if (sections.Count == 0)
        {
            WriteColored("No movie or TV show libraries found.", ErrColor);
            return;
        }

        var selectedSection = PickLibrary(sections);
        if (selectedSection is null) return;

        WriteColored($"\nLoading library: {selectedSection.Title}...", Accent);

        var libTask = client.GetAllItemsAsync(selectedSection.Key);
        var histTask = client.GetWatchHistoryAsync();
        await Task.WhenAll(libTask, histTask);

        var allItems = libTask.Result;
        var history = histTask.Result;

        WriteColored($"Loaded {allItems.Count} items and {history.Count} history entries.", SuccessColor);

        var watched = allItems.Where(i => i.IsWatched).ToList();
        var unwatched = allItems.Where(i => i.IsUnwatched).ToList();

        if (watched.Count == 0)
        {
            WriteColored("No watched items found. Watch some content first!", GoldColor);
            return;
        }

        WriteColored($"Analyzed {watched.Count} watched, {unwatched.Count} unwatched items.", MutedColor);

        var profile = TasteProfile.Build(watched, history);
        var categories = RecommendationEngine.GenerateCategories(profile, unwatched);

        if (categories.Count == 0)
        {
            WriteColored("Not enough data to generate recommendations.", GoldColor);
            return;
        }

        while (true)
        {
            var chosenCategory = PickCategory(categories);
            if (chosenCategory is null) break;

            var shouldContinue = ShowRecommendations(chosenCategory);
            if (!shouldContinue) break;
        }

        WriteColored("\nThanks for using PlexSuggest! Happy watching!", Accent);
    }

    static void WriteBanner()
    {
        var title = new TextBlock("PlexSuggest");
        title.SetStyle(new TextBlockStyle
        {
            ForegroundBrush = Brush.LinearGradient(
                new GradientPoint(0f, 0f),
                new GradientPoint(1f, 0f),
                new GradientStop(0f, Accent),
                new GradientStop(1f, Color.Rgb(138, 43, 226)))
        });

        var subtitle = new TextBlock("Personalized recommendations from your Plex library");
        subtitle.SetStyle(new TextBlockStyle { Foreground = MutedColor });

        T.Write(new VStack(title, subtitle, new TextBlock("")));
    }

    static void WriteColored(string text, Color color)
    {
        var tb = new TextBlock(text);
        tb.SetStyle(new TextBlockStyle { Foreground = color });
        T.Write(tb);
    }

    static AppConfig? ResolveConfig(string? serverUrl, string? token)
    {
        // CLI args take precedence — save and use directly
        if (!string.IsNullOrWhiteSpace(serverUrl) && !string.IsNullOrWhiteSpace(token))
        {
            var config = new AppConfig(serverUrl, token);
            ConfigManager.Save(config);
            return config;
        }

        var fullConfig = ConfigManager.LoadConfig();

        if (fullConfig.Servers.Count > 0)
            return PickServer(fullConfig);

        return PromptAndAddServer();
    }

    static AppConfig? PickServer(PlexSuggestConfig config)
    {
        const string addNew = "+ Add new server";

        // Build display labels keyed to server entries
        var serverLabels = new List<(string Label, ServerEntry Server)>();
        foreach (var s in config.Servers)
        {
            var label = string.IsNullOrWhiteSpace(s.Name) ? s.ServerUrl : s.Name;
            var desc = string.IsNullOrWhiteSpace(s.Description) ? "" : $" — {s.Description}";
            var url = string.IsNullOrWhiteSpace(s.Name) ? "" : $" ({s.ServerUrl})";
            var marker = s.Id == config.LastServerId ? " [last used]" : "";
            serverLabels.Add(($"{label}{desc}{url}{marker}", s));
        }

        // Put last-used server first
        if (config.LastServerId is not null)
        {
            var lastIdx = serverLabels.FindIndex(x => x.Server.Id == config.LastServerId);
            if (lastIdx > 0)
            {
                var item = serverLabels[lastIdx];
                serverLabels.RemoveAt(lastIdx);
                serverLabels.Insert(0, item);
            }
        }

        var options = serverLabels.Select(x => x.Label).Append(addNew).ToArray();
        var prompt = new SelectionPrompt<string>("Select a server:").Items(options);
        var selected = T.Prompt(prompt);

        if (selected == addNew)
            return PromptAndAddServer();

        var match = serverLabels.FirstOrDefault(x => x.Label == selected);
        if (match.Server is null)
            return null;

        ConfigManager.SetLastServer(match.Server.Id);
        return match.Server.ToAppConfig();
    }

    static AppConfig? PromptAndAddServer()
    {
        WriteColored("Add New Server", Accent);

        var url = T.Ask("Server URL (e.g. http://192.168.1.100:32400):");
        if (string.IsNullOrWhiteSpace(url)) return null;

        var tok = T.Ask("Plex Token:");
        if (string.IsNullOrWhiteSpace(tok)) return null;

        var description = T.Ask("Description (e.g. Home Server, Office):");

        // Validate connection to get server name
        var appConfig = new AppConfig(url.Trim(), tok.Trim());
        string name = "";

        try
        {
            WriteColored("Validating connection...", MutedColor);
            using var client = new PlexClient(appConfig);
            name = client.ValidateConnectionAsync().GetAwaiter().GetResult() ?? "";
        }
        catch (Exception ex)
        {
            WriteColored($"Warning: Could not validate connection: {ex.Message}", GoldColor);
        }

        ConfigManager.AddServer(
            appConfig.ServerUrl,
            appConfig.Token,
            name,
            description?.Trim() ?? ""
        );

        return appConfig;
    }

    static LibrarySection? PickLibrary(List<LibrarySection> sections)
    {
        var options = sections.Select(s => $"{s.Title} ({s.Type})").ToArray();
        var prompt = new SelectionPrompt<string>("Select a library:").Items(options);
        var selected = T.Prompt(prompt);
        var idx = Array.IndexOf(options, selected);
        return idx >= 0 ? sections[idx] : null;
    }

    static Category? PickCategory(List<Category> categories)
    {
        var options = categories
            .Select(c => $"{c.Name} — {c.Description}")
            .Append("Quit")
            .ToArray();

        var prompt = new SelectionPrompt<string>("Choose a category:").Items(options);
        var selected = T.Prompt(prompt);
        var idx = Array.IndexOf(options, selected);
        return idx >= 0 && idx < categories.Count ? categories[idx] : null;
    }

    static bool ShowRecommendations(Category category)
    {
        WriteColored($"\n{category.Name}", Accent);
        WriteColored(category.Description, MutedColor);

        while (true)
        {
            var options = category.Items
                .Select(s => $"{s.Item.Title} ({s.Item.Year}) — Score: {s.Score:F0}")
                .Append("Back to Categories")
                .Append("Quit")
                .ToArray();

            var prompt = new SelectionPrompt<string>("Select a recommendation for details:").Items(options);
            var selected = T.Prompt(prompt);
            var idx = Array.IndexOf(options, selected);

            if (idx >= category.Items.Count)
                return selected == "Back to Categories";

            if (idx >= 0 && idx < category.Items.Count)
                ShowDetail(category.Items[idx]);
        }
    }

    static void ShowDetail(ScoredItem scored)
    {
        var m = scored.Item;

        var titleTb = new TextBlock($"  {m.Title}");
        titleTb.SetStyle(new TextBlockStyle { Foreground = Accent });

        var ratingTb = new TextBlock($"  Rating: {m.EffectiveRating:F1}/10");
        ratingTb.SetStyle(new TextBlockStyle { Foreground = GoldColor });

        var scoreTb = new TextBlock($"  Score: {scored.Score:F0}/100");
        scoreTb.SetStyle(new TextBlockStyle { Foreground = SuccessColor });

        var reasonTb = new TextBlock($"  {scored.Reason}");
        reasonTb.SetStyle(new TextBlockStyle { Foreground = MutedColor });

        var summaryTb = new TextBlock($"  {m.Summary ?? "No summary available."}");
        summaryTb.Wrap = true;

        T.Write(new VStack(
            new TextBlock(""),
            titleTb,
            new TextBlock($"  Year: {m.Year}"),
            ratingTb,
            new TextBlock($"  Content Rating: {m.ContentRating ?? "N/A"}"),
            new TextBlock($"  Genres: {string.Join(", ", m.Genres.Select(g => g.Name))}"),
            new TextBlock($"  Director: {string.Join(", ", m.Directors.Select(d => d.Name))}"),
            new TextBlock($"  Cast: {string.Join(", ", m.Roles.Select(a => a.Name).Take(5))}"),
            scoreTb,
            reasonTb,
            new TextBlock(""),
            summaryTb,
            new TextBlock("")
        ));
    }

    public static void ShowHelp()
    {
        WriteBanner();

        var usageTb = new TextBlock("Usage: plexsuggest [options]");
        usageTb.SetStyle(new TextBlockStyle { Foreground = Accent });

        var footerTb = new TextBlock("Server configurations are saved to ~/.plexsuggest/config.json");
        footerTb.SetStyle(new TextBlockStyle { Foreground = MutedColor });

        T.Write(new VStack(
            usageTb,
            new TextBlock(""),
            new TextBlock("Options:"),
            new TextBlock("  --server-url <url>   Plex server URL (e.g. http://192.168.1.100:32400)"),
            new TextBlock("  --token <token>      Plex authentication token"),
            new TextBlock("  --reset              Clear saved configuration"),
            new TextBlock("  --help               Show this help message"),
            new TextBlock(""),
            new TextBlock("Multiple servers can be saved and selected from the interactive picker."),
            footerTb
        ));
    }
}
