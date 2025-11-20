using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Clockwork.Core.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Gif;
using Veldrid;

namespace Clockwork.UI.Graphics;

/// <summary>
/// Manages loading and caching of textures for ImGui, including animated GIFs
/// </summary>
public class TextureManager : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ImGuiRenderer _imguiRenderer;
    private readonly Dictionary<string, IntPtr> _textureCache = new();
    private readonly Dictionary<string, AnimatedTexture> _animatedTextures = new();

    public TextureManager(GraphicsDevice graphicsDevice, ImGuiRenderer imguiRenderer)
    {
        _graphicsDevice = graphicsDevice;
        _imguiRenderer = imguiRenderer;
    }

    /// <summary>
    /// Update all animated textures. Call this every frame.
    /// </summary>
    public void Update(double deltaTimeSeconds)
    {
        foreach (var kvp in _animatedTextures)
        {
            var animTexture = kvp.Value;
            if (animTexture.Update(deltaTimeSeconds))
            {
                // Frame changed, update ImGui binding
                var resourcePath = kvp.Key;
                var handle = _imguiRenderer.GetOrCreateImGuiBinding(
                    _graphicsDevice.ResourceFactory,
                    animTexture.CurrentFrame
                );
                _textureCache[resourcePath] = handle;
            }
        }
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

            // Check if it's a GIF
            bool isGif = resourcePath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase);

            if (isGif)
            {
                return LoadAnimatedGif(stream, resourcePath);
            }
            else
            {
                return LoadStaticImage(stream, resourcePath);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[TextureManager] Failed to load texture {resourcePath}: {ex.Message}");
            return null;
        }
    }

    private IntPtr? LoadStaticImage(Stream stream, string resourcePath)
    {
        // Load image with ImageSharp
        using var image = Image.Load<Rgba32>(stream);

        // Create Veldrid texture
        var texture = CreateTextureFromImage(image, resourcePath);

        // Get ImGui binding
        var handle = _imguiRenderer.GetOrCreateImGuiBinding(_graphicsDevice.ResourceFactory, texture);
        _textureCache[resourcePath] = handle;

        AppLogger.Debug($"[TextureManager] Loaded static image: {resourcePath}");
        return handle;
    }

    private IntPtr? LoadAnimatedGif(Stream stream, string resourcePath)
    {
        // Load GIF with all frames
        using var gif = Image.Load<Rgba32>(stream);

        var animTexture = new AnimatedTexture(_graphicsDevice);

        // Extract all frames
        for (int i = 0; i < gif.Frames.Count; i++)
        {
            var frame = gif.Frames[i];

            // Get frame duration (default to 100ms if not specified)
            int duration = 100;
            if (frame.Metadata.TryGetGifMetadata(out var gifMetadata))
            {
                duration = gifMetadata.FrameDelay * 10; // FrameDelay is in 1/100th of a second
                if (duration == 0) duration = 100; // Default if not specified
            }

            // Convert frame to Veldrid texture
            var texture = CreateTextureFromFrame(frame, $"{resourcePath}_frame{i}");
            animTexture.AddFrame(texture, duration);
        }

        // Store animated texture
        _animatedTextures[resourcePath] = animTexture;

        // Get initial frame binding
        var handle = _imguiRenderer.GetOrCreateImGuiBinding(
            _graphicsDevice.ResourceFactory,
            animTexture.CurrentFrame
        );
        _textureCache[resourcePath] = handle;

        AppLogger.Debug($"[TextureManager] Loaded animated GIF: {resourcePath} ({animTexture.FrameCount} frames)");
        return handle;
    }

    /// <summary>
    /// Loads a weather image by ID
    /// </summary>
    public IntPtr? LoadWeatherImage(byte weatherId)
    {
        var filename = WeatherImageMapping.GetWeatherImageFile(weatherId);
        if (filename == null)
            return null;

        // Try GIF first (since weather images are animated), then PNG
        var gifPath = $"Assets/Graphics/Weather/{filename}.gif";
        var pngPath = $"Assets/Graphics/Weather/{filename}.png";

        var handle = LoadTexture(gifPath);
        if (handle == null)
        {
            handle = LoadTexture(pngPath);
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

    private Texture CreateTextureFromFrame(ImageFrame<Rgba32> frame, string name)
    {
        var width = (uint)frame.Width;
        var height = (uint)frame.Height;

        // Extract pixel data from frame
        var pixels = new Rgba32[width * height];
        frame.CopyPixelDataTo(pixels);

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
        foreach (var animTexture in _animatedTextures.Values)
        {
            animTexture?.Dispose();
        }
        _animatedTextures.Clear();
        _textureCache.Clear();
    }
}
