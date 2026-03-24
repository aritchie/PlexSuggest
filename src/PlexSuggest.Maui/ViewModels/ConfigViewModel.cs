using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlexSuggest.Core.Configuration;
using PlexSuggest.Core.Plex;
using PlexSuggest.Maui.Pages;
using Shiny;

namespace PlexSuggest.Maui.ViewModels;

[ShellMap<ConfigPage>(registerRoute: false)]
public partial class ConfigViewModel(INavigator navigator, IDialogs dialogs) : ObservableObject,
    IPageLifecycleAware
{
    [ObservableProperty] List<ServerEntry> servers = [];
    [ObservableProperty] bool hasServers;
    [ObservableProperty] bool showAddForm;

    // Add-server form fields
    [ObservableProperty] string serverUrl = "";
    [ObservableProperty] string token = "";
    [ObservableProperty] string description = "";

    [ObservableProperty] string statusMessage = "";
    [ObservableProperty] bool isBusy;

    public void OnAppearing() => LoadServers();
    public void OnDisappearing() { }

    void LoadServers()
    {
        var config = ConfigManager.LoadConfig();
        Servers = [.. config.Servers];
        HasServers = Servers.Count > 0;
        ShowAddForm = !HasServers;
    }

    [RelayCommand]
    async Task ConnectToServerAsync(ServerEntry server)
    {
        IsBusy = true;
        StatusMessage = "Connecting...";

        try
        {
            var appConfig = server.ToAppConfig();
            using var client = new PlexClient(appConfig);
            var name = await client.ValidateConnectionAsync();

            // Update name if it changed
            if (name is not null && name != server.Name)
            {
                var updated = server with { Name = name };
                ConfigManager.UpdateServer(updated);
            }

            ConfigManager.SetLastServer(server.Id);
            StatusMessage = $"Connected to: {name}";

            await navigator.NavigateTo<LibraryPickerViewModel>();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Connection failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    async Task AddServerAsync()
    {
        if (string.IsNullOrWhiteSpace(ServerUrl) || string.IsNullOrWhiteSpace(Token))
        {
            StatusMessage = "Please enter both server URL and token.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Connecting...";

        try
        {
            var appConfig = new AppConfig(ServerUrl.Trim(), Token.Trim());
            using var client = new PlexClient(appConfig);
            var name = await client.ValidateConnectionAsync() ?? "";

            ConfigManager.AddServer(
                appConfig.ServerUrl,
                appConfig.Token,
                name,
                Description.Trim()
            );

            StatusMessage = $"Connected to: {name}";

            // Clear form and refresh
            ServerUrl = "";
            Token = "";
            Description = "";
            LoadServers();

            await navigator.NavigateTo<LibraryPickerViewModel>();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Connection failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    void ShowAddServerForm()
    {
        StatusMessage = "";
        ShowAddForm = true;
    }

    [RelayCommand]
    void CancelAddServer()
    {
        ServerUrl = "";
        Token = "";
        Description = "";
        StatusMessage = "";
        ShowAddForm = false;
    }

    [RelayCommand]
    async Task RemoveServerAsync(ServerEntry server)
    {
        var confirm = await dialogs.Confirm(
            "Remove Server",
            $"Remove \"{(string.IsNullOrWhiteSpace(server.Name) ? server.ServerUrl : server.Name)}\"?");

        if (!confirm) return;

        ConfigManager.RemoveServer(server.Id);
        LoadServers();
        StatusMessage = "Server removed.";
    }

    [RelayCommand]
    void ResetAll()
    {
        ConfigManager.Delete();
        Servers = [];
        HasServers = false;
        ShowAddForm = true;
        ServerUrl = "";
        Token = "";
        Description = "";
        StatusMessage = "All configuration cleared.";
    }
}
