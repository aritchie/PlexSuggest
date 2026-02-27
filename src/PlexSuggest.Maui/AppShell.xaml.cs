using PlexSuggest.Maui.Pages;

namespace PlexSuggest.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("library", typeof(LibraryPickerPage));
        Routing.RegisterRoute("categories", typeof(CategoriesPage));
        Routing.RegisterRoute("recommendations", typeof(RecommendationsPage));
    }
}
