using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlexSuggest.Core.Configuration;
using PlexSuggest.Core.Plex;
using PlexSuggest.Core.Plex.Models;
using PlexSuggest.Maui.Pages;
using Shiny;

namespace PlexSuggest.Maui.ViewModels;

[ShellMap<LibraryPickerPage>("library")]
public partial class LibraryPickerViewModel(INavigator navigator) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] bool isBusy;
    [ObservableProperty] string statusMessage = "";
    [ObservableProperty] List<LibrarySection> sections = [];

    public async void OnAppearing()
    {
        if (Sections.Count == 0)
            await LoadSectionsAsync();
    }

    public void OnDisappearing() { }

    [RelayCommand]
    async Task LoadSectionsAsync()
    {
        var config = ConfigManager.Load();
        if (config is null)
        {
            StatusMessage = "No configuration found.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Loading libraries...";

        try
        {
            using var client = new PlexClient(config);
            var sections = await client.GetLibrarySectionsAsync();
            Sections = [.. sections];
            StatusMessage = $"Found {sections.Count} libraries.";
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
    async Task SelectSectionAsync(LibrarySection section)
    {
        await navigator.NavigateTo<CategoriesViewModel>(vm =>
        {
            vm.SectionKey = section.Key;
            vm.SectionTitle = section.Title;
            vm.SectionType = section.Type;
        });
    }
}
