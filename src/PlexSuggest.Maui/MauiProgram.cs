using CommunityToolkit.Maui;
using PlexSuggest.Core.Configuration;
using Shiny;

namespace PlexSuggest.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        ConfigManager.SetConfigDirectory(FileSystem.AppDataDirectory);

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseShinyShell(x => x.AddGeneratedMaps())
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        return builder.Build();
    }
}
