using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Zerotier.Gui.Services;
using Zerotier.Gui.ViewModels;

namespace Zerotier.Gui;

public partial class App : Application
{
    private TrayIcon? _trayIcon;
    private MainWindowViewModel? _viewModel;
    private NetworkCache? _networkCache;
    private CancellationTokenSource? _refreshCts;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _networkCache = new NetworkCache();
            _viewModel = new MainWindowViewModel(_networkCache);

            desktop.MainWindow = new MainWindow
            {
                DataContext = _viewModel
            };

            _trayIcon = Resources["MainTrayIcon"] as TrayIcon;

            if (_trayIcon != null)
            {
                _trayIcon.Clicked += OnTrayShowClicked;
                _trayIcon.Menu = BuildTrayMenu();
            }

            _networkCache!.NetworksUpdated += (_, _) => Dispatcher.UIThread.Post(OnNetworksUpdated);
            _refreshCts = new CancellationTokenSource();
            _ = Task.Run(() => RefreshLoopAsync(_refreshCts.Token));
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task RefreshLoopAsync(CancellationToken cancellationToken)
    {
        if (_networkCache is null)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _networkCache.RefreshAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Network refresh failed: {ex.Message}");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void OnTrayShowClicked(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        if (desktop.MainWindow is null)
        {
            _viewModel ??= new MainWindowViewModel(_networkCache ?? new NetworkCache());

            desktop.MainWindow = new MainWindow
            {
                DataContext = _viewModel
            };
        }

        desktop.MainWindow.Show();
        desktop.MainWindow.Activate();
    }

    private void OnTrayExitClicked(object? sender, EventArgs e)
    {
        _refreshCts?.Cancel();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void OnNetworksUpdated()
    {
        _viewModel?.NotifyNetworksUpdated();

        if (_trayIcon != null)
        {
            _trayIcon.Menu = BuildTrayMenu();
        }
    }

    private NativeMenu BuildTrayMenu()
    {
        var menu = new NativeMenu
        {
            new NativeMenuItem("Open", OnTrayShowClicked),
        };

        if (_viewModel != null)
        {
            var networksItem = new NativeMenuItem("Networks")
            {
                Menu = new NativeMenu(),
            };

            foreach (var network in _viewModel.Networks)
            {
                var networkMenu = new NativeMenu
                {
                    new NativeMenuItem($"Status: {network.StatusLabel}") { IsEnabled = false },
                    new NativeMenuItem($"ID: {network.NetworkId}") { IsEnabled = false },
                    new NativeMenuItemSeparator(),
                    new NativeMenuItem("Allow default route")
                    {
                        IsChecked = network.AllowDefault,
                        ToggleType = NativeMenuItemToggleType.CheckBox,
                        Click = (_, _) => OnToggle("allowDefault", network),
                    },
                    new NativeMenuItem("Allow managed routes/DNS")
                    {
                        IsChecked = network.AllowManaged,
                        ToggleType = NativeMenuItemToggleType.CheckBox,
                        Click = (_, _) => OnToggle("allowManaged", network),
                    },
                    new NativeMenuItem("Allow global IPv6")
                    {
                        IsChecked = network.AllowGlobal,
                        ToggleType = NativeMenuItemToggleType.CheckBox,
                        Click = (_, _) => OnToggle("allowGlobal", network),
                    },
                };

                networksItem.Menu!.Add(new NativeMenuItem(network.Label)
                {
                    Menu = networkMenu,
                });
            }

            menu.Add(networksItem);
        }

        menu.Add(new NativeMenuItemSeparator());
        menu.Add(new NativeMenuItem("Quit", OnTrayExitClicked));
        return menu;
    }

    private static void OnToggle(string key, NetworkMenuModel network)
    {
        // Placeholder toggle handler to be replaced with CLI calls.
        Console.WriteLine($"Toggle '{key}' for {network.NetworkId} requested");
    }
}
