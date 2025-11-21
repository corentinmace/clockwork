using System.IO;

namespace Clockwork.Core.Models.LevelScript;

/// <summary>
/// Interface for level script triggers
/// </summary>
public interface ILevelScriptTrigger
{
    /// <summary>
    /// Trigger type identifier
    /// </summary>
    ushort TriggerType { get; }

    /// <summary>
    /// Write trigger to binary stream
    /// </summary>
    void WriteToStream(BinaryWriter writer);

    /// <summary>
    /// Get human-readable display string for this trigger
    /// </summary>
    string GetDisplayString();
}
