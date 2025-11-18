namespace Clockwork.Core.Services;

/// <summary>
/// Exemple de service pour démontrer l'architecture.
/// Peut être remplacé par vos propres services métier.
/// </summary>
public class ExampleService : IApplicationService
{
    private int _frameCount;
    private double _totalTime;

    /// <summary>
    /// Nombre de frames depuis le démarrage.
    /// </summary>
    public int FrameCount => _frameCount;

    /// <summary>
    /// Temps total écoulé en secondes.
    /// </summary>
    public double TotalTime => _totalTime;

    /// <summary>
    /// FPS moyen.
    /// </summary>
    public double AverageFps => _frameCount / Math.Max(_totalTime, 0.001);

    public void Initialize()
    {
        _frameCount = 0;
        _totalTime = 0;
        Console.WriteLine("[ExampleService] Initialized");
    }

    public void Update(double deltaTime)
    {
        _frameCount++;
        _totalTime += deltaTime;
    }

    public void Dispose()
    {
        Console.WriteLine($"[ExampleService] Disposing - Total frames: {_frameCount}, Total time: {_totalTime:F2}s");
    }
}
