using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlexSuggest.Core.Recommendations;
using PlexSuggest.Maui.Pages;
using Shiny;

namespace PlexSuggest.Maui.ViewModels;

[ShellMap<RecommendationsPage>("recommendations")]
public partial class RecommendationsViewModel(INavigator navigator) : ObservableObject
{
    [ObservableProperty]
    [property: ShellProperty]
    Category? category;

    [ObservableProperty] ScoredItem? selectedItem;
    [ObservableProperty] bool showDetail;
    [ObservableProperty] List<ScoredItem> items = [];

    partial void OnCategoryChanged(Category? value)
    {
        Items = value is null ? [] : [.. value.Items];
    }

    [RelayCommand]
    void SelectItem(ScoredItem item)
    {
        SelectedItem = item;
        ShowDetail = true;
    }

    [RelayCommand]
    void CloseDetail()
    {
        ShowDetail = false;
    }

    [RelayCommand]
    async Task GoBackAsync()
    {
        await navigator.GoBack();
    }
}
