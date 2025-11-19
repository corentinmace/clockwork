using Clockwork.Core;
using Clockwork.Core.Logging;
using ImGuiNET;
using System.Numerics;

namespace Clockwork.UI.Views;

/// <summary>
/// Window for viewing application logs in real-time
/// </summary>
public class LogViewerWindow : IView
{
    private readonly ApplicationContext _appContext;
    public bool IsVisible { get; set; } = false;

    private List<LogEntry> _displayedLogs = new();
    private bool _autoScroll = true;
    private LogLevel _filterLevel = LogLevel.Debug;
    private string _searchFilter = string.Empty;
    private float _refreshTimer = 0f;
    private const float RefreshInterval = 0.5f; // Refresh every 0.5 seconds

    // Colors for different log levels
    private static readonly Vector4 DebugColor = new(0.7f, 0.7f, 0.7f, 1.0f);     // Gray
    private static readonly Vector4 InfoColor = new(0.5f, 0.8f, 1.0f, 1.0f);      // Light blue
    private static readonly Vector4 WarningColor = new(1.0f, 0.8f, 0.3f, 1.0f);   // Orange
    private static readonly Vector4 ErrorColor = new(1.0f, 0.4f, 0.4f, 1.0f);     // Red
    private static readonly Vector4 FatalColor = new(1.0f, 0.2f, 0.2f, 1.0f);     // Dark red

    public LogViewerWindow(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Draw()
    {
        if (!IsVisible)
            return;

        // Update refresh timer
        _refreshTimer += ImGui.GetIO().DeltaTime;
        if (_refreshTimer >= RefreshInterval)
        {
            RefreshLogs();
            _refreshTimer = 0f;
        }

        bool isVisible = IsVisible;
        ImGui.SetNextWindowSize(new Vector2(1000, 600), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Log Viewer", ref isVisible, ImGuiWindowFlags.MenuBar))
        {
            DrawMenuBar();
            DrawToolbar();

            ImGui.Separator();

            // Log display area
            DrawLogTable();
        }
        ImGui.End();

        IsVisible = isVisible;
    }

    private void DrawMenuBar()
    {
        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("Open Log File"))
                {
                    OpenLogFile();
                }

                if (ImGui.MenuItem("Clear Logs"))
                {
                    ClearLogs();
                }

                ImGui.Separator();

                if (ImGui.MenuItem("Close"))
                {
                    IsVisible = false;
                }

                ImGui.EndMenu();
            }

            ImGui.EndMenuBar();
        }
    }

    private void DrawToolbar()
    {
        // Filter by level
        ImGui.Text("Filter Level:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        if (ImGui.BeginCombo("##filterlevel", _filterLevel.ToString()))
        {
            foreach (LogLevel level in Enum.GetValues(typeof(LogLevel)))
            {
                bool isSelected = _filterLevel == level;
                if (ImGui.Selectable(level.ToString(), isSelected))
                {
                    _filterLevel = level;
                }

                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }
            ImGui.EndCombo();
        }

        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();

        // Search filter
        ImGui.Text("Search:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200);
        ImGui.InputText("##search", ref _searchFilter, 256);

        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();

        // Auto-scroll toggle
        ImGui.Checkbox("Auto-scroll", ref _autoScroll);

        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();

        // Refresh button
        if (ImGui.Button("Refresh Now"))
        {
            RefreshLogs();
        }

        ImGui.SameLine();

        // Log count
        ImGui.Text($"({_displayedLogs.Count} entries)");
    }

    private void DrawLogTable()
    {
        float availableHeight = ImGui.GetContentRegionAvail().Y;

        ImGui.BeginChild("LogScrollArea", new Vector2(0, availableHeight), ImGuiChildFlags.Borders);

        if (ImGui.BeginTable("LogTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
        {
            ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 150);
            ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableHeadersRow();

            // Apply filters and display logs
            var filteredLogs = _displayedLogs
                .Where(log => log.Level >= _filterLevel)
                .Where(log => string.IsNullOrWhiteSpace(_searchFilter) ||
                             log.Message.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var log in filteredLogs)
            {
                ImGui.TableNextRow();

                // Timestamp column
                ImGui.TableSetColumnIndex(0);
                ImGui.Text(log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                // Level column
                ImGui.TableSetColumnIndex(1);
                Vector4 levelColor = GetLevelColor(log.Level);
                ImGui.TextColored(levelColor, log.Level.ToString());

                // Message column
                ImGui.TableSetColumnIndex(2);
                ImGui.TextWrapped(log.Message);
            }

            // Auto-scroll to bottom
            if (_autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
            {
                ImGui.SetScrollHereY(1.0f);
            }

            ImGui.EndTable();
        }

        ImGui.EndChild();
    }

    private Vector4 GetLevelColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => DebugColor,
            LogLevel.Info => InfoColor,
            LogLevel.Warning => WarningColor,
            LogLevel.Error => ErrorColor,
            LogLevel.Fatal => FatalColor,
            _ => new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
        };
    }

    private void RefreshLogs()
    {
        _displayedLogs = AppLogger.GetRecentLogs();
    }

    private void OpenLogFile()
    {
        try
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string logFilePath = Path.Combine(appDataPath, "Clockwork", "Logs", "application.log");

            if (File.Exists(logFilePath))
            {
                // Open with default text editor
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = logFilePath,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to open log file: {ex.Message}");
        }
    }

    private void ClearLogs()
    {
        _displayedLogs.Clear();
    }
}
