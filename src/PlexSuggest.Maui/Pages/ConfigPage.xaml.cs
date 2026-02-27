using PlexSuggest.Maui.ViewModels;

namespace PlexSuggest.Maui.Pages;

public partial class ConfigPage : ContentPage
{
    public ConfigPage(ConfigViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
