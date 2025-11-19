# LiTRE Map Editor Implementation Analysis

## Project Overview
**LiTRE** is a major overhaul of a Nintendo DS Pokemon ROM Editor written entirely in C#/.NET. It provides comprehensive tools for modifying Pokemon Gen IV ROM files (Diamond/Pearl, Platinum, HeartGold/SoulSilver).

**Repository**: https://github.com/corentinmace/LiTRE  
**Language**: C# (100%)  
**Framework**: .NET with Windows Forms UI

---

## 1. Main Map Editor Architecture

### Primary Classes

#### **MapEditor.cs** (`/DS_Map/Editors/MapEditor.cs`)
- **Type**: Windows Forms UserControl
- **Purpose**: Main interactive map editing interface combining 3D visualization and tile-based collision/permission editing

**Key Features**:
- 3D map model rendering with OpenGL
- Building placement and editing
- Collision map painting (32x32 grid)
- Permission type editing for each tile
- Real-time camera control (angle, distance, elevation)
- Texture pack loading for maps and buildings

**Static Renderers**:
```csharp
public static NSBMDGlRenderer mapRenderer = new NSBMDGlRenderer();
public static NSBMDGlRenderer buildingsRenderer = new NSBMDGlRenderer();
```

#### **MatrixEditor.cs** (`/DS_Map/Editors/MatrixEditor.cs`)
- **Type**: Windows Forms UserControl
- **Purpose**: Edit spatial grids (matrices) that organize maps in the game world

**Key Features**:
- 3 synchronized DataGridView tables for Headers, Heights, and Maps grids
- Dynamic matrix resizing (width/height adjustment)
- Double-click navigation to map/header editors
- Color-coded cell formatting for map types

#### **HeaderEditor.cs** (`/DS_Map/Editors/HeaderEditor.cs`)
- **Type**: Windows Forms UserControl
- **Purpose**: Edit map metadata and header properties

**Key Features**:
- 14+ metadata fields per map header
- Music selection (day/night tracks)
- Weather and camera angle configuration
- Script and event file references
- Copy/paste operations for bulk header transfer
- Search functionality by location name

---

## 2. Map Data Structures

### **MapFile.cs** - Core Map Data Container

**32x32 Tile Grid System**:
```csharp
public const byte mapSize = 32;  // Always 32x32 tiles
public byte[,] collisions = new byte[32, 32];  // Walkability
public byte[,] types = new byte[32, 32];       // Terrain types
```

**Data Members**:
```csharp
public List<Building> buildings;           // Buildings on map
public NSBMD mapModel;                     // 3D terrain mesh
public byte[] mapModelData;                // Raw NSBMD binary
public byte[] bdhc;                        // Terrain height data
public byte[] bgs;                         // Background sound (HGSS)
```

**Key Methods**:
```csharp
public void ImportPermissions(byte[] data) // Parse collision/type grid
public void ImportBuildings(byte[] data)   // Parse building list
public bool LoadMapModel(byte[] data)      // Load 3D terrain model
public byte[] ToByteArray()                // Complete serialization
public byte[] CollisionsToByteArray()      // Export collision grid
public byte[] BuildingsToByteArray()       // Export building list
```

### **Building.cs** - Individual Building

**48-Byte Structure**:
```csharp
public uint modelID;                // Which 3D model
public short xPosition, yPosition, zPosition;  // Base coordinates
public ushort xFraction, yFraction, zFraction;  // 1/65536th parts
public ushort xRotation, yRotation, zRotation;  // Rotation angles
public uint width, height, length;  // Scale factors (usually 16)
public NSBMD NSBMDFile;            // Loaded 3D model
```

**Full Coordinate Calculation**:
```csharp
float fullX = building.xPosition + building.xFraction / 65536f;
float fullY = building.yPosition + building.yFraction / 65536f;
float fullZ = building.zPosition + building.zFraction / 65536f;
```

**Rotation Conversion**:
```csharp
// Between degrees and ushort format
public static ushort DegToU16(float deg) => (ushort)(deg * 65536 / 360);
public static float U16ToDeg(ushort u16) => (float)u16 * 360 / 65536;
```

### **MapHeader.cs** - Map Metadata

**14 Core Properties**:
```csharp
public byte areaDataID;            // Area classification
public byte cameraAngleID;         // Camera preset
public ushort eventFileID;         // Event script reference
public ushort levelScriptID;       // Level script reference
public ushort matrixID;            // World matrix ID
public ushort scriptFileID;        // Script file ID
public ushort musicDayID;          // Day music track
public ushort musicNightID;        // Night music track
public byte locationSpecifier;     // Map name index
public byte battleBackground;      // Battle arena ID
public ushort textArchiveID;       // Dialogue reference
public byte weatherID;             // Weather condition
public byte flags;                 // Bitwise permission flags
public ushort wildPokemon;         // Encounter file ID
```

**Game-Specific Subclasses**:
- `HeaderDP` - Diamond/Pearl
- `HeaderPt` - Platinum (adds `timeId`)
- `HeaderHGSS` - HeartGold/SoulSilver (adds `followMode`, world coords)

### **GameMatrix.cs** - World Spatial Grid

**Structure** (variable size):
```csharp
public byte width;                 // Grid columns
public byte height;                // Grid rows
public string name;                // Matrix name
public bool hasHeadersSection;     // Optional
public bool hasHeightsSection;     // Optional
public ushort[,] headers;          // Map header IDs per cell
public byte[,] altitudes;          // Height per cell
public ushort[,] maps;             // Map file IDs per cell
public static readonly ushort EMPTY = 65535;  // Empty marker
```

**Key Operations**:
```csharp
public void ResizeMatrix(int newHeight, int newWidth)
// Resizes grid, fills new cells with EMPTY (65535)

public override byte[] ToByteArray()
// Complete serialization with optional sections
```

---

## 3. Map Rendering System

### **NSBMDGlRenderer.cs** - OpenGL 3D Renderer

**Rendering Foundation**:
- Uses **Tao.OpenGL** for OpenGL binding
- Imports OpenTK and HelixToolkit for 3D math
- Adapted from kiwi.ds' NSBMD Model Viewer

**Render Modes**:
```csharp
public enum RenderMode {
    Opaque = 1,       // Solid
    Translucent,      // Semi-transparent
    Picking           // Selection
}
```

**Matrix Stack**:
```csharp
private static MTX44[] MatrixStack = new MTX44[31];
private const float SCALE_IV = 4096.0f;  // Fixed-point scale
```

**Features**:
- Multiple texture formats (A3I5, 4-Color, 16-Color, 256-Color)
- Material animation (NSBTA, NSBCA, NSBTP)
- Alpha blending and transparency
- 4 configurable light sources

### **MapEditor Rendering Pipeline**

**Setup**:
```csharp
private void SetupRenderer(float ang, float dist, float elev, 
                          float perspective, int width, int height)
{
    Gl.glEnable(Gl.GL_DEPTH_TEST);
    Gl.glEnable(Gl.GL_BLEND);
    Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
    
    Gl.glMatrixMode(Gl.GL_PROJECTION);
    Gl.glLoadIdentity();
    Glu.gluPerspective(perspective, aspect, 0.2f, 500.0f);
    Gl.glTranslatef(0, 0, -dist);
    Gl.glRotatef(elev, 1, 0, 0);    // Elevation
    Gl.glRotatef(ang, 0, 1, 0);     // Rotation
}
```

**Building Transformation**:
```csharp
private void ScaleTranslateRotateBuilding(Building building)
{
    float fullX = building.xPosition + building.xFraction / 65536f;
    float fullY = building.yPosition + building.yFraction / 65536f;
    float fullZ = building.zPosition + building.zFraction / 65536f;
    
    float scaleFactor = building.NSBMDFile.models[0].modelScale / 1024;
    
    Gl.glScalef(scaleFactor * building.width,
                scaleFactor * building.height,
                scaleFactor * building.length);
    Gl.glTranslatef(fullX * translateFactor / building.width,
                    fullY * translateFactor / building.height,
                    fullZ * translateFactor / building.length);
    Gl.glRotatef(Building.U16ToDeg(building.xRotation), 1, 0, 0);
    Gl.glRotatef(Building.U16ToDeg(building.yRotation), 0, 1, 0);
    Gl.glRotatef(Building.U16ToDeg(building.zRotation), 0, 0, 1);
}
```

**Camera Control Constants**:
```csharp
public const int mapEditorSquareSize = 19;  // Pixel size for collision tiles
public static float ang = 0.0f;             // Rotation angle
public static float dist = 12.8f;           // Distance from origin
public static float elev = 50.0f;           // Elevation angle
public float perspective = 45f;             // FOV in degrees
```

---

## 4. Map Editing Capabilities

### **Collision & Permission Editing**

**Grid Structure**:
```csharp
public byte[,] collisions = new byte[32, 32];  // 0x00=walkable, 0x80=blocked
public byte[,] types = new byte[32, 32];       // Terrain type (grass, water, etc)
```

**Painting System**:
```csharp
public Pen paintPen;
public SolidBrush paintBrush;
public byte paintByte;              // Currently selected value
public int Transparency = 128;       // For visual feedback
public Rectangle painterBox;        // Preview display
```

### **Building Management**

**UI Operations**:
- Add new building (with default values)
- Duplicate selected building
- Remove building
- Edit position (X/Y/Z with fractional parts)
- Edit rotation (X/Y/Z in degrees)
- Edit scale (width/height/length)
- Edit model ID
- Real-time preview in 3D view

### **Matrix Editing**

**DataGridView Integration**:
```csharp
// Three synchronized grids
DataGridView headersGridView;      // ushort values
DataGridView heightsGridView;      // byte values
DataGridView mapFilesGridView;     // ushort values

// Cell editing
private void mapFilesGridView_CellValueChanged(...)
{
    if (ushort.TryParse(cell.Value.ToString(), out ushort val))
    {
        currentMatrix.maps[row, col] = val;
    }
}
```

**Resizing**:
```csharp
private void widthUpDown_ValueChanged(object sender, EventArgs e)
{
    int newWidth = (int)widthUpDown.Value;
    currentMatrix.ResizeMatrix(currentMatrix.height, newWidth);
    // Regenerate UI tables
}
```

### **Map Import/Export**

**Import Flow**:
```csharp
OpenFileDialog of = new OpenFileDialog { Filter = "Map BIN File (*.bin)|*.bin" };
MapFile temp = new MapFile(of.FileName, RomInfo.gameFamily);
if (temp.correctnessFlag)
{
    currentMapFile = temp;
    currentMapFile.SaveToFileDefaultDir(selectedIndex);
    // Refresh UI
}
```

**Export Options**:
- Standard .bin format
- With/without textures
- DAE model export (via Apicula)

---

## 5. Integration Architecture

### **Main Application Structure**

**Tab-Based System** (MainProgram.cs):
```
- Header Editor
- Matrix Editor  
- Map Editor
- NSBTX Editor
- Event Editor
- And 10+ more...
```

**Initialization Pattern**:
```csharp
public void SetupMapEditor(MainProgram parent, bool force=false)
{
    // Initialize OpenGL context
    mapOpenGlControl.InitializeContexts();
    mapOpenGlControl.MakeCurrent();
    
    // Unpack required archives
    DSUtils.TryUnpackNarcs(new List<DirNames> { 
        DirNames.maps,
        DirNames.exteriorBuildingModels,
        DirNames.buildingConfigFiles,
        DirNames.buildingTextures,
        DirNames.mapTextures,
    });
    
    // Populate UI dropdowns
    int mapCount = _parent.romInfo.GetMapCount();
    for (int i = 0; i < mapCount; i++)
    {
        selectMapComboBox.Items.Add($"{i:D3} - {GetMapName(i)}");
    }
    
    // Load texture list
    for (int i = 0; i < _parent.romInfo.GetMapTexturesCount(); i++)
    {
        mapTextureComboBox.Items.Add($"Map Texture Pack [{i:D2}]");
    }
    
    mapEditorIsReady = true;
}
```

### **Key Utilities**

**Helpers.cs**:
```csharp
public static void RenderMap(ref NSBMDGlRenderer mapRenderer,
                             ref NSBMDGlRenderer buildingsRenderer,
                             ref MapFile currentMapFile,
                             float ang, float dist, float elev,
                             float perspective, int width, int height,
                             bool mapTexturesOn, bool bldTexturesOn)

public static void DisableHandlers()    // Prevent cascading events
public static void EnableHandlers()
public static void statusLabelMessage(string msg = "")  // Status updates
```

**OffsetPictureBox.cs** - Pan/Zoom Control:
```csharp
public float offsX, offsY;          // Current offset
public void DrawAt(float x, float y)
public void DrawTranslate(float x, float y)
public void RedrawCentered()
```

---

## 6. Code Patterns for Clockwork Integration

### **1. Game Version Abstraction**

```csharp
switch (RomInfo.gameFamily)
{
    case GameFamilies.DP:
    case GameFamilies.Plat:
        // Diamond/Pearl/Platinum specific
        reader.BaseStream.Position = 0x10 + offset1 + offset2;
        break;
    default:
        // HeartGold/SoulSilver
        reader.BaseStream.Position = 0x12 + bgsSize + offset1 + offset2;
        break;
}
```

### **2. Event Handler Enable/Disable Pattern**

```csharp
// Before bulk update
Helpers.DisableHandlers();

for (int i = 0; i < 1000; i++)
{
    dataGridView.Rows[i].Cells[0].Value = newValue;  // No events
}

// After bulk update
Helpers.EnableHandlers();
```

### **3. Resource Caching Pattern**

```csharp
// Load once, reuse many times
if (!resourceCache.ContainsKey(resourceId))
{
    using (Stream fs = new FileStream(path, FileMode.Open))
    {
        resourceCache[resourceId] = NSBMDLoader.LoadNSBMD(fs);
    }
}

NSBMDGlRenderer renderer = new NSBMDGlRenderer();
renderer.Model = resourceCache[resourceId];
```

### **4. Binary I/O Pattern**

```csharp
// Parsing
public MapFile(Stream data, GameFamilies gFamily)
{
    using (BinaryReader reader = new BinaryReader(data))
    {
        int permLength = reader.ReadInt32();
        int bldLength = reader.ReadInt32();
        
        ImportPermissions(reader.ReadBytes(permLength));
        ImportBuildings(reader.ReadBytes(bldLength));
    }
}

// Serialization
public override byte[] ToByteArray()
{
    using (BinaryWriter writer = new BinaryWriter(new MemoryStream()))
    {
        writer.Write(permLength);
        writer.Write(bldLength);
        writer.Write(CollisionsToByteArray());
        writer.Write(BuildingsToByteArray());
        return writer.ToByteArray();
    }
}
```

### **5. OpenGL Rendering Pattern**

```csharp
private void RenderScene()
{
    glControl.MakeCurrent();
    
    // Setup
    SetupRenderer(angle, distance, elevation, perspective, width, height);
    
    // Render terrain
    mapRenderer.RenderModel(mapModel, ...);
    
    // Render buildings
    foreach (Building b in buildings)
    {
        Gl.glPushMatrix();
        ScaleTranslateRotateBuilding(b);
        buildingRenderer.RenderModel(b.NSBMDFile, ...);
        Gl.glPopMatrix();
    }
    
    // Display
    glControl.SwapBuffers();
}
```

### **6. Camera Control Pattern**

```csharp
private float angle = 0.0f;
private float distance = 12.8f;
private float elevation = 50.0f;

private void glControl_MouseDown(object sender, MouseEventArgs e)
{
    if (e.Button == MouseButtons.Left)
        rotating = true;
}

private void glControl_MouseMove(object sender, MouseEventArgs e)
{
    if (rotating)
    {
        angle += (e.X - lastX) * 0.5f;
        elevation += (e.Y - lastY) * 0.5f;
        lastX = e.X;
        lastY = e.Y;
        Refresh();
    }
}

private void glControl_MouseWheel(object sender, MouseEventArgs e)
{
    distance += e.Delta / 120.0f;
    Refresh();
}
```

---

## Summary: Essential Implementation Points

**Core Data Models**:
1. **MapFile** - 32x32 tile grid + buildings + 3D model
2. **Building** - Position (X/Y/Z + fractions), rotation, scale, model ID
3. **MapHeader** - 14+ metadata fields, game-version specific
4. **GameMatrix** - Variable-size grid of map references

**Rendering Pipeline**:
1. OpenGL with Tao.OpenGL binding
2. NSBMDGlRenderer for 3D models
3. Proper lighting (4 lights), alpha blending, texture support
4. Camera control via angle, distance, elevation

**UI Components**:
1. MapEditor - Main 3D view + collision painting
2. MatrixEditor - 3 DataGridView tables for grid editing
3. HeaderEditor - Metadata form editor
4. Main tabbed interface coordinating all editors

**Key Patterns**:
1. Separate data (ROMFiles), UI (Editors), rendering (LibNDSFormats)
2. Game version abstraction throughout
3. Handler enable/disable for event management
4. Resource caching for performance
5. Binary I/O with BinaryReader/Writer

