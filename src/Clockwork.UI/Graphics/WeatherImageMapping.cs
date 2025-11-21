using System.Collections.Generic;

namespace Clockwork.UI.Graphics;

/// <summary>
/// Mapping between weather IDs and image filenames
/// </summary>
public static class WeatherImageMapping
{
    /// <summary>
    /// Maps weather ID to image filename (without extension)
    /// </summary>
    public static readonly Dictionary<byte, string> WeatherImages = new()
    {
        { 0, "ptnormal" },                    // Normal
        { 1, "ptcloudy" },                    // Normal, somewhat dark
        { 2, "ptrain" },                      // Rain
        { 3, "ptheavyrain" },                 // Heavy rain
        { 4, "ptthunderstorm" },              // Thunderstorm
        { 5, "ptsnowslow" },                  // Snowfall, slow
        { 6, "ptdiamondsnow" },               // Diamond dust
        { 7, "ptblizzard" },                  // Blizzard
        { 8, "ptnormal" },                    // Normal [08]
        { 9, "ptsandfall" },                  // Volcanic ash fall, slow
        { 10, "ptsandstorm" },                // Sand storm
        { 11, "pthail" },                     // Hail
        { 12, "ptrocksascending" },           // Rocks ascending (?)
        { 13, "ptnormal" },                   // Normal [13]
        { 14, "ptfog" },                      // Fog
        { 15, "ptheavyfog" },                 // Deep fog
        { 16, "ptdark" },                     // Dark, Flash usable
        { 17, "ptlightning" },                // Lightning, no rain
        { 18, "ptlightfog" },                 // Light fog
        { 19, "ptheavyfog" },                 // Heavy fog
        { 20, "ptnormal" },                   // Normal [20]
        { 21, "ptdiamondsnow" },              // Diamond dust [21]
        { 22, "ptlightsandstorm" },           // Volcanic ash fall, steady
        { 23, "ptforestweather" },            // Eterna forest weather
        { 24, "ptspotlight" },                // Player spotlight [24]
        { 25, "ptspotlight" },                // Player spotlight [25]
        { 26, "ptdarkfog" },                  // Dark fog
        { 27, "ptgreenish" },                 // Somewhat green
        { 28, "ptredish" },                   // Somewhat red
        { 29, "ptblueish" },                  // Somewhat blue
        { 30, "ptdim" },                      // Dim light
        { 31, "ptnormal" },                   // Normal [31]
        { 32, "ptrain" },                     // Rain [32]
        { 33, "ptnormal" },                   // Normal [33]
        { 34, "ptdiamondsnow" },              // Diamond dust [34]
        { 35, "ptdiamondsnow" },              // Diamond dust [35]
        { 36, "ptsnowslow" }                  // Snowfall, slow [36]
    };

    /// <summary>
    /// Gets the image filename for a weather ID (without extension, checks both .png and .gif)
    /// </summary>
    public static string? GetWeatherImageFile(byte weatherId)
    {
        return WeatherImages.TryGetValue(weatherId, out var filename) ? filename : null;
    }

    /// <summary>
    /// Gets the camera image filename for a camera ID
    /// </summary>
    public static string GetCameraImageFile(byte cameraId)
    {
        return $"ptcamera{cameraId}";  // ptcamera0.png to ptcamera15.png
    }
}
