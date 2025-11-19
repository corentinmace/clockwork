# NDS Script File Serialization Implementation Guide

## Quick Reference: Binary Format at Byte Level

### File Layout
```
┌─────────────────────────────────────────────┐
│ FILE HEADER (16 bytes)                      │
│ +0x00: uint32 magic (varies by game)        │
│ +0x04: uint16 version/flags                 │
│ +0x06: uint16 reserved                      │
│ +0x08: uint16 script count (N)              │
│ +0x0A: uint16 function count (M)            │
│ +0x0C: uint16 action count (K)              │
│ +0x0E: uint16 reserved                      │
├─────────────────────────────────────────────┤
│ SCRIPT OFFSET TABLE (N * 4 bytes)           │
│ +0x00: uint32 offset to script 0            │
│ +0x04: uint32 offset to script 1            │
│ ... (repeat N times)                        │
│ TERMINATOR: 0xFD13 (2 bytes)                │
├─────────────────────────────────────────────┤
│ FUNCTION OFFSET TABLE (M * 4 bytes)         │
│ (Same structure as scripts)                 │
│ TERMINATOR: 0xFD13 (2 bytes)                │
├─────────────────────────────────────────────┤
│ ACTION OFFSET TABLE (K * 4 bytes)           │
│ (Same structure as scripts)                 │
│ TERMINATOR: 0xFD13 (2 bytes)                │
├─────────────────────────────────────────────┤
│ SCRIPT DATA                                 │
│ (All script bytecode, contiguous)           │
├─────────────────────────────────────────────┤
│ FUNCTION DATA                               │
│ (All function bytecode, contiguous)         │
├─────────────────────────────────────────────┤
│ ACTION DATA                                 │
│ (All action bytecode, contiguous)           │
└─────────────────────────────────────────────┘
```

## Implementation Pseudocode

### FromBinary() Implementation

```csharp
public static ScriptFile FromBinary(byte[] data, int fileID = 0)
{
    var file = new ScriptFile(fileID);
    using var ms = new MemoryStream(data);
    using var reader = new BinaryReader(ms);

    // Read header
    uint magic = reader.ReadUInt32();           // 0x00
    ushort version = reader.ReadUInt16();       // 0x04
    ushort reserved1 = reader.ReadUInt16();     // 0x06
    ushort scriptCount = reader.ReadUInt16();   // 0x08
    ushort functionCount = reader.ReadUInt16(); // 0x0A
    ushort actionCount = reader.ReadUInt16();   // 0x0C
    ushort reserved2 = reader.ReadUInt16();     // 0x0E
    
    // Determine base offset for command data
    // = header (16) + script offsets (scriptCount*4 + 2) 
    //   + function offsets (functionCount*4 + 2) 
    //   + action offsets (actionCount*4 + 2)
    long dataBaseOffset = 16 + (scriptCount * 4 + 2) + 
                              (functionCount * 4 + 2) + 
                              (actionCount * 4 + 2);
    
    // Read script offset table
    uint[] scriptOffsets = ReadOffsetTable(reader, scriptCount, dataBaseOffset);
    
    // Read function offset table
    uint[] functionOffsets = ReadOffsetTable(reader, functionCount, dataBaseOffset);
    
    // Read action offset table
    uint[] actionOffsets = ReadOffsetTable(reader, actionCount, dataBaseOffset);
    
    // Parse scripts
    for (int i = 0; i < scriptOffsets.Length; i++)
    {
        long endOffset = (i + 1 < scriptOffsets.Length) 
            ? scriptOffsets[i + 1] 
            : (functionOffsets.Length > 0 ? functionOffsets[0] : dataBaseOffset);
        
        var container = ParseContainer(data, scriptOffsets[i], endOffset, 
                                      (uint)i, ContainerType.Script);
        file.Scripts.Add(container);
    }
    
    // Parse functions
    for (int i = 0; i < functionOffsets.Length; i++)
    {
        long endOffset = (i + 1 < functionOffsets.Length) 
            ? functionOffsets[i + 1] 
            : (actionOffsets.Length > 0 ? actionOffsets[0] : data.Length);
        
        var container = ParseContainer(data, functionOffsets[i], endOffset, 
                                      (uint)i, ContainerType.Function);
        file.Functions.Add(container);
    }
    
    // Parse actions
    for (int i = 0; i < actionOffsets.Length; i++)
    {
        long endOffset = (i + 1 < actionOffsets.Length) 
            ? actionOffsets[i + 1] 
            : data.Length;
        
        var container = ParseContainer(data, actionOffsets[i], endOffset, 
                                      (uint)i, ContainerType.Action);
        file.Actions.Add(container);
    }
    
    return file;
}

private static uint[] ReadOffsetTable(BinaryReader reader, ushort count, long dataBase)
{
    var offsets = new uint[count];
    for (int i = 0; i < count; i++)
    {
        offsets[i] = reader.ReadUInt32();
    }
    // Read terminator
    ushort terminator = reader.ReadUInt16();
    if (terminator != 0xFD13)
    {
        // Log warning: unexpected terminator
    }
    return offsets;
}

private static ScriptContainer ParseContainer(byte[] data, long startOffset, long endOffset, 
                                              uint id, ContainerType type)
{
    var container = new ScriptContainer(id, type) { Offset = (uint)startOffset };
    
    int currentPos = (int)startOffset;
    while (currentPos < endOffset)
    {
        ushort commandID = BitConverter.ToUInt16(data, currentPos);
        currentPos += 2;
        
        var commandInfo = ScriptDatabase.GetCommandInfo(commandID);
        var command = new ScriptCommand { CommandID = commandID, Offset = (uint)(currentPos - 2) };
        
        // Parse parameters based on command definition
        if (commandInfo != null)
        {
            foreach (var paramType in commandInfo.Parameters)
            {
                byte[] paramBytes = ReadParameter(data, ref currentPos, paramType);
                command.Parameters.Add(paramBytes);
            }
        }
        // If unknown command, try to read until next command ID (risky, better to have database)
        
        container.Commands.Add(command);
    }
    
    return container;
}

private static byte[] ReadParameter(byte[] data, ref int position, ScriptParameterType type)
{
    byte[] result = type switch
    {
        ScriptParameterType.Byte => new[] { data[position++] },
        ScriptParameterType.Word => 
        {
            byte[] val = data[position..(position + 2)];
            position += 2;
            yield return val;
        },
        ScriptParameterType.DWord => 
        {
            byte[] val = data[position..(position + 4)];
            position += 4;
            yield return val;
        },
        ScriptParameterType.Offset => 
        {
            byte[] val = data[position..(position + 4)];
            position += 4;
            yield return val;
        },
        ScriptParameterType.Variable => 
        {
            // For variable parameters, read until next command
            // This requires lookahead or knowledge of parameter size
            // This is the tricky case - may need per-command special handling
            throw new NotImplementedException("Variable parameters need special handling");
        },
        _ => throw new ArgumentException($"Unknown parameter type: {type}")
    };
    return result;
}
```

### ToBytes() Implementation

```csharp
public byte[] ToBytes()
{
    using var ms = new MemoryStream();
    using var writer = new BinaryWriter(ms);
    
    // Write header
    writer.Write((uint)0);                                    // Magic (set appropriately)
    writer.Write((ushort)0);                                  // Version
    writer.Write((ushort)0);                                  // Reserved
    writer.Write((ushort)Scripts.Count);                      // Script count
    writer.Write((ushort)Functions.Count);                    // Function count
    writer.Write((ushort)Actions.Count);                      // Action count
    writer.Write((ushort)0);                                  // Reserved
    
    // Build command data first (to know offsets)
    byte[] scriptData = SerializeContainers(Scripts);
    byte[] functionData = SerializeContainers(Functions);
    byte[] actionData = SerializeContainers(Actions);
    
    // Calculate offset table base
    long offsetTableSize = (Scripts.Count * 4 + 2) +
                          (Functions.Count * 4 + 2) +
                          (Actions.Count * 4 + 2);
    long dataBaseOffset = 16 + offsetTableSize;
    
    // Write script offsets
    WriteOffsetTable(writer, Scripts, scriptData, dataBaseOffset);
    
    // Adjust base for next section
    dataBaseOffset += scriptData.Length;
    
    // Write function offsets
    WriteOffsetTable(writer, Functions, functionData, dataBaseOffset);
    
    // Adjust base for next section
    dataBaseOffset += functionData.Length;
    
    // Write action offsets
    WriteOffsetTable(writer, Actions, actionData, dataBaseOffset);
    
    // Write command data
    writer.Write(scriptData);
    writer.Write(functionData);
    writer.Write(actionData);
    
    return ms.ToArray();
}

private byte[] SerializeContainers(List<ScriptContainer> containers)
{
    using var ms = new MemoryStream();
    using var writer = new BinaryWriter(ms);
    
    foreach (var container in containers)
    {
        foreach (var command in container.Commands)
        {
            writer.Write(command.CommandID);
            foreach (var param in command.Parameters)
            {
                writer.Write(param);
            }
        }
    }
    
    return ms.ToArray();
}

private void WriteOffsetTable(BinaryWriter writer, List<ScriptContainer> containers, 
                             byte[] data, long baseOffset)
{
    long currentOffset = baseOffset;
    
    // Calculate individual container offsets
    foreach (var container in containers)
    {
        writer.Write((uint)currentOffset);
        int containerSize = CalculateContainerSize(container);
        currentOffset += containerSize;
    }
    
    // Write terminator
    writer.Write((ushort)0xFD13);
}

private int CalculateContainerSize(ScriptContainer container)
{
    int size = 0;
    foreach (var command in container.Commands)
    {
        size += 2; // Command ID
        foreach (var param in command.Parameters)
        {
            size += param.Length;
        }
    }
    return size;
}
```

## Critical Implementation Notes

### 1. Offset Calculation
- All offsets in offset table are relative to the start of command data (after all headers/tables)
- This is NOT the absolute file offset
- When reading, you must add the data base offset to get actual file position

### 2. Terminator Handling
- Each offset table ends with 0xFD13 (little endian: 0x13, 0xFD in file)
- This is **required** for proper parsing
- Don't skip it!

### 3. Parameter Size Calculation
- You MUST have the command database (ScriptCommands.json) loaded
- Unknown commands cannot be parsed safely
- Consider throwing exception if command not found, or have fallback to "End" command

### 4. Little Endian Handling
```csharp
// Correct for little endian
writer.Write(value);  // BinaryWriter handles this automatically

// When reading raw bytes, be careful
ushort word = BitConverter.ToUInt16(data, offset);  // Correct
uint dword = BitConverter.ToUInt32(data, offset);   // Correct
```

### 5. Game Version Differences
- The current implementation only loads Platinum commands from JSON
- For Diamond/Pearl or HGSS support, extend ScriptDatabase to load version-specific commands
- Different games may have different command sets or parameters

## Testing Checklist

```
[ ] Round-trip test: Read file → Write to memory → Compare bytes
[ ] Offset validation: All offsets point to valid command positions
[ ] Terminator validation: All offset tables end with 0xFD13
[ ] Command parsing: All commands in database parse successfully
[ ] Parameter validation: Parameters match expected types/sizes
[ ] Empty containers: Handle scripts/functions/actions lists with 0 items
[ ] Edge cases: Single command, single container, mixed container types
[ ] File size: Serialized size matches calculated size
```

## Example File Trace

Given a file with:
- 1 script (2 commands)
- 1 function (1 command)  
- 0 actions

```
Offset  Hex Data                      Description
------  ---                           -----------
0x0000  00 00 00 00                   Magic
0x0004  00 00                         Version
0x0006  00 00                         Reserved
0x0008  01 00                         Script count (1)
0x000A  01 00                         Function count (1)
0x000C  00 00                         Action count (0)
0x000E  00 00                         Reserved

0x0010  00 00 00 00                   Script offset table: script 0 @ offset 0
0x0014  13 FD                         Script terminator

0x0016  0E 00 00 00                   Function offset table: function 0 @ offset 0x0E
0x001A  13 FD                         Function terminator

0x001C  13 FD                         Action terminator (empty)

DATA BASE OFFSET = 0x001E (30 bytes)

0x001E  02 00                         Script data starts: End command (0x0002)
0x0020  1E 00 01 00                   Lock command (0x001E) with param 0x0001
0x0024  27 00                         Function data starts: Return command (0x0027)
```

## Current Status in Codebase

Files that need implementation:
- `/home/user/clockwork/src/Clockwork.Core/Formats/NDS/Scripts/ScriptFile.cs`
  - `FromBinary()` method (line 51-63)
  - `ToBytes()` method (line 82-92)

Related files (already implemented):
- `ScriptContainer.cs` - Container serialization (ToBytes) is partial
- `ScriptCommand.cs` - Command serialization (ToBytes) is implemented
- `ScriptDatabase.cs` - Command definitions loaded from JSON
- `ScriptCompiler.cs` - Text-to-binary compilation
- `ScriptDecompiler.cs` - Binary-to-text decompilation

