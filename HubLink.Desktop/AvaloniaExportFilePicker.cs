using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using HubLink.Application;

namespace HubLink.Desktop;

public sealed class AvaloniaExportFilePicker : IExportFilePicker
{
    public async Task<string?> PickSavePathAsync(string suggestedFileName)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime
                is not IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
            return null;

        var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export shipments to CSV",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "csv",
            FileTypeChoices = [new FilePickerFileType("CSV file") { Patterns = ["*.csv"] }]
        });

        return file?.TryGetLocalPath();   // null when the user cancels
    }
}