using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlexSuggest.Core.Configuration;
using PlexSuggest.Core.Plex;
using PlexSuggest.Core.Recommendations;
using PlexSuggest.Maui.Pages;
using Shiny;

namespace PlexSuggest.Maui.ViewModels;

[ShellMap<CategoriesPage>("categories")]
public partial class CategoriesViewModel(INavigator navigator) : ObservableObject,
    IPageLifecycleAware
{
    [ObservableProperty]
    [property: ShellProperty]
    string sectionKey = "";

    [ObservableProperty]
    [property: ShellProperty]
    string sectionTitle = "";

    [ObservableProperty]
    [property: ShellProperty]
    string sectionType = "";

    [ObservableProperty] bool isBusy;
    [ObservableProperty] string statusMessage = "";
    [ObservableProperty] List<Category> categories = [];

    public async void OnAppearing()
    {
        if (Categories.Count == 0)
            await LoadCategoriesAsync();
    }

    public void OnDisappearing() { }

    [RelayCommand]
    async Task LoadCategoriesAsync()
    {
        var config = ConfigManager.Load();
        if (config is null || string.IsNullOrEmpty(SectionKey)) return;

        IsBusy = true;
        StatusMessage = "Loading library and analyzing your taste...";

        try
        {
            using var client = new PlexClient(config);

            var libTask = client.GetAllItemsAsync(SectionKey);
            var histTask = client.GetWatchHistoryAsync();
            await Task.WhenAll(libTask, histTask);

            var allItems = libTask.Result;
            var history = histTask.Result;

            var watched = allItems.Where(i => i.IsWatched).ToList();
            var unwatched = allItems.Where(i => i.IsUnwatched).ToList();

            if (watched.Count == 0)
            {
                StatusMessage = "No watched items found. Watch some content first!";
                IsBusy = false;
                return;
            }

            var profile = TasteProfile.Build(watched, history);
            Categories = RecommendationEngine.GenerateCategories(profile, unwatched);

            StatusMessage = $"Found {watched.Count} watched, {unwatched.Count} unwatched. Generated {Categories.Count} categories.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    async Task SelectCategoryAsync(Category category)
    {
        await navigator.NavigateTo<RecommendationsViewModel>(vm => vm.Category = category);
    }
}
