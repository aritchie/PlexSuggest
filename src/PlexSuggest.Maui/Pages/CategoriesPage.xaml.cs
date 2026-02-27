using PlexSuggest.Maui.ViewModels;

namespace PlexSuggest.Maui.Pages;

public partial class CategoriesPage : ContentPage
{
    readonly CategoriesViewModel _vm;

    public CategoriesPage(CategoriesViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_vm.Categories.Count == 0)
            await _vm.LoadCategoriesCommand.ExecuteAsync(null);
    }
}
