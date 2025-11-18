namespace Clockwork.Core.Services;

/// <summary>
/// Service for file and folder selection.
/// Note: Native dialogs are not implemented. Use manual text input for paths.
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
    /// <param name="filter">File filter (not used in current implementation)</param>
    /// <param name="title">Dialog title (not used in current implementation)</param>
    /// <returns>Always returns null - use manual text input instead</returns>
    public string? OpenFileDialog(string filter = "All Files|*.*", string title = "Select File")
    {
        // Native file dialogs not implemented - users should type paths manually
        return null;
    }

    /// <summary>
    /// Opens a folder selection dialog.
    /// </summary>
    /// <param name="description">Dialog description (not used in current implementation)</param>
    /// <returns>Always returns null - use manual text input instead</returns>
    public string? OpenFolderDialog(string description = "Select Folder")
    {
        // Native folder dialogs not implemented - users should type paths manually
        return null;
    }
}
