using PlexSuggest.Maui.ViewModels;

namespace PlexSuggest.Maui.Pages;

public partial class LibraryPickerPage : ContentPage
{
    readonly LibraryPickerViewModel _vm;

    public LibraryPickerPage(LibraryPickerViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_vm.Sections.Count == 0)
            await _vm.LoadSectionsCommand.ExecuteAsync(null);
    }
}
