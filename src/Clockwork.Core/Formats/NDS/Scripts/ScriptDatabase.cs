namespace Clockwork.Core.Formats.NDS.Scripts;

/// <summary>
/// Database of script commands for different Pokémon NDS games
/// </summary>
public static class ScriptDatabase
{
    private static Dictionary<ushort, ScriptCommandInfo>? _platinumCommands;

    /// <summary>
    /// Gets script command database for Platinum (default)
    /// </summary>
    public static Dictionary<ushort, ScriptCommandInfo> PlatinumCommands
    {
        get
        {
            if (_platinumCommands == null)
            {
                InitializePlatinumCommands();
            }
            return _platinumCommands!;
        }
    }

    /// <summary>
    /// Gets command info by ID, returns null if not found
    /// </summary>
    public static ScriptCommandInfo? GetCommandInfo(ushort commandID)
    {
        if (PlatinumCommands.TryGetValue(commandID, out var info))
        {
            return info;
        }
        return null;
    }

    /// <summary>
    /// Gets command name by ID, returns hex ID if not found
    /// </summary>
    public static string GetCommandName(ushort commandID)
    {
        var info = GetCommandInfo(commandID);
        return info?.Name ?? $"CMD_0x{commandID:X4}";
    }

    private static void InitializePlatinumCommands()
    {
        _platinumCommands = new Dictionary<ushort, ScriptCommandInfo>();

        // Basic control flow commands (from LiTRE code analysis)
        AddCommand(0x0002, "End", "Ends the script execution");
        AddCommand(0x0016, "Jump", ScriptParameterType.Offset, "Jumps to another location");
        AddCommand(0x001A, "Call", ScriptParameterType.Offset, "Calls a function");
        AddCommand(0x001B, "Return", "Returns from a function");

        // Conditional jumps
        AddCommand(0x0017, "JumpIfEqual", ScriptParameterType.Byte, ScriptParameterType.Offset, "Jump if equal");
        AddCommand(0x0018, "JumpIfNotEqual", ScriptParameterType.Byte, ScriptParameterType.Offset, "Jump if not equal");
        AddCommand(0x0019, "JumpIfGreater", ScriptParameterType.Byte, ScriptParameterType.Offset, "Jump if greater");
        AddCommand(0x001C, "JumpIfLess", ScriptParameterType.Byte, ScriptParameterType.Offset, "Jump if less");
        AddCommand(0x001D, "JumpIfGreaterOrEqual", ScriptParameterType.Byte, ScriptParameterType.Offset, "Jump if greater or equal");

        // Text and messages
        AddCommand(0x0068, "Message", ScriptParameterType.Word, "Displays a message");
        AddCommand(0x0094, "CloseMessage", "Closes the message box");
        AddCommand(0x0095, "WaitMessage", "Waits for message to close");

        // Movement commands
        AddCommand(0x005E, "ApplyMovement", ScriptParameterType.Word, ScriptParameterType.Offset, "Applies movement to overworld");
        AddCommand(0x005F, "WaitMovement", "Waits for movement to finish");

        // Variable operations
        AddCommand(0x0021, "SetVar", ScriptParameterType.Word, ScriptParameterType.Word, "Sets a variable value");
        AddCommand(0x0022, "CopyVar", ScriptParameterType.Word, ScriptParameterType.Word, "Copies variable value");
        AddCommand(0x0023, "SetVarFromWork", ScriptParameterType.Word, ScriptParameterType.Word, "Sets variable from work");
        AddCommand(0x0024, "SetWorkFromVar", ScriptParameterType.Word, ScriptParameterType.Word, "Sets work from variable");

        // Common game functions
        AddCommand(0x0004, "Nop", "No operation");
        AddCommand(0x0003, "FacePlayer", "Makes NPC face player");
        AddCommand(0x0005, "Wait", ScriptParameterType.Word, "Waits for specified frames");
        AddCommand(0x001E, "Lock", ScriptParameterType.Word, "Locks entity");
        AddCommand(0x001F, "Release", ScriptParameterType.Word, "Releases entity");
        AddCommand(0x0020, "AddFlag", ScriptParameterType.Word, "Sets a flag");
        AddCommand(0x0025, "RemoveFlag", ScriptParameterType.Word, "Clears a flag");
        AddCommand(0x0026, "CheckFlag", ScriptParameterType.Word, "Checks if flag is set");

        // Player and world
        AddCommand(0x0034, "Warp", ScriptParameterType.Word, ScriptParameterType.Word, ScriptParameterType.Word, ScriptParameterType.Word, ScriptParameterType.Word, "Warps the player");
        AddCommand(0x00BC, "GiveItem", ScriptParameterType.Word, ScriptParameterType.Word, "Gives an item to the player");
        AddCommand(0x0193, "HealPokemon", "Heals all Pokémon");

        // Sound and music
        AddCommand(0x00F0, "PlayBGM", ScriptParameterType.Word, "Plays background music");
        AddCommand(0x00F1, "StopBGM", "Stops background music");
        AddCommand(0x00F2, "PlaySound", ScriptParameterType.Word, "Plays a sound effect");

        // Battle
        AddCommand(0x01CF, "WildBattle", ScriptParameterType.Word, ScriptParameterType.Byte, "Starts a wild battle");
        AddCommand(0x01D0, "TrainerBattle", ScriptParameterType.Word, ScriptParameterType.Word, "Starts a trainer battle");

        // Placeholder for unknown commands - will show as CMD_0xXXXX
    }

    private static void AddCommand(ushort id, string name, params ScriptParameterType[] parameters)
    {
        var info = new ScriptCommandInfo(id, name, parameters);
        _platinumCommands![id] = info;
    }

    private static void AddCommand(ushort id, string name, string description, params ScriptParameterType[] parameters)
    {
        var info = new ScriptCommandInfo(id, name, parameters)
        {
            Description = description
        };
        _platinumCommands![id] = info;
    }

    private static void AddCommand(ushort id, string name, ScriptParameterType param1, string description)
    {
        var info = new ScriptCommandInfo(id, name, param1)
        {
            Description = description
        };
        _platinumCommands![id] = info;
    }

    private static void AddCommand(ushort id, string name, ScriptParameterType param1, ScriptParameterType param2, string description)
    {
        var info = new ScriptCommandInfo(id, name, param1, param2)
        {
            Description = description
        };
        _platinumCommands![id] = info;
    }
}
