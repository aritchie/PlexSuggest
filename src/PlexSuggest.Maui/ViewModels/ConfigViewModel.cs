using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlexSuggest.Core.Configuration;
using PlexSuggest.Core.Plex;

namespace PlexSuggest.Maui.ViewModels;

public partial class ConfigViewModel : ObservableObject
{
    [ObservableProperty] ObservableCollection<ServerEntry> servers = [];
    [ObservableProperty] bool hasServers;
    [ObservableProperty] bool showAddForm;

    // Add-server form fields
    [ObservableProperty] string serverUrl = "";
    [ObservableProperty] string token = "";
    [ObservableProperty] string description = "";

    [ObservableProperty] string statusMessage = "";
    [ObservableProperty] bool isBusy;

    public ConfigViewModel()
    {
        LoadServers();
    }

    void LoadServers()
    {
        var config = ConfigManager.LoadConfig();
        Servers = new ObservableCollection<ServerEntry>(config.Servers);
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

            await Shell.Current.GoToAsync("library");
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

            await Shell.Current.GoToAsync("library");
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
        var confirm = await Shell.Current.DisplayAlertAsync(
            "Remove Server",
            $"Remove \"{(string.IsNullOrWhiteSpace(server.Name) ? server.ServerUrl : server.Name)}\"?",
            "Remove",
            "Cancel");

        if (!confirm) return;

        ConfigManager.RemoveServer(server.Id);
        LoadServers();
        StatusMessage = "Server removed.";
    }

    [RelayCommand]
    void ResetAll()
    {
        ConfigManager.Delete();
        Servers.Clear();
        HasServers = false;
        ShowAddForm = true;
        ServerUrl = "";
        Token = "";
        Description = "";
        StatusMessage = "All configuration cleared.";
    }
}
