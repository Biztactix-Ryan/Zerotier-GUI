using System.Collections.Generic;
using Zerotier.Gui.Services;

namespace Zerotier.Gui.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly NetworkCache _networkCache;

    public MainWindowViewModel(NetworkCache networkCache)
    {
        _networkCache = networkCache;
        _networkCache.NetworksUpdated += (_, _) => NotifyNetworksUpdated();
    }

    public string Title => "Zerotier GUI";

    public string Subtitle => "Mint-first Avalonia shell with tray integration.";

    public IReadOnlyList<NetworkMenuModel> Networks => _networkCache.Networks;

    public void NotifyNetworksUpdated()
    {
        this.RaisePropertyChanged(nameof(Networks));
    }
}

public enum NetworkStatus
{
    Online,
    Offline,
    PortError,
}

public record NetworkMenuModel(
    string Name,
    string NetworkId,
    NetworkStatus Status,
    bool AllowDefault,
    bool AllowManaged,
    bool AllowGlobal)
{
    public string Label => $"{Name} ({NetworkId})";

    public string StatusLabel => Status switch
    {
        NetworkStatus.Online => "Online",
        NetworkStatus.PortError => "Port error",
        _ => "Offline",
    };
}
