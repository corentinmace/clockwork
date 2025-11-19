using System.Drawing;

namespace Clockwork.Core.Services;

/// <summary>
/// Service for loading and managing ColorTable files (.ctb) for matrix cell coloring.
/// Based on LiTRE's ColorTable implementation.
/// </summary>
public class ColorTableService : IApplicationService
{
    private ApplicationContext? _appContext;

    // Dictionary mapping map IDs to background/foreground colors
    public Dictionary<List<uint>, (Color Background, Color Foreground)> ColorDictionary { get; private set; } = new();

    // Path to currently loaded color table
    public string? CurrentColorTablePath { get; private set; }

    public void Initialize()
    {
        // Initialize with default (empty cell color)
        ResetColorTable();
    }

    public void Update(double deltaTime)
    {
        // No per-frame updates needed
    }

    public void Dispose()
    {
        ColorDictionary.Clear();
    }

    /// <summary>
    /// Load a color table from a .ctb file.
    /// </summary>
    /// <param name="filePath">Path to the .ctb file</param>
    /// <param name="silent">If true, don't throw exceptions on errors</param>
    /// <returns>True if loaded successfully</returns>
    public bool LoadColorTable(string filePath, bool silent = false)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        if (!File.Exists(filePath))
        {
            if (!silent)
                throw new FileNotFoundException($"Color table file not found: {filePath}");
            return false;
        }

        try
        {
            string[] lines = File.ReadAllLines(filePath);

            if (lines.Length == 0)
            {
                if (!silent)
                    throw new FormatException("Color table file is empty");
                return false;
            }

            var newColorDict = new Dictionary<List<uint>, (Color Background, Color Foreground)>();
            List<string> errorLines = new();

            const string MAP_KEYWORD = "[Maplist]";
            const string COLOR_KEYWORD = "[Color]";
            const string TEXT_COLOR_KEYWORD = "[TextColor]";
            const string SEPARATOR = "-";

            for (int lineNum = 0; lineNum < lines.Length; lineNum++)
            {
                string line = lines[lineNum].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                try
                {
                    // Split line by spaces
                    string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    int index = 0;

                    // Parse [Maplist]
                    if (parts[index] != MAP_KEYWORD)
                        throw new FormatException($"Expected {MAP_KEYWORD}");
                    index++;

                    // Parse map IDs
                    List<uint> mapList = new();
                    while (index < parts.Length && parts[index] != SEPARATOR)
                    {
                        if (parts[index] == "and")
                        {
                            index++;
                            continue;
                        }

                        uint firstValue = uint.Parse(parts[index++]);
                        mapList.Add(firstValue);

                        // Handle range (e.g., "5 to 10")
                        if (index < parts.Length && parts[index] == "to")
                        {
                            index++;
                            uint finalValue = uint.Parse(parts[index++]);

                            // Ensure firstValue <= finalValue
                            if (firstValue > finalValue)
                            {
                                (firstValue, finalValue) = (finalValue, firstValue);
                            }

                            // Add all values in range
                            for (uint id = firstValue + 1; id <= finalValue; id++)
                            {
                                mapList.Add(id);
                            }
                        }
                    }

                    // Expect separator
                    if (parts[index] != SEPARATOR)
                        throw new FormatException($"Expected {SEPARATOR}");
                    index++;

                    // Parse [Color] RGB
                    if (parts[index] != COLOR_KEYWORD)
                        throw new FormatException($"Expected {COLOR_KEYWORD}");
                    index++;

                    int r = int.Parse(parts[index++]);
                    int g = int.Parse(parts[index++]);
                    int b = int.Parse(parts[index++]);
                    Color backgroundColor = Color.FromArgb(r, g, b);

                    // Expect separator
                    if (parts[index] != SEPARATOR)
                        throw new FormatException($"Expected {SEPARATOR}");
                    index++;

                    // Parse [TextColor]
                    if (parts[index] != TEXT_COLOR_KEYWORD)
                        throw new FormatException($"Expected {TEXT_COLOR_KEYWORD}");
                    index++;

                    string colorName = parts[index++];
                    Color foregroundColor = Color.FromName(colorName);

                    // Add to dictionary
                    newColorDict.Add(mapList, (backgroundColor, foregroundColor));
                }
                catch (Exception ex)
                {
                    errorLines.Add($"Line {lineNum + 1}: {ex.Message}");
                    if (!silent)
                        continue; // Collect all errors
                }
            }

            // Add default color for empty cells (65535)
            newColorDict.Add(new List<uint> { 65535 }, (Color.Black, Color.White));

            // Success - replace dictionary
            ColorDictionary = newColorDict;
            CurrentColorTablePath = filePath;

            if (errorLines.Count > 0 && !silent)
            {
                string errorMsg = $"Color table loaded with {errorLines.Count} errors:\n" +
                                  string.Join("\n", errorLines.Take(10));
                throw new FormatException(errorMsg);
            }

            return true;
        }
        catch (Exception ex)
        {
            if (!silent)
                throw new Exception($"Failed to load color table: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Reset to default color table (empty).
    /// </summary>
    public void ResetColorTable()
    {
        ColorDictionary.Clear();
        // Default: Empty cells are black background with white text
        ColorDictionary.Add(new List<uint> { 65535 }, (Color.Black, Color.White));
        CurrentColorTablePath = null;
    }

    /// <summary>
    /// Get the color for a given map ID.
    /// </summary>
    /// <param name="mapID">The map ID to look up</param>
    /// <returns>Tuple of (background, foreground) colors, or null if not found</returns>
    public (Color Background, Color Foreground)? GetColorForMapID(uint mapID)
    {
        foreach (var entry in ColorDictionary)
        {
            if (entry.Key.Contains(mapID))
            {
                return entry.Value;
            }
        }

        return null; // No color mapping found
    }

    /// <summary>
    /// Check if a color table is currently loaded.
    /// </summary>
    public bool HasColorTable => !string.IsNullOrEmpty(CurrentColorTablePath);
}
