using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlexSuggest.Core.Recommendations;

namespace PlexSuggest.Maui.ViewModels;

[QueryProperty(nameof(Category), "Category")]
public partial class RecommendationsViewModel : ObservableObject
{
    [ObservableProperty] Category? category;
    [ObservableProperty] ScoredItem? selectedItem;
    [ObservableProperty] bool showDetail;

    public ObservableCollection<ScoredItem> Items { get; } = [];

    partial void OnCategoryChanged(Category? value)
    {
        Items.Clear();
        if (value is null) return;

        foreach (var item in value.Items)
            Items.Add(item);
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
        await Shell.Current.GoToAsync("..");
    }
}
