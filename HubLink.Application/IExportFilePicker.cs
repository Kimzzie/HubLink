namespace HubLink.Application;

public interface IExportFilePicker
{
    // Returns the chosen path, or null if the user cancelled.
    Task<string?> PickSavePathAsync(string suggestedFileName);
}