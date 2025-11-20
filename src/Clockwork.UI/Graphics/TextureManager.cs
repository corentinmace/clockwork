using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Clockwork.Core.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Veldrid;

namespace Clockwork.UI.Graphics;

/// <summary>
/// Manages loading and caching of textures for ImGui
/// </summary>
public class TextureManager : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ImGuiRenderer _imguiRenderer;
    private readonly Dictionary<string, IntPtr> _textureCache = new();
    private readonly Dictionary<string, Texture> _rawTextures = new();

    public TextureManager(GraphicsDevice graphicsDevice, ImGuiRenderer imguiRenderer)
    {
        _graphicsDevice = graphicsDevice;
        _imguiRenderer = imguiRenderer;
    }

    /// <summary>
    /// Loads a texture from embedded resources and returns an ImGui binding handle
    /// </summary>
    public IntPtr? LoadTexture(string resourcePath)
    {
        // Check cache first
        if (_textureCache.TryGetValue(resourcePath, out IntPtr cachedHandle))
        {
            return cachedHandle;
        }

        try
        {
            // Load image from embedded resources
            var assembly = Assembly.GetExecutingAssembly();
            var fullResourceName = $"Clockwork.UI.{resourcePath.Replace("/", ".").Replace("\\", ".")}";

            using var stream = assembly.GetManifestResourceStream(fullResourceName);
            if (stream == null)
            {
                AppLogger.Warn($"[TextureManager] Resource not found: {fullResourceName}");
                return null;
            }

            // Load image with ImageSharp
            using var image = Image.Load<Rgba32>(stream);

            // Create Veldrid texture
            var texture = CreateTextureFromImage(image, resourcePath);
            _rawTextures[resourcePath] = texture;

            // Get ImGui binding
            var handle = _imguiRenderer.GetOrCreateImGuiBinding(_graphicsDevice.ResourceFactory, texture);
            _textureCache[resourcePath] = handle;

            AppLogger.Debug($"[TextureManager] Loaded texture: {resourcePath}");
            return handle;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[TextureManager] Failed to load texture {resourcePath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Loads a weather image by ID
    /// </summary>
    public IntPtr? LoadWeatherImage(byte weatherId)
    {
        var filename = WeatherImageMapping.GetWeatherImageFile(weatherId);
        if (filename == null)
            return null;

        // Try PNG first, then GIF
        var pngPath = $"Assets/Graphics/Weather/{filename}.png";
        var gifPath = $"Assets/Graphics/Weather/{filename}.gif";

        var handle = LoadTexture(pngPath);
        if (handle == null)
        {
            handle = LoadTexture(gifPath);
        }

        return handle;
    }

    /// <summary>
    /// Loads a camera image by ID
    /// </summary>
    public IntPtr? LoadCameraImage(byte cameraId)
    {
        var filename = WeatherImageMapping.GetCameraImageFile(cameraId);
        var path = $"Assets/Graphics/Camera/{filename}.png";
        return LoadTexture(path);
    }

    private Texture CreateTextureFromImage(Image<Rgba32> image, string name)
    {
        var width = (uint)image.Width;
        var height = (uint)image.Height;

        // Extract pixel data
        var pixels = new Rgba32[width * height];
        image.CopyPixelDataTo(pixels);

        // Convert to byte array
        var pixelBytes = new byte[width * height * 4];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixelBytes[i * 4 + 0] = pixels[i].R;
            pixelBytes[i * 4 + 1] = pixels[i].G;
            pixelBytes[i * 4 + 2] = pixels[i].B;
            pixelBytes[i * 4 + 3] = pixels[i].A;
        }

        // Create texture
        var texture = _graphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(
            width, height,
            1, 1, 1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled,
            TextureType.Texture2D
        ));
        texture.Name = name;

        // Upload pixel data
        _graphicsDevice.UpdateTexture(texture, pixelBytes, 0, 0, 0, width, height, 1, 0, 0);

        return texture;
    }

    public void Dispose()
    {
        foreach (var texture in _rawTextures.Values)
        {
            texture?.Dispose();
        }
        _rawTextures.Clear();
        _textureCache.Clear();
    }
}
