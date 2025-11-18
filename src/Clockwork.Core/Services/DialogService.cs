namespace Clockwork.Core.Services;

/// <summary>
/// Service for native file and folder selection dialogs.
/// </summary>
public class DialogService : IApplicationService
{
    public void Initialize()
    {
        // Nothing to initialize
    }

    public void Update(double deltaTime)
    {
        // Nothing to update
    }

    public void Shutdown()
    {
        // Nothing to shutdown
    }

    public void Dispose()
    {
        // Nothing to dispose
    }

    /// <summary>
    /// Opens a file selection dialog.
    /// </summary>
    /// <param name="filter">File filter (e.g., "NDS ROM Files|*.nds|All Files|*.*")</param>
    /// <param name="title">Dialog title</param>
    /// <returns>Selected file path, or null if cancelled</returns>
    public string? OpenFileDialog(string filter = "All Files|*.*", string title = "Select File")
    {
        try
        {
            using var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = filter,
                Title = title,
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                return dialog.FileName;
            }
        }
        catch
        {
            // Silently fail on non-Windows platforms or if dialog fails
        }

        return null;
    }

    /// <summary>
    /// Opens a folder selection dialog.
    /// </summary>
    /// <param name="description">Dialog description</param>
    /// <returns>Selected folder path, or null if cancelled</returns>
    public string? OpenFolderDialog(string description = "Select Folder")
    {
        try
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = description,
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                return dialog.SelectedPath;
            }
        }
        catch
        {
            // Silently fail on non-Windows platforms or if dialog fails
        }

        return null;
    }
}
