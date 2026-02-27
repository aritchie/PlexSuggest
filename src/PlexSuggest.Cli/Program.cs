using PlexSuggest.Cli.UI;

string? serverUrl = null;
string? token = null;
var reset = false;
var showHelp = false;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--server-url" when i + 1 < args.Length:
            serverUrl = args[++i];
            break;
        case "--token" when i + 1 < args.Length:
            token = args[++i];
            break;
        case "--reset":
            reset = true;
            break;
        case "--help":
        case "-h":
            showHelp = true;
            break;
    }
}

if (showHelp)
{
    AppUI.ShowHelp();
    return;
}

await AppUI.RunAsync(serverUrl, token, reset);
