namespace Clockwork.Core.Formats.NDS.Scripts;

/// <summary>
/// Exception thrown when script compilation fails
/// </summary>
public class ScriptCompilationException : Exception
{
    public ScriptCompilationException(string message) : base(message)
    {
    }

    public ScriptCompilationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
