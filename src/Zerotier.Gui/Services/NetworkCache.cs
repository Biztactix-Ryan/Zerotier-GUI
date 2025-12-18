using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zerotier.Gui.ViewModels;

namespace Zerotier.Gui.Services;

public class NetworkCache
{
    private readonly List<NetworkMenuModel> _networks = new()
    {
        new("Home LAN", "8056c2e21c000001", NetworkStatus.Online,
            allowDefault: true, allowManaged: true, allowGlobal: false),
        new("Lab", "c3po1ab123000abc", NetworkStatus.Online,
            allowDefault: false, allowManaged: true, allowGlobal: true),
        new("Guest", "faded12345000002", NetworkStatus.Offline,
            allowDefault: false, allowManaged: false, allowGlobal: false),
    };

    public event EventHandler? NetworksUpdated;

    public IReadOnlyList<NetworkMenuModel> Networks => _networks;

    public DateTimeOffset LastRefreshed { get; private set; } = DateTimeOffset.MinValue;

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        // Placeholder: replace with Zerotier CLI invocation and parsing.
        await Task.CompletedTask;

        LastRefreshed = DateTimeOffset.UtcNow;
        NetworksUpdated?.Invoke(this, EventArgs.Empty);
    }
}
