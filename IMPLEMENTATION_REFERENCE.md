# NDS Script File Implementation Reference

## File Locations

### Main Implementation File
**Path**: `/home/user/clockwork/src/Clockwork.Core/Formats/NDS/Scripts/ScriptFile.cs`

**Methods to implement**:
- Lines 51-63: `public static ScriptFile FromBinary(byte[] data, int fileID = 0)`
- Lines 82-92: `public byte[] ToBytes()`

### Supporting Classes (Already Implemented)

**ScriptCommand.cs** (Lines 49-64)
```csharp
public byte[] ToBytes()
{
    using var ms = new MemoryStream();
    using var writer = new BinaryWriter(ms);
    
    writer.Write(CommandID);  // 2 bytes
    foreach (var param in Parameters)
    {
        writer.Write(param);  // variable bytes
    }
    return ms.ToArray();
}
```
✓ This already works perfectly - use it as reference

**ScriptContainer.cs** (Lines 69-80)
```csharp
public byte[] ToBytes()
{
    using var ms = new MemoryStream();
    foreach (var command in Commands)
    {
        var commandBytes = command.ToBytes();
        ms.Write(commandBytes, 0, commandBytes.Length);
    }
    return ms.ToArray();
}
```
✓ This is partial but shows the pattern

**ScriptDatabase.cs**
- Loads command definitions from JSON
- `GetCommandInfo(ushort commandID)` - returns ScriptCommandInfo
- `GetCommandInfo(string commandName)` - returns ScriptCommandInfo

**ScriptCommands.json**
- Location: `/home/user/clockwork/src/Clockwork.Core/Resources/ScriptCommands.json`
- Contains Platinum commands with IDs, names, and parameter types

## Binary Format Summary (Quick Reference)

### File Structure
```
[Header: 16 bytes]
[Script Offsets: N×4 bytes] [Terminator: 2 bytes]
[Function Offsets: M×4 bytes] [Terminator: 2 bytes]
[Action Offsets: K×4 bytes] [Terminator: 2 bytes]
[Script Data: variable] [Function Data: variable] [Action Data: variable]
```

### Header Format (Little Endian)
```
Offset  Size  Type     Field
------  ----  -------  -----------
0x00    4     uint32   Magic
0x04    2     uint16   Version
0x06    2     uint16   Reserved
0x08    2     uint16   ScriptCount (N)
0x0A    2     uint16   FunctionCount (M)
0x0C    2     uint16   ActionCount (K)
0x0E    2     uint16   Reserved
```

### Offset Tables
- Each container type gets an offset table
- Offsets are relative to command data start (NOT absolute)
- Table terminated with 0xFD13 (uint16)

### Command Data
- Sequence of commands: [ID (2 bytes)] [Params (variable)]
- No separators, no padding between commands
- Parameter types from ScriptParameterType enum

## Critical Implementation Details

### 1. Offset Calculation

Offset of command data start:
```csharp
long dataBaseOffset = 16 +                           // Header
                     (scriptCount * 4 + 2) +         // Script offsets + terminator
                     (functionCount * 4 + 2) +       // Function offsets + terminator
                     (actionCount * 4 + 2);          // Action offsets + terminator
```

When reading offsets from file:
```csharp
long actualFileOffset = dataBaseOffset + offsetFromTable;
```

### 2. Terminator Handling

After each offset table:
```csharp
// Writing
writer.Write((ushort)0xFD13);

// Reading
ushort terminator = reader.ReadUInt16();
if (terminator != 0xFD13)
{
    // Warning or error
}
```

### 3. Parameter Parsing

```csharp
foreach (var paramType in commandInfo.Parameters)
{
    byte[] paramBytes = paramType switch
    {
        ScriptParameterType.Byte => new[] { data[pos++] },
        ScriptParameterType.Word => 
        {
            var bytes = data[pos..(pos+2)];
            pos += 2;
            bytes
        },
        ScriptParameterType.DWord =>
        {
            var bytes = data[pos..(pos+4)];
            pos += 4;
            bytes
        },
        ScriptParameterType.Offset =>
        {
            var bytes = data[pos..(pos+4)];
            pos += 4;
            bytes  // Still just bytes, interpretation happens elsewhere
        },
        ScriptParameterType.Variable => throw new NotImplementedException(),
        _ => throw new ArgumentException()
    };
    command.Parameters.Add(paramBytes);
}
```

### 4. Container Boundary Detection

When parsing commands, you need to know when one container ends and another begins:

```csharp
// For container at index i in offset array
long containerStart = offsetTable[i];
long containerEnd = (i + 1 < offsetTable.Length) 
    ? offsetTable[i + 1]  // Next container
    : endOfAllCommandsForThisType;

// Parse commands from containerStart to containerEnd
while (currentPos < containerEnd)
{
    // Read command...
}
```

### 5. Serialization: Offset Table Writing

```csharp
// After calculating all container data:
long currentOffset = 0;  // Relative to data start

foreach (var container in containers)
{
    writer.Write((uint)currentOffset);
    // Add container size to current offset
    currentOffset += CalculateContainerSize(container);
}

// Write terminator
writer.Write((ushort)0xFD13);
```

## Related Utility Classes

### EndianBinaryReader.cs
Located at: `/home/user/clockwork/src/Clockwork.Core/Formats/NDS/EndianBinaryReader.cs`

Provides methods for reading various data types:
- `ReadByte()`, `ReadInt32()`, `ReadUInt32()`, etc.
- Handles endianness automatically
- Already used in project

### Utils.cs
Located at: `/home/user/clockwork/src/Clockwork.Core/Formats/NDS/Utils.cs`

Contains helper functions for byte manipulation:
- `Read2BytesAsushort(byte[], int offset)`
- `Read4BytesAsUInt32(byte[], int offset)`
- etc.

## Test Cases to Implement

### Test 1: Empty File
```csharp
var file = new ScriptFile();
var bytes = file.ToBytes();
var restored = ScriptFile.FromBinary(bytes);

Assert.Equal(0, restored.Scripts.Count);
Assert.Equal(0, restored.Functions.Count);
Assert.Equal(0, restored.Actions.Count);
```

### Test 2: Single Script with End Command
```csharp
var file = new ScriptFile();
var script = new ScriptContainer(0, ContainerType.Script);
script.Commands.Add(new ScriptCommand(0x0002));  // End command
file.Scripts.Add(script);

var bytes = file.ToBytes();
var restored = ScriptFile.FromBinary(bytes);

Assert.Single(restored.Scripts);
Assert.Single(restored.Scripts[0].Commands);
Assert.Equal(0x0002, restored.Scripts[0].Commands[0].CommandID);
```

### Test 3: Round-Trip Verification
```csharp
byte[] originalData = File.ReadAllBytes("test_script.bin");
var file = ScriptFile.FromBinary(originalData);
byte[] serialized = file.ToBytes();

// These should match (byte-for-byte identical)
Assert.Equal(originalData, serialized);
```

## Command Database Integration

The ScriptDatabase is already initialized in ScriptFile.FromBinary():

```csharp
// This happens automatically when you call:
var commandInfo = ScriptDatabase.GetCommandInfo(commandID);

// Returns ScriptCommandInfo with:
// - ID: ushort (command ID)
// - Name: string (e.g., "End", "Jump")
// - Parameters: List<ScriptParameterType> (parameter types)
// - Description: string
```

## Debugging Hints

### If offsets are wrong:
- Check if you're calculating dataBaseOffset correctly
- Remember offsets in table are relative, not absolute
- Add debug output for each offset table read

### If commands don't parse:
- Verify command is in ScriptCommands.json
- Check parameter types match what you're reading
- Print hex dump of command bytes to compare

### If serialization doesn't match original:
- Check header values (especially counts)
- Verify terminators are written (0xFD13)
- Check offset table values are correct
- Verify no extra padding added

## File Format Specification References

All detailed information is in:
1. **NDS_SCRIPT_FORMAT.md** - Complete format spec
2. **SCRIPT_SERIALIZATION_GUIDE.md** - Implementation guide with pseudocode
3. **SCRIPT_FORMAT_SUMMARY.md** - Quick reference summary

## Relevant Source Code Patterns

### Similar Binary Parsing in Codebase

Check these files for patterns to follow:
- `Building.cs` - ReadFromBytes() method
- `MapFile.cs` - Binary import/export patterns
- `GameMatrix.cs` - ToByteArray() method

They all follow similar patterns for binary serialization.

