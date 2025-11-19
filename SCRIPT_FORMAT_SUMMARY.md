# NDS Script File Binary Format - Executive Summary

## Complete File Structure

An NDS script file has exactly this layout:

```
Header (16 bytes) → Offset Tables → Command Data
```

### Header: 16 bytes, all little-endian
```
Bytes 0-3:   Magic/FileType (uint32)
Bytes 4-5:   Version/Flags (uint16)
Bytes 6-7:   Reserved (uint16)
Bytes 8-9:   Script Count (uint16) - number of scripts
Bytes 10-11: Function Count (uint16) - number of functions  
Bytes 12-13: Action Count (uint16) - number of actions
Bytes 14-15: Reserved (uint16)
```

### Offset Tables
For EACH container type (Scripts, then Functions, then Actions):
```
[4 bytes × container_count] = offsets to each container
[2 bytes] = terminator 0xFD13
```

All offsets are relative to start of command data (NOT absolute file offsets)

### Command Data
Contiguous binary stream of all commands:
```
For each container:
  For each command:
    [2 bytes] Command ID (uint16)
    [variable] Parameters (size determined by ScriptDatabase)
```

## Key Format Facts

1. **Little Endian**: All multi-byte values use little endian
2. **No Padding**: Data is packed with no gaps (except optional 4-byte alignment)
3. **Terminator 0xFD13**: Required after each offset table
4. **Offset Relativity**: Offsets in tables are relative to command data start
5. **Command Database**: ScriptCommands.json defines parameter types for each command ID

## Critical Data Structures in Code

```csharp
// Already exists in codebase:
public enum ScriptParameterType
{
    Byte,       // 1 byte
    Word,       // 2 bytes (uint16)
    DWord,      // 4 bytes (uint32)
    Offset,     // 4 bytes (pointer/offset)
    Variable    // Variable length (rare, needs special handling)
}

public class ScriptCommand
{
    public ushort CommandID;              // 2 bytes in file
    public List<byte[]> Parameters;       // Raw parameter data
    public byte[] ToBytes() { /* impl */ } // Already works!
}

public class ScriptContainer
{
    public uint ID;                       // Container ID
    public ContainerType Type;            // Script/Function/Action
    public List<ScriptCommand> Commands;  
    public uint Offset;                   // Original file offset
}

public class ScriptFile
{
    public int FileID;
    public List<ScriptContainer> Scripts;
    public List<ScriptContainer> Functions;
    public List<ScriptContainer> Actions;
    
    public static ScriptFile FromBinary(byte[] data); // NEEDS IMPLEMENTATION
    public byte[] ToBytes();                          // NEEDS IMPLEMENTATION
}
```

## Implementation Checklist

### FromBinary() Steps:
1. Read 16-byte header, extract counts
2. Calculate dataBaseOffset = 16 + (scriptCount×4+2) + (functionCount×4+2) + (actionCount×4+2)
3. Read script offset table (scriptCount uint32s) + skip terminator
4. Read function offset table (functionCount uint32s) + skip terminator  
5. Read action offset table (actionCount uint32s) + skip terminator
6. For each offset, parse commands until reaching next container's offset (or end)
7. For each command:
   - Read 2-byte CommandID
   - Look up command in ScriptDatabase
   - For each parameter, read appropriate number of bytes based on type
   - Create ScriptCommand and add to container

### ToBytes() Steps:
1. Serialize all commands from Scripts, Functions, Actions to byte arrays
2. Calculate offsets based on serialized data sizes
3. Write 16-byte header with container counts
4. For each container type:
   - Write offset table (4 bytes per container)
   - Write terminator 0xFD13
5. Write all command data in order (Scripts, Functions, Actions)

## Files to Modify

**Primary File**: `/home/user/clockwork/src/Clockwork.Core/Formats/NDS/Scripts/ScriptFile.cs`
- Line 51-63: `FromBinary()` method
- Line 82-92: `ToBytes()` method

**Reference Files** (already partially implemented):
- `ScriptCommand.cs` - command binary serialization
- `ScriptContainer.cs` - container binary serialization  
- `ScriptDatabase.cs` - command definition lookup
- `ScriptCommands.json` - command database

## Byte-Level Example

A minimal script file with 1 script containing 1 "End" command (0x0002):

```
Offset  Bytes                              
------  -----                              
0x00    00 00 00 00                        (magic)
0x04    00 00                              (version)
0x06    00 00                              (reserved)
0x08    01 00                              (script count = 1)
0x0A    00 00                              (function count = 0)
0x0C    00 00                              (action count = 0)
0x0E    00 00                              (reserved)
0x10    00 00 00 00                        (script 0 offset = 0)
0x14    13 FD                              (script terminator)
0x16    13 FD                              (function terminator - empty)
0x18    13 FD                              (action terminator - empty)
0x1A    02 00                              (command data: End command)
```
File size: 0x1C (28 bytes)

## Most Important Points

1. **Offsets are relative to command data start**, not file start
   - This is the most common mistake
   
2. **Terminator 0xFD13 is mandatory** between offset sections
   - You must write and read it
   
3. **Command database lookup is required**
   - Can't parse file without knowing each command's parameters
   - ScriptDatabase already handles this
   
4. **Little endian everything**
   - BinaryWriter/BitConverter handle this automatically
   
5. **Contiguous data**
   - No padding between command data sections
   - Data immediately follows all headers and offset tables

## Testing Strategy

1. Create a minimal script file with 1 script, 1 function, 0 actions
2. Serialize it with ToBytes()
3. Deserialize with FromBinary()
4. Compare original to round-tripped version (should be identical)
5. Verify file structure with hex editor
6. Test with actual ROM script files after basic tests pass

## Documentation Files Created

- **NDS_SCRIPT_FORMAT.md** - Complete binary format specification
- **SCRIPT_SERIALIZATION_GUIDE.md** - Implementation guide with pseudocode
- **SCRIPT_FORMAT_SUMMARY.md** - This file (quick reference)

All files are in `/home/user/clockwork/` directory.

