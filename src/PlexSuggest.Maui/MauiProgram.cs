using CommunityToolkit.Maui;
using MauiDevFlow.Agent;
using PlexSuggest.Core.Configuration;
using PlexSuggest.Maui.ViewModels;

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
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<ConfigViewModel>();
        builder.Services.AddTransient<LibraryPickerViewModel>();
        builder.Services.AddTransient<CategoriesViewModel>();
        builder.Services.AddTransient<RecommendationsViewModel>();

        builder.Services.AddTransient<Pages.ConfigPage>();
        builder.Services.AddTransient<Pages.LibraryPickerPage>();
        builder.Services.AddTransient<Pages.CategoriesPage>();
        builder.Services.AddTransient<Pages.RecommendationsPage>();

#if DEBUG
        builder.AddMauiDevFlowAgent();
#endif

        return builder.Build();
    }
}
