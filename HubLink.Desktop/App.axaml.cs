using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HubLink.Desktop.ViewModels;
using HubLink.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace HubLink.Desktop;

public partial class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = CompositionRoot.Build();
            var viewModel = services.GetRequiredService<MainViewModel>();

            var window = new MainWindow { DataContext = viewModel };

            // Load shipments once the window is visible, so startup never blocks on HTTP.
            window.Opened += async (_, _) => await viewModel.LoadAsync();

            desktop.MainWindow = window;
            desktop.Exit += (_, _) => services.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}