using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlexSuggest.Core.Configuration;
using PlexSuggest.Core.Plex;
using PlexSuggest.Core.Recommendations;

namespace PlexSuggest.Maui.ViewModels;

[QueryProperty(nameof(SectionKey), "SectionKey")]
[QueryProperty(nameof(SectionTitle), "SectionTitle")]
[QueryProperty(nameof(SectionType), "SectionType")]
public partial class CategoriesViewModel : ObservableObject
{
    [ObservableProperty] string sectionKey = "";
    [ObservableProperty] string sectionTitle = "";
    [ObservableProperty] string sectionType = "";
    [ObservableProperty] bool isBusy;
    [ObservableProperty] string statusMessage = "";

    public ObservableCollection<Category> Categories { get; } = [];

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
            var categories = RecommendationEngine.GenerateCategories(profile, unwatched);

            Categories.Clear();
            foreach (var c in categories)
                Categories.Add(c);

            StatusMessage = $"Found {watched.Count} watched, {unwatched.Count} unwatched. Generated {categories.Count} categories.";
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
        await Shell.Current.GoToAsync("recommendations", new Dictionary<string, object>
        {
            ["Category"] = category
        });
    }
}
