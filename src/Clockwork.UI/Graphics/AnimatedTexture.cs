using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Veldrid;

namespace Clockwork.UI.Graphics;

/// <summary>
/// Represents an animated texture (e.g., GIF) with multiple frames
/// </summary>
public class AnimatedTexture : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly List<Texture> _frames = new();
    private readonly List<int> _frameDurations = new(); // Duration in milliseconds
    private int _currentFrameIndex = 0;
    private double _elapsedTime = 0;

    public int FrameCount => _frames.Count;
    public Texture CurrentFrame => _frames[_currentFrameIndex];
    public bool IsAnimated => _frames.Count > 1;

    public AnimatedTexture(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    /// <summary>
    /// Add a frame to the animation
    /// </summary>
    public void AddFrame(Texture texture, int durationMs)
    {
        _frames.Add(texture);
        _frameDurations.Add(durationMs);
    }

    /// <summary>
    /// Update animation state based on elapsed time
    /// </summary>
    public bool Update(double deltaTimeSeconds)
    {
        if (!IsAnimated) return false;

        _elapsedTime += deltaTimeSeconds * 1000; // Convert to milliseconds

        int currentDuration = _frameDurations[_currentFrameIndex];

        if (_elapsedTime >= currentDuration)
        {
            _elapsedTime -= currentDuration;
            _currentFrameIndex = (_currentFrameIndex + 1) % _frames.Count;
            return true; // Frame changed
        }

        return false; // No frame change
    }

    public void Dispose()
    {
        foreach (var frame in _frames)
        {
            frame?.Dispose();
        }
        _frames.Clear();
        _frameDurations.Clear();
    }
}
