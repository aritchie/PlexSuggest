using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlexSuggest.Core.Configuration;
using PlexSuggest.Core.Plex;
using PlexSuggest.Core.Plex.Models;

namespace PlexSuggest.Maui.ViewModels;

public partial class LibraryPickerViewModel : ObservableObject
{
    [ObservableProperty] bool isBusy;
    [ObservableProperty] string statusMessage = "";

    public ObservableCollection<LibrarySection> Sections { get; } = [];

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

            Sections.Clear();
            foreach (var s in sections)
                Sections.Add(s);

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
        await Shell.Current.GoToAsync("categories", new Dictionary<string, object>
        {
            ["SectionKey"] = section.Key,
            ["SectionTitle"] = section.Title,
            ["SectionType"] = section.Type
        });
    }
}
