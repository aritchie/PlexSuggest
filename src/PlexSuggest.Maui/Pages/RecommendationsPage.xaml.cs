using PlexSuggest.Maui.ViewModels;

namespace PlexSuggest.Maui.Pages;

public partial class RecommendationsPage : ContentPage
{
    public RecommendationsPage(RecommendationsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
