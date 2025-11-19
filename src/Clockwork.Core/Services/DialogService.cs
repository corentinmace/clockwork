using NativeFileDialogSharp;

namespace Clockwork.Core.Services;

/// <summary>
/// Service for cross-platform file and folder selection dialogs.
/// Uses native OS dialogs via NativeFileDialogSharp.
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
    /// Opens a native file selection dialog.
    /// </summary>
    /// <param name="filter">File filter in format "Description|*.ext|Description2|*.ext2" (Windows Forms style)</param>
    /// <param name="title">Dialog title (not used by NativeFileDialogSharp)</param>
    /// <returns>Selected file path, or null if cancelled</returns>
    public string? OpenFileDialog(string filter = "All Files|*.*", string title = "Select File")
    {
        try
        {
            // Convert Windows Forms filter format to NFD format
            // "NDS ROM Files|*.nds|All Files|*.*" -> "nds"
            string? nfdFilter = null;
            if (!string.IsNullOrEmpty(filter) && filter != "All Files|*.*")
            {
                var parts = filter.Split('|');
                if (parts.Length >= 2)
                {
                    var extension = parts[1].Replace("*.", "").Replace("*", "");
                    if (!string.IsNullOrEmpty(extension) && extension != ".")
                    {
                        nfdFilter = extension;
                    }
                }
            }

            DialogResult result;
            if (nfdFilter != null)
            {
                result = Dialog.FileOpen(nfdFilter);
            }
            else
            {
                result = Dialog.FileOpen();
            }

            if (result.IsOk)
            {
                return result.Path;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening file dialog: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Opens a native folder selection dialog.
    /// </summary>
    /// <param name="description">Dialog description (not used by NativeFileDialogSharp)</param>
    /// <returns>Selected folder path, or null if cancelled</returns>
    public string? OpenFolderDialog(string description = "Select Folder")
    {
        try
        {
            var result = Dialog.FolderPicker();

            if (result.IsOk)
            {
                return result.Path;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening folder dialog: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Opens a native file save dialog.
    /// </summary>
    /// <param name="filter">File filter in format "Description|*.ext|Description2|*.ext2" (Windows Forms style)</param>
    /// <param name="title">Dialog title (not used by NativeFileDialogSharp)</param>
    /// <param name="defaultName">Default file name</param>
    /// <returns>Selected file path for saving, or null if cancelled</returns>
    public string? SaveFileDialog(string filter = "All Files|*.*", string title = "Save File", string defaultName = "")
    {
        try
        {
            // Convert Windows Forms filter format to NFD format
            // "NDS ROM Files|*.nds|All Files|*.*" -> "nds"
            string? nfdFilter = null;
            if (!string.IsNullOrEmpty(filter) && filter != "All Files|*.*")
            {
                var parts = filter.Split('|');
                if (parts.Length >= 2)
                {
                    var extension = parts[1].Replace("*.", "").Replace("*", "");
                    if (!string.IsNullOrEmpty(extension) && extension != ".")
                    {
                        nfdFilter = extension;
                    }
                }
            }

            DialogResult result;
            if (nfdFilter != null)
            {
                result = Dialog.FileSave(nfdFilter, defaultName);
            }
            else
            {
                result = Dialog.FileSave(null, defaultName);
            }

            if (result.IsOk)
            {
                return result.Path;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening save file dialog: {ex.Message}");
        }

        return null;
    }
}
