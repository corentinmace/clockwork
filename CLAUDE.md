# CLAUDE.md - AI Assistant Guide for Clockwork

**Last Updated:** 2025-11-19
**Project:** Clockwork - Modern Pokémon DS ROM Editor
**Framework:** .NET 8 with ImGui.NET and OpenTK
**Target Games:** Pokémon Gen IV (Diamond/Pearl/Platinum/HeartGold/SoulSilver)

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Architecture & Design Patterns](#architecture--design-patterns)
3. [Directory Structure](#directory-structure)
4. [Key Files Reference](#key-files-reference)
5. [Development Workflows](#development-workflows)
6. [Code Conventions](#code-conventions)
7. [Common Tasks](#common-tasks)
8. [Data Formats](#data-formats)
9. [Build & Deployment](#build--deployment)
10. [Important Gotchas](#important-gotchas)

---

## Project Overview

### What is Clockwork?

Clockwork is a modern, cross-platform Pokémon DS ROM editor built with .NET 8. It provides an interactive interface for editing:
- **Maps**: 32x32 tile grids with collision and terrain types
- **Buildings**: 3D models placed on maps with position/rotation/scale
- **Headers**: Map metadata (music, weather, scripts, events)
- **Matrices**: Spatial grids organizing maps in the game world

### Project Goals

- **Modern Stack**: .NET 8, ImGui for UI, OpenTK for OpenGL rendering
- **Cross-Platform**: Windows, Linux, macOS support
- **LiTRE Compatible**: Can read/write ROM extractions from LiTRE toolchain
- **Clean Architecture**: Service-oriented design with backend/frontend separation
- **Interactive**: Real-time visual feedback with ImGui immediate mode

### Current State

**Implemented:**
- ✅ ROM extraction and loading
- ✅ Map editing (collision/terrain painting, building management)
- ✅ Header editing (all 24-byte Platinum fields)
- ✅ Matrix editing (game world grids)
- ✅ Native file dialogs
- ✅ ImGui docking interface with dark theme
- ✅ NSBMD 3D model parser (5,000+ lines from kiwi.ds)

**Partially Implemented:**
- ⚠️ 3D model viewer (stub exists, not integrated)
- ⚠️ Model rendering in map editor (renderer exists, not connected)

**Not Yet Implemented:**
- ❌ NARC packing/unpacking
- ❌ Texture editing
- ❌ Script/event editing
- ❌ Multiple game version support (only Platinum currently)
- ❌ Unit tests

---

## Architecture & Design Patterns

### 1. Backend/Frontend Separation

```
┌─────────────────────────────────────┐
│      Clockwork.UI (Frontend)        │
│   ImGui + OpenTK + Views            │
└──────────────┬──────────────────────┘
               │ Depends on
               ↓
┌─────────────────────────────────────┐
│     Clockwork.Core (Backend)        │
│   Models + Services + Parsers       │
└─────────────────────────────────────┘
```

**Key Principle**: Core is UI-agnostic. It could support CLI, web, or other frontends.

### 2. Service-Oriented Architecture

All business logic lives in services implementing `IApplicationService`:

```csharp
public interface IApplicationService {
    void Initialize();           // Called once at startup
    void Update(double deltaTime); // Called every frame
    void Dispose();              // Called on shutdown
}
```

**Service Lifecycle:**
1. Created in `Program.Main()`
2. Registered via `appContext.AddService(service)`
3. Initialized via `appContext.Initialize()`
4. Updated every frame via `appContext.Update(deltaTime)`
5. Disposed via `appContext.Shutdown()`

**Available Services:**
- `RomService` - ROM loading and management
- `MapService` - Map file loading/saving
- `HeaderService` - Header loading/saving
- `DialogService` - Native file/folder dialogs
- `NdsToolService` - ndstool.exe wrapper for ROM extraction

**Dependency Injection (Manual):**
```csharp
// Services retrieve dependencies via GetService<T>()
var romService = _appContext.GetService<RomService>();

// Or via setter injection:
headerService.SetRomService(romService);
```

### 3. Model-View Pattern

- **Models** (`MapFile`, `Building`, `MapHeader`) - Pure data structures with serialization
- **Services** - Business logic, file I/O, state management
- **Views** - ImGui rendering, user interaction, state display

**No Controllers**: Views call services directly and hold UI state.

### 4. Binary I/O Pattern

All file formats use `BinaryReader`/`BinaryWriter` with explicit field order:

```csharp
public static MapFile ReadFromBytes(byte[] data) {
    using var ms = new MemoryStream(data);
    using var reader = new BinaryReader(ms);

    // Read fields in exact order from spec
    var permissions = reader.ReadInt32();
    var buildings = reader.ReadInt32();
    // ... etc
}

public byte[] ToBytes() {
    using var ms = new MemoryStream();
    using var writer = new BinaryWriter(ms);

    // Write fields in exact order to spec
    writer.Write(permissionsSectionLength);
    writer.Write(buildingsSectionLength);
    // ... etc

    return ms.ToArray();
}
```

### 5. ImGui Immediate Mode Pattern

Views redraw **every frame** (60+ FPS):

```csharp
public void Draw() {
    if (!IsVisible) return;

    ImGui.Begin("Window Title", ref IsVisible);

    // UI elements redraw every frame
    if (ImGui.Button("Click Me")) {
        // Handle click
    }

    ImGui.End();
}
```

**Key Features:**
- Docking enabled (`ImGuiConfigFlags.DockingEnable`)
- Custom dark theme in `MainWindow.ConfigureImGuiStyle()`
- No retained UI tree - everything is stateless rendering

---

## Directory Structure

```
clockwork/
├── src/
│   ├── Clockwork.Core/                    # Backend (UI-agnostic)
│   │   ├── Models/                        # Data structures
│   │   │   ├── MapFile.cs                 # 32x32 grid + buildings (240 lines)
│   │   │   ├── Building.cs                # 48-byte building (120 lines)
│   │   │   ├── MapHeader.cs               # 24-byte Platinum header (170 lines)
│   │   │   ├── GameMatrix.cs              # Variable-size map grid (225 lines)
│   │   │   ├── RomInfo.cs                 # ROM metadata
│   │   │   ├── GameVersion.cs             # Game version enums
│   │   │   ├── CollisionType.cs           # 9 collision types with colors
│   │   │   └── TerrainType.cs             # 40+ terrain types with colors
│   │   │
│   │   ├── Services/                      # Business logic
│   │   │   ├── RomService.cs              # ROM loading (196 lines)
│   │   │   ├── MapService.cs              # Map loading/saving (237 lines)
│   │   │   ├── HeaderService.cs           # Header loading/saving (208 lines)
│   │   │   ├── DialogService.cs           # Native file dialogs (104 lines)
│   │   │   └── NdsToolService.cs          # ndstool.exe wrapper (162 lines)
│   │   │
│   │   ├── Formats/NDS/                   # Nintendo DS file format parsers
│   │   │   ├── NSBMD/                     # 3D model format (5,021 lines total)
│   │   │   │   ├── NSBMD.cs               # Main container
│   │   │   │   ├── NSBMDLoader.cs         # Binary parser
│   │   │   │   ├── NSBMDModel.cs          # Model data
│   │   │   │   ├── NSBMDGlRenderer.cs     # OpenGL renderer
│   │   │   │   ├── NSBMDMaterial.cs       # Material system
│   │   │   │   ├── NSBMDTexture.cs        # Texture data
│   │   │   │   ├── NSBMDPalette.cs        # Palette system
│   │   │   │   ├── NSBMDPolygon.cs        # Geometry data
│   │   │   │   └── Utils/                 # Math helpers
│   │   │   │
│   │   │   ├── NSBTX/                     # Texture format
│   │   │   │   └── NSBTXLoader.cs
│   │   │   │
│   │   │   ├── MapFile/                   # Map file parser
│   │   │   │   └── MapFile.cs
│   │   │   │
│   │   │   └── Utils/                     # Binary utilities
│   │   │       ├── EndianBinaryReader.cs
│   │   │       ├── DSUtils.cs
│   │   │       └── NSBUtils.cs
│   │   │
│   │   ├── ApplicationContext.cs          # Service container (45 lines)
│   │   ├── IApplicationService.cs         # Service interface (10 lines)
│   │   └── Clockwork.Core.csproj
│   │
│   └── Clockwork.UI/                      # Frontend (ImGui interface)
│       ├── Views/                         # ImGui view components
│       │   ├── IView.cs                   # Base view interface (18 lines)
│       │   ├── MapEditorView.cs           # Interactive map editor (851 lines)
│       │   ├── HeaderEditorView.cs        # Header property editor (150+ lines)
│       │   ├── RomLoaderView.cs           # ROM extraction/loading (150+ lines)
│       │   ├── ModelViewerWindow.cs       # 3D model viewer (stub)
│       │   └── AboutView.cs               # About dialog
│       │
│       ├── Program.cs                     # Entry point + service registration (61 lines)
│       ├── MainWindow.cs                  # Main window + docking + menu (296 lines)
│       ├── ImGuiController.cs             # ImGui/OpenGL integration (401 lines)
│       └── Clockwork.UI.csproj
│
├── Tools/                                 # External tools
│   └── README.md                          # ndstool.exe instructions
│
├── Clockwork.sln                          # Visual Studio solution
├── README.md                              # User-facing documentation (French)
├── LITRE_CODE_EXAMPLES.md                 # Reference implementations from LiTRE
├── LITRE_MAP_EDITOR_ANALYSIS.md           # LiTRE architecture analysis
├── CLAUDE.md                              # This file (AI assistant guide)
└── .gitignore
```

**Total:** 43 C# files, ~8,500+ lines of code (excluding NSBMD library)

---

## Key Files Reference

### Critical Files to Understand

#### 1. **Entry Point & Service Setup**
- **src/Clockwork.UI/Program.cs:1-61** - Main entry point, service registration, OpenTK window creation

#### 2. **Service Container**
- **src/Clockwork.Core/ApplicationContext.cs:1-45** - Service lifecycle management (Add, Get, Initialize, Update, Dispose)

#### 3. **Main Window & UI**
- **src/Clockwork.UI/MainWindow.cs:1-296** - Menu bar, sidebar navigation, docking setup, dark theme configuration
- **src/Clockwork.UI/ImGuiController.cs:1-401** - ImGui/OpenGL 3.3 integration, input handling, rendering

#### 4. **Core Services**
- **src/Clockwork.Core/Services/RomService.cs:1-196** - ROM loading, validation, directory structure
- **src/Clockwork.Core/Services/MapService.cs:1-237** - Map file loading/saving, current map state
- **src/Clockwork.Core/Services/HeaderService.cs:1-208** - Header loading/saving, internal names

#### 5. **Data Models**
- **src/Clockwork.Core/Models/MapFile.cs:1-240** - 32x32 collision/terrain grid + building list
- **src/Clockwork.Core/Models/Building.cs:1-120** - 48-byte building structure with position/rotation/scale
- **src/Clockwork.Core/Models/MapHeader.cs:1-170** - 24-byte Platinum header with 14+ fields

#### 6. **Map Editor UI**
- **src/Clockwork.UI/Views/MapEditorView.cs:1-851** - Interactive grid painter, building editor, tool palette

#### 7. **File Format Parsers**
- **src/Clockwork.Core/Formats/NDS/NSBMD/** - Complete NSBMD 3D model parser (5,021 lines from kiwi.ds)
- **src/Clockwork.Core/Formats/NDS/Utils/EndianBinaryReader.cs** - Little-endian binary reader

---

## Development Workflows

### Adding a New Service

1. **Create service class** in `src/Clockwork.Core/Services/`:
```csharp
namespace Clockwork.Core.Services;

public class MyService : IApplicationService
{
    private ApplicationContext _appContext;

    public MyService(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Initialize()
    {
        // One-time setup
    }

    public void Update(double deltaTime)
    {
        // Per-frame logic (optional)
    }

    public void Dispose()
    {
        // Cleanup resources
    }
}
```

2. **Register service** in `src/Clockwork.UI/Program.cs`:
```csharp
var myService = new MyService(appContext);
appContext.AddService(myService);
```

3. **Use in views**:
```csharp
private MyService? _myService;

public void Initialize(ApplicationContext appContext)
{
    _myService = appContext.GetService<MyService>();
}
```

### Adding a New View

1. **Create view class** in `src/Clockwork.UI/Views/`:
```csharp
namespace Clockwork.UI.Views;

public class MyView : IView
{
    public bool IsVisible { get; set; } = false;
    private ApplicationContext _appContext;

    public void Initialize(ApplicationContext appContext)
    {
        _appContext = appContext;
        // Get required services
    }

    public void Draw()
    {
        if (!IsVisible) return;

        ImGui.Begin("My Window", ref IsVisible);

        // Draw UI here
        ImGui.Text("Hello from MyView!");

        if (ImGui.Button("Click Me"))
        {
            // Handle action
        }

        ImGui.End();
    }
}
```

2. **Register view** in `src/Clockwork.UI/MainWindow.cs`:
```csharp
private MyView _myView = new();

// In Initialize():
_myView.Initialize(_appContext);

// In DrawUI():
_myView.Draw();
```

3. **Add menu item** in `MainWindow.DrawMenuBar()`:
```csharp
if (ImGui.MenuItem("My View"))
{
    _myView.IsVisible = true;
}
```

### Adding a New Data Model

1. **Create model class** in `src/Clockwork.Core/Models/`:
```csharp
namespace Clockwork.Core.Models;

public class MyModel
{
    // Properties
    public int MyProperty { get; set; }

    // Deserialization
    public static MyModel ReadFromBytes(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        return new MyModel
        {
            MyProperty = reader.ReadInt32()
        };
    }

    // Serialization
    public byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(MyProperty);

        return ms.ToArray();
    }
}
```

2. **Add service methods** to load/save the model
3. **Create view** to edit the model

### Working with Binary File Formats

**Always use `BinaryReader`/`BinaryWriter` in exact field order:**

```csharp
// Reading
using var ms = new MemoryStream(fileData);
using var reader = new BinaryReader(ms);

var field1 = reader.ReadInt32();
var field2 = reader.ReadUInt16();
var field3 = reader.ReadByte();
// Read fields in spec order

// Writing
using var ms = new MemoryStream();
using var writer = new BinaryWriter(ms);

writer.Write(field1);
writer.Write(field2);
writer.Write(field3);
// Write fields in spec order

return ms.ToArray();
```

**Important:**
- Nintendo DS is **little-endian** (Intel byte order)
- Use `BinaryReader` directly (defaults to little-endian)
- For big-endian data, use `EndianBinaryReader` from `Formats/NDS/Utils/`

---

## Code Conventions

### Naming Conventions

**Strictly follow C# standards:**

| Element | Convention | Example |
|---------|-----------|---------|
| **Namespaces** | PascalCase | `Clockwork.Core.Services` |
| **Classes** | PascalCase | `MapFile`, `ApplicationContext` |
| **Interfaces** | I + PascalCase | `IApplicationService`, `IView` |
| **Public Properties** | PascalCase | `MapID`, `IsLoaded`, `Buildings` |
| **Public Methods** | PascalCase | `GetCollision()`, `LoadMap()` |
| **Private Fields** | _camelCase | `_appContext`, `_currentMap`, `_romService` |
| **Parameters** | camelCase | `deltaTime`, `mapID`, `headerID` |
| **Local Variables** | camelCase | `collision`, `terrain`, `building` |
| **Constants** | UPPER_CASE | `MAP_SIZE`, `BUILDING_SIZE`, `NDS_TYPE_BMD0` |

### File Organization

- **One class per file** (standard C# practice)
- **Filename matches class name**: `MapFile.cs`, `IApplicationService.cs`
- **Nested classes**: Only for tightly coupled helpers (e.g., `Building` inside `MapFile` in LiTRE)

### Language & Comments

- **Code**: English (all identifiers, methods, classes)
- **Comments**: Mixed (English preferred, French acceptable)
- **UI Labels**: French (target audience is French-speaking)

Example:
```csharp
// Load the collision data from the map file
private void LoadCollisions() // English method name
{
    // La grille de collision est toujours 32x32 (French comment OK)
    for (int x = 0; x < MapFile.MAP_SIZE; x++)
    {
        // ...
    }
}
```

### Project References

**Never use relative paths across projects.**

Use project references in `.csproj`:
```xml
<ItemGroup>
  <ProjectReference Include="..\Clockwork.Core\Clockwork.Core.csproj" />
</ItemGroup>
```

### Nullable Reference Types

**.NET 8 nullable annotations are enabled:**
```xml
<Nullable>enable</Nullable>
```

**Use nullable types explicitly:**
```csharp
private RomService? _romService;  // Nullable
private MapFile? _currentMap;     // Nullable
private List<Building> _buildings = new(); // Non-nullable with initializer
```

---

## Common Tasks

### Task 1: Load a ROM

```csharp
var romService = appContext.GetService<RomService>();

// Option 1: Extract from .nds file
await romService.ExtractRom(ndsFilePath, outputFolder);
romService.LoadRom(outputFolder);

// Option 2: Load from already extracted folder
romService.LoadRom(extractedFolder);

// Check if loaded
if (romService.IsLoaded)
{
    var romInfo = romService.RomInfo;
    Console.WriteLine($"Game: {romInfo.GameCode}");
}
```

### Task 2: Load and Edit a Map

```csharp
var mapService = appContext.GetService<MapService>();

// Load map by ID
mapService.LoadMap(mapID);

if (mapService.CurrentMap != null)
{
    var map = mapService.CurrentMap;

    // Edit collision at position
    map.SetCollision(x, y, CollisionType.Water);

    // Edit terrain at position
    map.SetTerrain(x, y, TerrainType.Grass);

    // Add a building
    var building = new Building
    {
        ModelID = 10,
        XPosition = 5,
        ZPosition = 10,
        Width = 16,
        Height = 16,
        Length = 16
    };
    map.AddBuilding(building);

    // Save changes
    mapService.SaveCurrentMap();
}
```

### Task 3: Load and Edit a Header

```csharp
var headerService = appContext.GetService<HeaderService>();

// Load header by ID
var header = headerService.LoadHeader(headerID);

if (header != null)
{
    // Edit properties
    header.MusicDayID = 425;
    header.MusicNightID = 426;
    header.Weather = 3;
    header.CameraAngle = 1;

    // Save changes
    headerService.SaveHeader(headerID, header);
}

// Get internal name
var name = headerService.GetInternalName(headerID);
Console.WriteLine($"Map name: {name}");
```

### Task 4: Draw an ImGui Window

```csharp
public void Draw()
{
    if (!IsVisible) return;

    ImGui.Begin("My Editor", ref IsVisible);

    // Text
    ImGui.Text("Map Editor");
    ImGui.Separator();

    // Input
    ImGui.InputInt("Map ID", ref _mapID);

    // Button
    if (ImGui.Button("Load Map"))
    {
        LoadMap(_mapID);
    }

    // Checkbox
    ImGui.Checkbox("Show Grid", ref _showGrid);

    // Combo box
    if (ImGui.BeginCombo("Tool", _currentTool))
    {
        if (ImGui.Selectable("Paint")) _currentTool = "Paint";
        if (ImGui.Selectable("Erase")) _currentTool = "Erase";
        ImGui.EndCombo();
    }

    ImGui.End();
}
```

### Task 5: Handle Mouse Input in ImGui

```csharp
// Check if mouse is over window content area
if (ImGui.IsWindowHovered())
{
    var mousePos = ImGui.GetMousePos();
    var windowPos = ImGui.GetWindowPos();

    // Convert to window-relative coordinates
    var relativeX = mousePos.X - windowPos.X;
    var relativeY = mousePos.Y - windowPos.Y;

    // Check if mouse button is down
    if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
    {
        // Handle left mouse drag
        HandlePaint(relativeX, relativeY);
    }

    // Check for click (down + released)
    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
    {
        // Handle single click
        SelectCell(relativeX, relativeY);
    }
}
```

---

## Data Formats

### MapFile Structure (32x32 Grid + Buildings)

**Binary Layout:**
```
[4 bytes] Permissions Section Length
[4 bytes] Buildings Section Length
[4 bytes] NSBMD Section Length
[4 bytes] BDHC Section Length
[variable] Permissions Data (collision + terrain)
[variable] Buildings Data (48 bytes per building)
[variable] NSBMD Model Data
[variable] BDHC Terrain Height Data
```

**Permissions Data:**
- 32x32 grid (1024 bytes total)
- 2 bits per cell for collision (4 values: 0-3)
- 6 bits per cell for terrain type (64 values: 0-63)
- Packed as bytes: `byte = (terrain << 2) | collision`

**Collision Types (9 types):**
```csharp
public enum CollisionType : byte
{
    Walkable = 0,      // Green
    Water = 1,         // Blue
    Rail = 2,          // Orange
    Blocked = 3,       // Red
    // ... (see CollisionType.cs for all 9)
}
```

**Terrain Types (40+ types):**
```csharp
public enum TerrainType : byte
{
    Normal = 0,        // Gray
    Grass = 1,         // Light Green
    Sand = 2,          // Yellow
    Snow = 3,          // White
    // ... (see TerrainType.cs for all types)
}
```

### Building Structure (48 bytes)

```
Offset | Size | Type   | Field
-------|------|--------|------------------
0x00   | 4    | uint   | Model ID
0x04   | 2    | short  | X Position (integer part)
0x06   | 2    | ushort | X Fraction (1/65536ths)
0x08   | 2    | short  | Y Position (integer part)
0x0A   | 2    | ushort | Y Fraction (1/65536ths)
0x0C   | 2    | short  | Z Position (integer part)
0x0E   | 2    | ushort | Z Fraction (1/65536ths)
0x10   | 2    | ushort | X Rotation (65536 = 360°)
0x12   | 2    | ushort | Y Rotation (65536 = 360°)
0x14   | 2    | ushort | Z Rotation (65536 = 360°)
0x16   | 4    | uint   | Width (usually 16)
0x1A   | 4    | uint   | Height (usually 16)
0x1E   | 4    | uint   | Length (usually 16)
0x22   | 14   | -      | Padding (zeros)
```

**Position Calculation:**
```csharp
float actualX = building.XPosition + (building.XFraction / 65536.0f);
float actualY = building.YPosition + (building.YFraction / 65536.0f);
float actualZ = building.ZPosition + (building.ZFraction / 65536.0f);
```

**Rotation Calculation:**
```csharp
float angleX = (building.XRotation / 65536.0f) * 360.0f; // degrees
float angleY = (building.YRotation / 65536.0f) * 360.0f;
float angleZ = (building.ZRotation / 65536.0f) * 360.0f;
```

### MapHeader Structure (24 bytes - Platinum)

```
Offset | Size | Type   | Field
-------|------|--------|------------------
0x00   | 1    | byte   | Terrain Type
0x01   | 1    | byte   | Battle Background
0x02   | 2    | ushort | Unknown
0x04   | 2    | ushort | Music Day ID
0x06   | 2    | ushort | Music Night ID
0x08   | 2    | ushort | Event File ID
0x0A   | 1    | byte   | Weather
0x0B   | 1    | byte   | Camera Angle
0x0C   | 1    | byte   | Unknown
0x0D   | 1    | byte   | Unknown
0x0E   | 2    | ushort | Script File ID
0x10   | 2    | ushort | Unknown
0x12   | 2    | ushort | Unknown
0x14   | 2    | ushort | Unknown
0x16   | 2    | ushort | Unknown
0x18   | 4    | uint   | Flags
0x1C   | 4    | -      | Padding
```

### GameMatrix Structure

```
[1 byte]  Width
[1 byte]  Height
[2 bytes] Padding
[Width * Height * 2 bytes] Map IDs (ushort each)
[Width * Height] Heights (byte each)
```

**Notes:**
- Map ID of `0xFFFF` means "no map" (empty cell)
- Heights are relative elevation values

---

## Build & Deployment

### Prerequisites

- **.NET 8 SDK** - https://dotnet.microsoft.com/download/dotnet/8.0
- **OpenGL 3.3+ compatible GPU** with up-to-date drivers
- **ndstool.exe** - Download from https://github.com/DS-Pokemon-Rom-Editor/DSPRE
  - Place in `Tools/ndstool.exe`
  - Not included in repository (external dependency)

### Build Commands

```bash
# Restore dependencies
dotnet restore

# Build Debug (default)
dotnet build

# Build Release
dotnet build -c Release

# Run from root
dotnet run --project src/Clockwork.UI

# Run from UI directory
cd src/Clockwork.UI
dotnet run
```

### Project Configuration

**Clockwork.Core.csproj:**
```xml
<TargetFramework>net8.0</TargetFramework>
<ImplicitUsings>enable</ImplicitUsings>
<Nullable>enable</Nullable>
<LangVersion>latest</LangVersion>
```

**Clockwork.UI.csproj:**
```xml
<OutputType>WinExe</OutputType>              <!-- GUI app, no console -->
<TargetFramework>net8.0</TargetFramework>
<AllowUnsafeBlocks>true</AllowUnsafeBlocks>  <!-- For ImGui interop -->

<!-- Copy Tools folder to output -->
<ItemGroup>
  <None Include="..\..\Tools\**">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### Deployment Strategies

**Option 1: Framework-Dependent (smaller)**
```bash
dotnet publish -c Release
```
- **Pros**: Small (~10 MB)
- **Cons**: Requires .NET 8 runtime on target machine

**Option 2: Self-Contained (larger)**
```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained

# Linux
dotnet publish -c Release -r linux-x64 --self-contained

# macOS
dotnet publish -c Release -r osx-x64 --self-contained
```
- **Pros**: No .NET runtime required, portable
- **Cons**: Larger (~70 MB per platform)

**Don't forget:**
- Include `Tools/ndstool.exe` in distribution
- Include instructions for OpenGL driver requirements

---

## Important Gotchas

### 1. ndstool.exe is NOT in Git

**Issue**: The `Tools/` folder is copied to output, but `ndstool.exe` is not committed to git.

**Solution**: Users must download it separately from:
https://github.com/DS-Pokemon-Rom-Editor/DSPRE

**Code Location**: `NdsToolService.cs:36` checks for tool existence:
```csharp
var toolPath = Path.Combine(AppContext.BaseDirectory, "Tools", "ndstool.exe");
if (!File.Exists(toolPath))
{
    throw new FileNotFoundException("ndstool.exe not found in Tools folder");
}
```

### 2. Map Grid is Always 32x32

**Issue**: Maps are ALWAYS 32x32 tiles, regardless of actual terrain size.

**Why**: Pokémon DS uses fixed-size collision grids for performance.

**Code Location**: `MapFile.cs:11`
```csharp
public const int MAP_SIZE = 32;
public byte[,] Collisions = new byte[MAP_SIZE, MAP_SIZE];
public byte[,] Terrain = new byte[MAP_SIZE, MAP_SIZE];
```

**Implication**: Smaller maps waste space, larger maps need multiple connected maps.

### 3. Building Positions Use Fractional Parts

**Issue**: Building coordinates have both integer and fractional parts (1/65536 precision).

**Why**: Allows sub-tile positioning for smooth object placement.

**Code Location**: `Building.cs:16-21`
```csharp
public short XPosition { get; set; }
public ushort XFraction { get; set; }  // 0-65535 = 0.0-0.99998

// Actual position:
float actualX = XPosition + (XFraction / 65536.0f);
```

**Implication**: Always use both parts when positioning buildings.

### 4. ImGui Draws Every Frame

**Issue**: All UI code runs 60+ times per second.

**Why**: ImGui is an immediate-mode GUI (no retained UI tree).

**Implication**:
- Avoid heavy computation in `Draw()` methods
- Cache expensive calculations
- Use dirty flags to track changes

```csharp
// BAD - recomputes every frame
public void Draw()
{
    var heavyResult = ExpensiveCalculation(); // 60+ times/second!
    ImGui.Text($"Result: {heavyResult}");
}

// GOOD - cache and recompute only on change
private string _cachedResult = "";
private bool _isDirty = true;

public void Draw()
{
    if (_isDirty)
    {
        _cachedResult = ExpensiveCalculation(); // Only when needed
        _isDirty = false;
    }
    ImGui.Text($"Result: {_cachedResult}");
}
```

### 5. Service Dependencies Must Be Manual

**Issue**: No automatic dependency injection container.

**Why**: Keeping architecture simple for small project.

**Code Location**: Services retrieve dependencies via `GetService<T>()`
```csharp
public void Initialize()
{
    _romService = _appContext.GetService<RomService>();
    _mapService = _appContext.GetService<MapService>();
}
```

**Implication**:
- Services must call `GetService<T>()` in `Initialize()`
- Null-check returned services
- Be aware of service initialization order

### 6. ROM Directory Structure is Specific

**Issue**: ROM must be extracted in LiTRE-compatible structure.

**Expected Structure:**
```
rom_folder/
├── header.bin                  # Must exist for game code detection
├── data/
│   └── fielddata/
│       └── maptable/
│           └── mapname.bin     # Internal names
└── unpacked/                   # NARC extracted files
    ├── dynamicHeaders/         # Map headers (*.bin)
    ├── maps/                   # Map files (*.bin)
    └── matrices/               # Matrix files (*.bin)
```

**Code Location**: `RomService.cs:52-80` validates structure

**Implication**: Only works with properly extracted ROMs (use ndstool.exe or LiTRE).

### 7. Unsafe Code Required for ImGui

**Issue**: `AllowUnsafeBlocks` must be enabled in UI project.

**Why**: ImGui.NET uses pointers for performance.

**Code Location**: `Clockwork.UI.csproj:6`
```xml
<AllowUnsafeBlocks>true</AllowUnsafeBlocks>
```

**Implication**: Code analysis may warn about unsafe code blocks (this is expected).

### 8. OpenTK Window Lifecycle is Strict

**Issue**: OpenTK `GameWindow` has strict initialization order.

**Sequence**:
1. Constructor - create window
2. `OnLoad()` - initialize OpenGL, ImGui, services
3. `OnUpdateFrame()` - update logic, services
4. `OnRenderFrame()` - clear, render, swap buffers
5. `OnUnload()` - dispose resources

**Code Location**: `MainWindow.cs:26-90`

**Implication**: Don't call OpenGL functions before `OnLoad()`.

### 9. NSBMD Renderer is Not Yet Integrated

**Issue**: Full NSBMD renderer exists (5,000+ lines) but is not connected to map editor.

**Why**: Still in development.

**Code Location**: `src/Clockwork.Core/Formats/NDS/NSBMD/`

**Implication**: 3D models can be parsed but not yet displayed in map editor.

### 10. Only Platinum is Fully Supported

**Issue**: Code assumes Pokémon Platinum data structures.

**Why**: Initial target game.

**Code Locations**:
- `MapHeader.cs` - 24-byte Platinum format
- `RomService.cs` - Detects game code but uses Platinum offsets

**Implication**: Diamond/Pearl/HGSS may need format adjustments.

---

## Testing Guidelines

### Current State

**No test project exists yet.**

### Recommended Testing Strategy

When implementing tests, focus on:

1. **Binary I/O** (highest priority)
   - MapFile serialization/deserialization
   - Building serialization/deserialization
   - MapHeader serialization/deserialization
   - GameMatrix serialization/deserialization

2. **Data Model Methods**
   - `MapFile.GetCollision()`, `SetCollision()`
   - `MapFile.GetTerrain()`, `SetTerrain()`
   - `MapFile.IsCellWalkable()`
   - `Building.CalculateActualPosition()`

3. **Service Logic**
   - `RomService.ValidateRomStructure()`
   - `MapService.LoadMap()`, `SaveMap()`
   - `HeaderService.LoadHeader()`, `SaveHeader()`

### Test Project Setup

```bash
# Create test project
dotnet new xunit -n Clockwork.Core.Tests
cd Clockwork.Core.Tests
dotnet add reference ../src/Clockwork.Core/Clockwork.Core.csproj

# Add to solution
dotnet sln add Clockwork.Core.Tests/Clockwork.Core.Tests.csproj
```

### Example Test

```csharp
using Xunit;
using Clockwork.Core.Models;

public class MapFileTests
{
    [Fact]
    public void SerializeDeserialize_ShouldPreserveData()
    {
        // Arrange
        var original = new MapFile();
        original.SetCollision(5, 10, CollisionType.Water);
        original.SetTerrain(5, 10, TerrainType.Grass);

        // Act
        var bytes = original.ToBytes();
        var deserialized = MapFile.ReadFromBytes(bytes);

        // Assert
        Assert.Equal(CollisionType.Water, deserialized.GetCollision(5, 10));
        Assert.Equal(TerrainType.Grass, deserialized.GetTerrain(5, 10));
    }
}
```

---

## Reference Documentation

### External Resources

- **LiTRE Repository**: https://github.com/corentinmace/LiTRE
  - Original Windows Forms editor
  - Reference implementations in `LITRE_CODE_EXAMPLES.md`
  - Architecture analysis in `LITRE_MAP_EDITOR_ANALYSIS.md`

- **DSPRE Repository**: https://github.com/DS-Pokemon-Rom-Editor/DSPRE
  - Source for `ndstool.exe`
  - Alternative ROM editor

- **ImGui Documentation**: https://github.com/ocornut/imgui
  - Original C++ ImGui library
  - API reference and examples

- **ImGui.NET**: https://github.com/ImGuiNET/ImGui.NET
  - .NET bindings for ImGui
  - C# API documentation

- **OpenTK**: https://opentk.net/
  - OpenGL bindings for .NET
  - Windowing and input handling

- **.NET 8 Documentation**: https://learn.microsoft.com/dotnet/
  - Language features, APIs, best practices

### Internal Documentation

- **README.md** - User-facing documentation (architecture, installation, usage)
- **LITRE_CODE_EXAMPLES.md** - Code snippets from LiTRE for reference
- **LITRE_MAP_EDITOR_ANALYSIS.md** - Detailed analysis of LiTRE architecture
- **Tools/README.md** - Instructions for obtaining ndstool.exe

---

## Quick Reference

### Common File Paths

```
Entry Point:     src/Clockwork.UI/Program.cs
Main Window:     src/Clockwork.UI/MainWindow.cs
Map Editor View: src/Clockwork.UI/Views/MapEditorView.cs

ROM Service:     src/Clockwork.Core/Services/RomService.cs
Map Service:     src/Clockwork.Core/Services/MapService.cs
Header Service:  src/Clockwork.Core/Services/HeaderService.cs

Map Model:       src/Clockwork.Core/Models/MapFile.cs
Building Model:  src/Clockwork.Core/Models/Building.cs
Header Model:    src/Clockwork.Core/Models/MapHeader.cs
```

### Build Commands Cheat Sheet

```bash
# Restore packages
dotnet restore

# Build
dotnet build                    # Debug
dotnet build -c Release         # Release

# Run
dotnet run --project src/Clockwork.UI

# Clean
dotnet clean

# Publish
dotnet publish -c Release -r win-x64 --self-contained
```

### Common ImGui Patterns

```csharp
// Window
ImGui.Begin("Title", ref isVisible);
// ... content
ImGui.End();

// Button
if (ImGui.Button("Click")) { /* action */ }

// Input
ImGui.InputInt("Label", ref intValue);
ImGui.InputText("Label", ref strValue, 256);

// Checkbox
ImGui.Checkbox("Label", ref boolValue);

// Combo
if (ImGui.BeginCombo("Label", currentValue))
{
    if (ImGui.Selectable("Option 1")) currentValue = "Option 1";
    if (ImGui.Selectable("Option 2")) currentValue = "Option 2";
    ImGui.EndCombo();
}

// Separator
ImGui.Separator();

// Spacing
ImGui.Spacing();

// Same line
ImGui.SameLine();
```

---

## Version History

- **2025-11-19**: Initial CLAUDE.md creation
  - Comprehensive codebase analysis
  - Architecture documentation
  - Development workflows
  - Code conventions
  - Common tasks reference

---

**End of CLAUDE.md**

For questions or clarifications about this codebase, refer to:
1. This CLAUDE.md file for architecture and patterns
2. README.md for user-facing documentation
3. LITRE_CODE_EXAMPLES.md for reference implementations
4. Source code comments for implementation details
