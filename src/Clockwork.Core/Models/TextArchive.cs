using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Clockwork.Core.Formats.NDS;
using Clockwork.Core.Logging;

namespace Clockwork.Core.Models;

/// <summary>
/// Represents a Pokemon DS text archive with encrypted messages.
/// Based on LiTRE's TextArchive implementation.
/// </summary>
public class TextArchive
{
    public int ID { get; }
    public List<string> Messages { get; set; } = new();
    public ushort Key { get; set; }

    /// <summary>
    /// Creates a new TextArchive from messages
    /// </summary>
    public TextArchive(int id, List<string>? messages = null)
    {
        ID = id;
        if (messages != null)
        {
            Messages = messages;
        }
    }

    /// <summary>
    /// Read a text archive from binary data
    /// </summary>
    public static TextArchive ReadFromBytes(byte[] data, int archiveID = 0)
    {
        using var ms = new MemoryStream(data);
        var messages = TextConverter.ReadMessageFromStream(ms, out ushort key);

        return new TextArchive(archiveID, messages)
        {
            Key = key
        };
    }

    /// <summary>
    /// Read a text archive from a file path
    /// </summary>
    public static TextArchive ReadFromFile(string filePath, int archiveID = 0)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Text archive file not found: {filePath}");
        }

        byte[] data = File.ReadAllBytes(filePath);
        return ReadFromBytes(data, archiveID);
    }

    /// <summary>
    /// Convert the text archive to binary data
    /// </summary>
    public byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        if (!TextConverter.WriteMessagesToStream(ref ms, Messages, Key))
        {
            AppLogger.Error($"Failed to convert Text Archive ID {ID} to byte array.");
        }
        return ms.ToArray();
    }

    /// <summary>
    /// Save the text archive to a file
    /// </summary>
    public void SaveToFile(string filePath)
    {
        try
        {
            byte[] data = ToBytes();
            File.WriteAllBytes(filePath, data);
            AppLogger.Info($"Saved text archive {ID} to {filePath}");
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to save text archive {ID}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Export to plain text format (for editing)
    /// </summary>
    public void ExportToTextFile(string filePath)
    {
        try
        {
            var utf8WithoutBom = new UTF8Encoding(false);
            string firstLine = $"# Key: 0x{Key:X4}";
            string textToSave = string.Join(Environment.NewLine, Messages);
            textToSave = firstLine + Environment.NewLine + textToSave;

            File.WriteAllText(filePath, textToSave, utf8WithoutBom);
            AppLogger.Info($"Exported text archive {ID} to {filePath}");
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to export text archive {ID}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Import from plain text format
    /// </summary>
    public static TextArchive ImportFromTextFile(string filePath, int archiveID = 0)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Text file not found: {filePath}");
        }

        try
        {
            List<string> lines = File.ReadAllLines(filePath).ToList();
            if (lines.Count == 0)
            {
                throw new InvalidDataException("Text file is empty");
            }

            // First line should be the key
            string firstLine = lines[0];
            if (!firstLine.StartsWith("# Key: "))
            {
                throw new InvalidDataException("Text file is missing the key in the first line (format: # Key: 0xXXXX)");
            }

            string keyHex = firstLine.Substring(7).Trim();
            if (!ushort.TryParse(keyHex.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out ushort key))
            {
                throw new InvalidDataException("Text file has an invalid key format");
            }

            // Check for newline character in last line and add a blank line if needed
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                if (fs.Length > 0)
                {
                    fs.Seek(-1, SeekOrigin.End);
                    int lastByte = fs.ReadByte();
                    if (lastByte == '\n' || lastByte == '\r')
                    {
                        lines.Add(string.Empty);
                    }
                }
            }

            // Remove the first line (the key) from the messages
            lines.RemoveAt(0);

            var archive = new TextArchive(archiveID, lines)
            {
                Key = key
            };

            AppLogger.Info($"Imported text archive {archiveID} from {filePath}");
            return archive;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to import text archive from {filePath}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Get simple trainer names (without {TRAINER_NAME:} wrapper)
    /// </summary>
    public List<string> GetSimpleTrainerNames()
    {
        List<string> simpleMessages = new List<string>();
        foreach (string msg in Messages)
        {
            string simpleMsg = TextConverter.GetSimpleTrainerName(msg);
            simpleMessages.Add(simpleMsg);
        }
        return simpleMessages;
    }

    /// <summary>
    /// Set a simple trainer name at a specific index
    /// </summary>
    public bool SetSimpleTrainerName(int messageIndex, string newSimpleName)
    {
        if (messageIndex < 0)
        {
            AppLogger.Error($"Invalid message index {messageIndex} for Text Archive ID {ID}");
            return false;
        }

        if (messageIndex >= Messages.Count)
        {
            Messages.Add("{TRAINER_NAME:" + newSimpleName + "}");
            return true;
        }

        string currentMessage = Messages[messageIndex];
        string updatedMessage = TextConverter.GetProperTrainerName(currentMessage, newSimpleName);
        if (updatedMessage == currentMessage)
        {
            // No change made
            return false;
        }

        Messages[messageIndex] = updatedMessage;
        return true;
    }

    /// <summary>
    /// Get a message by index
    /// </summary>
    public string GetMessage(int index)
    {
        if (index >= 0 && index < Messages.Count)
            return Messages[index];

        return string.Empty;
    }

    /// <summary>
    /// Set a message by index
    /// </summary>
    public void SetMessage(int index, string message)
    {
        if (index >= 0 && index < Messages.Count)
        {
            Messages[index] = message;
        }
        else if (index == Messages.Count)
        {
            Messages.Add(message);
        }
        else
        {
            AppLogger.Warn($"Invalid message index {index} for Text Archive ID {ID}");
        }
    }

    /// <summary>
    /// Get the number of messages
    /// </summary>
    public int MessageCount => Messages.Count;

    /// <summary>
    /// Convert to string (all messages)
    /// </summary>
    public override string ToString()
    {
        return string.Join(Environment.NewLine, Messages);
    }
}
