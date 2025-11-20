using System.Collections.Concurrent;
using System.Text;

namespace Clockwork.Core.Logging;

/// <summary>
/// Application-wide logger with file output and in-memory buffering
/// </summary>
public static class AppLogger
{
    private static string _logDirectory = string.Empty;
    private static string _logFilePath = string.Empty;
    private static readonly object _fileLock = new();
    private static int _writeCount = 0;
    private static readonly int MaxLogLines = 500;
    private static readonly int TrimCheckInterval = 10;

    /// <summary>
    /// Thread-safe collection of recent log entries for UI display
    /// </summary>
    private static readonly ConcurrentQueue<LogEntry> _recentLogs = new();
    private static readonly int MaxBufferedLogs = 1000;

    /// <summary>
    /// Minimum log level to record
    /// </summary>
    public static LogLevel MinimumLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Initializes the logger with AppData directory
    /// </summary>
    public static void Initialize()
    {
        try
        {
            // Use AppData/Clockwork for logs
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string clockworkPath = Path.Combine(appDataPath, "Clockwork");
            _logDirectory = Path.Combine(clockworkPath, "Logs");

            Directory.CreateDirectory(_logDirectory);

            _logFilePath = Path.Combine(_logDirectory, "application.log");

            Info("AppLogger initialized");
            Info($"Log file: {_logFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize AppLogger: {ex.Message}");
        }
    }

    /// <summary>
    /// Logs a message with specified level
    /// </summary>
    public static void Log(LogLevel level, string message)
    {
        if (level < MinimumLevel)
            return;

        try
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message
            };

            // Add to in-memory buffer
            _recentLogs.Enqueue(entry);
            while (_recentLogs.Count > MaxBufferedLogs)
            {
                _recentLogs.TryDequeue(out _);
            }

            // Write to file
            if (!string.IsNullOrEmpty(_logFilePath))
            {
                string formattedMessage = FormatLogEntry(entry);

                lock (_fileLock)
                {
                    File.AppendAllText(_logFilePath, formattedMessage + Environment.NewLine);
                    _writeCount++;

                    // Periodically trim log file
                    if (_writeCount % TrimCheckInterval == 0)
                    {
                        TrimLogFile();
                    }
                }
            }

            // Also write to console for debugging
            Console.WriteLine($"[{level}] {message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to log message: {ex.Message}");
        }
    }

    /// <summary>
    /// Formats a log entry to string
    /// </summary>
    private static string FormatLogEntry(LogEntry entry)
    {
        return $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] {entry.Message}";
    }

    /// <summary>
    /// Trims log file if it exceeds MaxLogLines
    /// </summary>
    private static void TrimLogFile()
    {
        try
        {
            if (!File.Exists(_logFilePath))
                return;

            var lines = File.ReadAllLines(_logFilePath);
            if (lines.Length > MaxLogLines)
            {
                var recentLines = lines.Skip(lines.Length - MaxLogLines).ToArray();
                File.WriteAllLines(_logFilePath, recentLines);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to trim log file: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets recent log entries from memory buffer
    /// </summary>
    public static List<LogEntry> GetRecentLogs()
    {
        return _recentLogs.ToList();
    }

    /// <summary>
    /// Gets recent log entries as formatted strings
    /// </summary>
    public static List<string> GetRecentLogsFormatted()
    {
        return _recentLogs.Select(FormatLogEntry).ToList();
    }

    /// <summary>
    /// Gets the last error or fatal message (for status bar display)
    /// </summary>
    public static LogEntry? GetLastError()
    {
        return _recentLogs
            .Where(e => e.Level == LogLevel.Error || e.Level == LogLevel.Fatal)
            .LastOrDefault();
    }

    // Convenience methods for different log levels

    public static void Debug(string message) => Log(LogLevel.Debug, message);
    public static void Info(string message) => Log(LogLevel.Info, message);
    public static void Warn(string message) => Log(LogLevel.Warning, message);
    public static void Error(string message) => Log(LogLevel.Error, message);
    public static void Fatal(string message) => Log(LogLevel.Fatal, message);
}

/// <summary>
/// Log severity levels
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}

/// <summary>
/// Represents a single log entry
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
}
