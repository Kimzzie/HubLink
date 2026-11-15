using System;
using System.Net.Http;
using System.Threading;
using HubLink.Application;
using HubLink.Desktop.ViewModels;
using HubLink.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HubLink.Desktop;

public static class CompositionRoot
{
    public static ServiceProvider Build()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var settings = configuration
            .GetSection(HubLinkSettings.SectionName)
            .Get<HubLinkSettings>() ?? new HubLinkSettings();

        var services = new ServiceCollection();

        services.AddSingleton(settings);

        // One shared client. The per-request timeout is enforced by HttpShipmentService.
        services.AddSingleton(new HttpClient { Timeout = Timeout.InfiniteTimeSpan });

        services.AddSingleton<IShipmentService, HttpShipmentService>();
        services.AddSingleton<ICsvExporter, CsvExporter>();
        services.AddSingleton<IExportFilePicker, AvaloniaExportFilePicker>();
        services.AddSingleton<MainViewModel>();

        return services.BuildServiceProvider();
    }
}