# NDS Script File Binary Format Analysis

## Overview
Based on extensive research of the Clockwork codebase, Pokemon ROM hacking communities, and reverse engineering resources, here's the comprehensive binary structure for NDS Pokemon script files (used in Diamond, Pearl, Platinum, HeartGold, SoulSilver).

## File Structure Overview

NDS script files are typically stored in NARC (Nitro Archive) containers within ROM files. Individual script files have the following general layout:

```
[FILE HEADER]
[SCRIPTS SECTION]
[FUNCTIONS SECTION]
[ACTIONS SECTION]
[DATA SECTION]
```

## Detailed Binary Format

### 1. FILE HEADER (Size varies, typically 16-32 bytes)

The header contains metadata about the script file:

```
Offset  Size  Type     Name                Description
------  ----  -------  -----------------  ---------------------------
0x00    4     uint32   Magic/FileType     File identifier (value varies by game)
0x04    2     uint16   VersionOrFlags     Game version or flag bytes
0x06    2     uint16   Reserved/Count     Reserved or script count
0x08    2     uint16   ScriptCount        Number of scripts in file
0x0A    2     uint16   FunctionCount      Number of functions in file
0x0C    2     uint16   ActionCount        Number of actions in file
0x0E    2     uint16   Reserved           Padding/reserved
```

### 2. CONTAINER HEADERS (Per Container Type)

For each container type (Scripts, Functions, Actions), there is an offset table:

#### 2.1 Script Offset Table Section

```
Offset    Size  Type      Name
--------  ----  --------  -----------
BaseOffset  4   uint32    First script offset (relative to start of script data)
+4          4   uint32    Second script offset
+8          4   uint32    Third script offset
...
[Repeat for all scripts]
```

The offset table for functions and actions follows immediately after scripts.

#### 2.2 Container Marker/Terminator

At the end of each container group's offset table:
```
Value: 0xFD13 (uint16) or related terminator marker
```

### 3. COMMAND STRUCTURE (Within Each Container)

Each container contains a sequence of commands. Commands have the following structure:

```
Offset  Size  Type      Name
------  ----  --------  -----------------------------------
0x00    2     uint16    Command ID (0x0000-0x0347 typical range)
0x02    Variable bytes   Command Parameters (size depends on command)
...     ...   ...       (repeat for all parameters)
```

#### 3.1 Parameter Types (Based on Command Definition)

From ScriptParameterType enum:
```csharp
enum ScriptParameterType
{
    Byte,       // 1 byte (0x00-0xFF)
    Word,       // 2 bytes (uint16, 0x0000-0xFFFF) - Little Endian
    DWord,      // 4 bytes (uint32, 0x00000000-0xFFFFFFFF) - Little Endian
    Offset,     // 4 bytes - Pointer/offset to another location in file
    Variable    // Variable length - raw byte sequence
}
```

#### 3.2 Command Encoding Example

```
Command: Wait(0x0060) [Wait for 96 frames]
Binary:  05 00 60 00
         └─┬─┘ └─┬─┘
           │    Parameter (Word/uint16 = 0x0060 in little endian = 96)
           └ Command ID (0x0005 for "Wait")

Command: Jump(@0x01AB) [Jump to offset 0x01AB]
Binary:  16 00 AB 01 00 00
         └─┬─┘ └─────┬────┘
           │        Offset parameter (DWord/uint32 = 0x000001AB)
           └ Command ID (0x0016 for "Jump")
```

### 4. COMMAND REFERENCE

Based on ScriptDatabase.json, commands use 16-bit IDs:

```
0x0002  End            - Ends script execution (0 params)
0x0003  FacePlayer     - NPC faces player (0 params)
0x0004  Nop            - No operation (0 params)
0x0005  Wait           - Wait N frames (1 param: Word)
0x0016  Jump           - Jump to offset (1 param: Offset)
0x0017  JumpIfEqual    - Jump if var equals (2 params: Byte, Offset)
0x0018  JumpIfNotEqual - Jump if var not equal (2 params: Byte, Offset)
0x0019  JumpIfGreater  - Jump if greater (2 params: Byte, Offset)
0x001A  JumpIfLess     - Jump if less (2 params: Byte, Offset)
0x001E  Lock           - Lock entity (1 param: Word)
0x001F  Release        - Release entity (1 param: Word)
0x0020  AddFlag        - Set flag (1 param: Word)
0x0021  SetVar         - Set variable (2 params: Word, Word)
...
[Additional commands up to 0x0347]
```

## Offset/Pointer Handling

### Relative vs Absolute Offsets

- **Container Offsets**: Relative to the start of the script data section
- **Jump/Call Offsets**: Relative to the current position in the file (as indicated in decompiler output "Offset: 0x...")
- **Pointer Resolution**: On load, pointers are NOT adjusted for memory loading (unlike SIR0 format)

### Offset Table Example

For a file with 2 scripts and 1 function:

```
File Header:          [16 bytes]
Script Offsets:       [0x00000000, 0x00000020]     (2 scripts)
Script Terminator:    0xFD13
Function Offsets:     [0x00000045]                  (1 function)
Function Terminator:  0xFD13
Action Offsets:       []                            (0 actions)
Action Terminator:    0xFD13

Script Data:
  Script 0 (offset 0x00000000):
    05 00 60 00          [Wait 0x0060]
    02 00               [End]
    
  Script 1 (offset 0x00000020):
    16 00 45 00 00 00    [Jump @0x00000045]
    
Function Data:
  Function 0 (offset 0x00000045):
    1E 00 01 00          [Lock 0x0001]
    27 00                [Return]
```

## Key Implementation Details

### 1. File Structure in Code

```csharp
ScriptFile {
    FileID: int              // File identifier
    IsLevelScript: bool      // Level vs event script
    Scripts: List<ScriptContainer>    // Scripts
    Functions: List<ScriptContainer>  // Functions  
    Actions: List<ScriptContainer>    // Actions
}

ScriptContainer {
    ID: uint               // Container number/ID
    Type: ContainerType    // Script/Function/Action
    Commands: List<ScriptCommand>
    Offset: uint           // Original file offset (for reference)
    Name: string?          // Optional name/label
}

ScriptCommand {
    CommandID: ushort      // 0x0000-0x0347 range
    Parameters: List<byte[]>  // Raw parameter bytes
    Offset: uint           // Original offset in file
}
```

### 2. Little Endian Byte Order

All multi-byte values (Word, DWord, Offset) are stored in **Little Endian** format:

```
uint16 0x1234 → bytes [34, 12]
uint32 0x12345678 → bytes [78, 56, 34, 12]
```

### 3. Game-Version Differences

Different Pokemon games (DP, Platinum, HGSS) may have:
- Different command sets
- Different header formats
- Different parameter types for specific commands

The Clockwork implementation uses a JSON database (ScriptCommands.json) to handle this abstraction.

## File Size Calculation

Total file size = Header + OffsetTables + Terminators + CommandData

```csharp
int headerSize = 16;  // Base header
int offsetTableSize = (scriptCount + functionCount + actionCount) * 4 + (3 * 2); // terminators
int commandDataSize = sum of all command sizes
int totalSize = headerSize + offsetTableSize + commandDataSize;
```

## Container Layout Rules

1. **Offset Table Always First**: Before any command data
2. **Command Data Follows**: After all offset tables and terminators
3. **No Gaps**: Data is contiguous (except optional padding to 4-byte boundaries)
4. **Terminator Between Groups**: Each container type has a terminator (0xFD13)

## Binary Serialization Pseudocode

```
WriteHeader(file, scriptCount, functionCount, actionCount)
WriteOffsetTableAndData(file, Scripts)
WriteTerminator(file)
WriteOffsetTableAndData(file, Functions)
WriteTerminator(file)
WriteOffsetTableAndData(file, Actions)
WriteTerminator(file)

procedure WriteOffsetTableAndData(offsets[], containers[]):
  startPos = current file position
  for each container:
    WriteUInt32(startPos + offsetTableSize + (index * 4) + current offset in data)
  for each container:
    for each command in container:
      WriteUInt16(command.ID)
      for each parameter in command:
        WriteBytes(parameter.bytes)
```

## Practical Considerations for Implementation

1. **Offset Calculation**: Account for header and offset table size when calculating container offsets
2. **Memory Efficiency**: Build offset table after serializing command data to know exact positions
3. **Round-Trip Verification**: Ensure serialized data can be perfectly deserialized
4. **Bounds Checking**: Validate that all offset pointers stay within file bounds
5. **Command Database**: Use JSON to define command parameters for different game versions

## References

- PokePlat Disassembly: github.com/JimB16/PokePlat
- PPRE (Project Pokemon ROM Editor): github.com/projectpokemon/PPRE
- Project Pokemon Forums: projectpokemon.org/home/forums
- Clockwork Architecture: Uses LiTRE-based design pattern

