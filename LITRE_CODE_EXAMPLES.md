# LiTRE Implementation Code Examples

This document provides actual code snippets from the LiTRE repository that can serve as reference implementations for Clockwork.

## File Paths Reference

All files are located in: https://github.com/corentinmace/LiTRE

- **MapFile.cs** → `/DS_Map/ROMFiles/MapFile.cs` (391 lines)
- **Building.cs** → `/DS_Map/ROMFiles/MapFile.cs` (classes at lines 264-390)
- **MapHeader.cs** → `/DS_Map/ROMFiles/MapHeader.cs` (200+ lines)
- **GameMatrix.cs** → `/DS_Map/ROMFiles/GameMatrix.cs` (224 lines)
- **MapEditor.cs** → `/DS_Map/Editors/MapEditor.cs` (400+ lines)
- **MatrixEditor.cs** → `/DS_Map/Editors/MatrixEditor.cs` (300+ lines)
- **NSBMDGlRenderer.cs** → `/DS_Map/LibNDSFormats/NSBMD/NSBMDGlRenderer.cs` (47,458 tokens - very large)
- **GameCamera.cs** → `/DS_Map/GameCamera.cs`
- **OffsetPictureBox.cs** → `/DS_Map/OffsetPictureBox.cs`

---

## 1. MapFile Data Structure (Complete)

```csharp
namespace LiTRE.ROMFiles {
    /// <summary>
    /// Class to store map data in Pokémon NDS games
    /// </summary>
    public class MapFile : RomFile {
        #region Fields

        public static readonly string NSBMDFilter = "NSBMD File (*.nsbmd)|*.nsbmd";
        public static readonly string TexturedNSBMDFilter = "Textured" + NSBMDFilter;
        public static readonly string UntexturedNSBMDFilter = "Untextured" + NSBMDFilter;

        public static readonly string MovepermsFilter = "Permissions File (*.per)|*.per";
        public static readonly string BuildingsFilter = "Buildings File (*.bld)|*.bld";
        public static readonly string BDHCFilter = "Terrain File (*.bdhc)|*.bdhc";
        public static readonly string BDHCamFilter = "Terrain File (*.bdhc, *.bdhcam)|*.bdhc;*.bdhcam";
        public static readonly string BGSFilter = "BackGround Sound File (*.bgs)|*.bgs";
        
        public bool correctnessFlag = true;
        public static readonly byte mapSize = 32;
        public static readonly byte buildingHeaderSize = 48;
        public static readonly byte[] blankBGS = new byte[] { 0x34, 0x12, 0x00, 0x00 };

        public List<Building> buildings;
        public NSBMD mapModel;
        public byte[,] collisions = new byte[mapSize, mapSize];
        public byte[,] types = new byte[mapSize, mapSize];
        public byte[] mapModelData;
        public byte[] bdhc;
        public byte[] bgs = blankBGS;
        #endregion

        #region Constructors
        public MapFile(string path, GameFamilies gFamily, bool discardMoveperms = false, bool showMessages = true) 
            : this(new FileStream(path, FileMode.Open), gFamily, discardMoveperms, showMessages) {}
        
        public MapFile(int mapNumber, GameFamilies gFamily, bool discardMoveperms = false, bool showMessages = true) 
            : this(RomInfo.gameDirs[DirNames.maps].unpackedDir + "\\" + mapNumber.ToString("D4"), gFamily, discardMoveperms, showMessages) { }
        
        public MapFile(Stream data, GameFamilies gFamily, bool discardMoveperms = false, bool showMessages = true) {
            using (BinaryReader reader = new BinaryReader(data)) {
                /* Read sections lengths */
                int permissionsSectionLength = reader.ReadInt32();
                int buildingsSectionLength = reader.ReadInt32();
                int nsbmdSectionLength = reader.ReadInt32();
                int bdhcSectionLength = reader.ReadInt32();

                /* Read background sounds section (HGSS only) */
                if (gFamily == GameFamilies.HGSS) {
                    ushort bgsSignature = reader.ReadUInt16();
                    if (bgsSignature == 0x1234) {
                        ushort bgsDataLength = reader.ReadUInt16();
                        reader.BaseStream.Position -= 4;
                        ImportSoundPlates(reader.ReadBytes(bgsDataLength + 4));
                    } else {
                        correctnessFlag = false;
                        if (showMessages) {
                            MessageBox.Show("The header section of this map's BackGround Sound data is corrupted.",
                            "BGS Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }

                /* Read permission data */
                if (discardMoveperms) {
                    reader.BaseStream.Position += permissionsSectionLength;
                } else {
                    ImportPermissions(reader.ReadBytes(permissionsSectionLength));
                }

                /* Read buildings data */
                ImportBuildings(reader.ReadBytes(buildingsSectionLength));

                /* Read nsbmd model */
                if (!LoadMapModel(reader.ReadBytes(nsbmdSectionLength), showMessages)) {
                    correctnessFlag = false;
                    mapModel = null;
                };

                /* Read bdhc data */
                ImportTerrain(reader.ReadBytes(bdhcSectionLength));
            }
        }
        #endregion

        #region Methods
        public byte[] BuildingsToByteArray() {
            MemoryStream newData = new MemoryStream(buildingHeaderSize * buildings.Count);
            using (BinaryWriter writer = new BinaryWriter(newData)) {
                for (int i = 0; i < buildings.Count; i++) {
                    writer.Write(buildings[i].modelID);
                    writer.Write(buildings[i].xFraction);
                    writer.Write(buildings[i].xPosition);
                    writer.Write(buildings[i].yFraction);
                    writer.Write(buildings[i].yPosition);
                    writer.Write(buildings[i].zFraction);
                    writer.Write(buildings[i].zPosition);

                    writer.Write((int)buildings[i].xRotation);
                    writer.Write((int)buildings[i].yRotation);
                    writer.Write((int)buildings[i].zRotation);

                    writer.BaseStream.Position += 1;

                    writer.Write(buildings[i].width);
                    writer.Write(buildings[i].height);
                    writer.Write(buildings[i].length);

                    writer.Write(new byte[0x7]);
                }
            }
            return newData.ToArray();
        }

        public byte[] CollisionsToByteArray() {
            MemoryStream newData = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(newData)) {
                for (int i = 0; i < mapSize; i++) {
                    for (int j = 0; j < mapSize; j++) {
                        writer.Write(types[i, j]);
                        writer.Write(collisions[i, j]);
                    }
                }
            }
            return newData.ToArray();
        }

        public void ImportBuildings(byte[] newData) {
            buildings = new List<Building>();
            using (BinaryReader reader = new BinaryReader(new MemoryStream(newData))) {
                for (int i = 0; i < newData.Length / buildingHeaderSize; i++) {
                    buildings.Add(new Building(new MemoryStream(reader.ReadBytes(buildingHeaderSize))));
                }
            }
        }

        public bool LoadMapModel(byte[] newData, bool showMessages = true) {
            using (BinaryReader modelReader = new BinaryReader(new MemoryStream(newData))) {

                if (modelReader.ReadUInt32() != NSBMD.NDS_TYPE_BMD0) {
                    if (showMessages) {
                        MessageBox.Show("Please select an NSBMD file.", "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    return false;
                }

                modelReader.BaseStream.Position = 0xE;
                if (modelReader.ReadInt16() > 1) {
                    mapModelData = NSBUtils.GetModelWithoutTextures(newData);
                } else {
                    modelReader.BaseStream.Position = 0x0;
                    mapModelData = modelReader.ReadBytes((int)modelReader.BaseStream.Length);
                }

                mapModel = NSBMDLoader.LoadNSBMD(new MemoryStream(mapModelData));
            }
            return true;
        }

        public void ImportPermissions(byte[] newData) {
            using (BinaryReader reader = new BinaryReader(new MemoryStream(newData))) {
                for (int i = 0; i < 32; i++) {
                    for (int j = 0; j < 32; j++) {
                        types[i, j] = reader.ReadByte();
                        collisions[i, j] = reader.ReadByte();
                    }
                }
            }
        }

        public void ImportSoundPlates(byte[] newData) {
            using (BinaryReader reader = new BinaryReader(new MemoryStream(newData))) {
                bgs = reader.ReadBytes((int)newData.Length);
            }
        }

        public void ImportTerrain(byte[] newData) {
            using (BinaryReader reader = new BinaryReader(new MemoryStream(newData))) {
                bdhc = reader.ReadBytes((int)newData.Length);
            }
        }

        public override byte[] ToByteArray() {
            MemoryStream newData = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(newData)) {
                /* Write section lengths */
                writer.Write(collisions.Length + types.Length);
                writer.Write(buildings.Count * buildingHeaderSize);
                writer.Write(mapModelData.Length);
                writer.Write(bdhc.Length);

                /* Write soundplate section for HG/SS */
                if (RomInfo.gameFamily == GameFamilies.HGSS) {
                    writer.Write(bgs);
                }

                /* Write sections */
                writer.Write(CollisionsToByteArray());
                writer.Write(BuildingsToByteArray());
                writer.Write(mapModelData);
                writer.Write(bdhc);
            }
            return newData.ToArray();
        }

        public SortedSet<byte> GetUsedTypes() {
            SortedSet<byte> sortedBytes = new SortedSet<byte>();
            for (int i = 0; i < mapSize; i++) {
                for (int j = 0; j < mapSize; j++) {
                    sortedBytes.Add(types[i, j]);
                }
            }
            return sortedBytes;
        }

        public void SaveToFileDefaultDir(int IDtoReplace, bool showSuccessMessage = true) {
            SaveToFileDefaultDir(DirNames.maps, IDtoReplace, showSuccessMessage);
        }

        public void SaveToFileExplorePath(string suggestedFileName, bool showSuccessMessage = true) {
            SaveToFileExplorePath("Gen IV Map Bin", "bin", suggestedFileName, showSuccessMessage);
        }
        #endregion
    }

    /// <summary>
    /// Class to store building data from Pokémon NDS games
    /// </summary>
    public class Building {
        #region Fields
        public NSBMD NSBMDFile;
        public uint modelID { get; set; }
        public short xPosition { get; set; }
        public short yPosition { get; set; }
        public short zPosition { get; set; }
        public ushort xFraction { get; set; }
        public ushort yFraction { get; set; }
        public ushort zFraction { get; set; }
        public ushort xRotation { get; set; }
        public ushort yRotation { get; set; }
        public ushort zRotation { get; set; }
        public uint width { get; set; }
        public uint height { get; set; }
        public uint length { get; set; }
        #endregion Fields

        #region Constructors
        public Building(Stream data) {
            using (BinaryReader reader = new BinaryReader(data)) {
                modelID = reader.ReadUInt32();

                xFraction = reader.ReadUInt16();
                xPosition = reader.ReadInt16();
                yFraction = reader.ReadUInt16();
                yPosition = reader.ReadInt16();
                zFraction = reader.ReadUInt16();
                zPosition = reader.ReadInt16();
                
                xRotation = reader.ReadUInt16();
                reader.BaseStream.Position += 0x2;
                yRotation = reader.ReadUInt16();
                reader.BaseStream.Position += 0x2;
                zRotation = reader.ReadUInt16();
                reader.BaseStream.Position += 0x2;

                reader.BaseStream.Position += 0x1;

                width = reader.ReadUInt16();
                reader.BaseStream.Position += 0x2;
                height = reader.ReadUInt16();
                reader.BaseStream.Position += 0x2;
                length = reader.ReadUInt16();
            }
        }

        public Building() {
            modelID = 0;
            xFraction = 0;
            xPosition = 0;
            yFraction = 0;
            yPosition = 1;
            zFraction = 0;
            zPosition = 0;

            xRotation = yRotation = zRotation = 0;
            width = 16;
            height = 16;
            length = 16;
        }

        public Building(Building toCopy) {
            modelID = toCopy.modelID;
            xFraction = toCopy.xFraction;
            xPosition = toCopy.xPosition;
            yFraction = toCopy.yFraction;
            yPosition = (short)(toCopy.yPosition + 1);
            zFraction = toCopy.zFraction;
            zPosition = toCopy.zPosition;

            xRotation = toCopy.xRotation;
            yRotation = toCopy.yRotation;
            zRotation = toCopy.zRotation;

            width = toCopy.width;
            height = toCopy.height;
            length = toCopy.length;
        }
        #endregion Constructors

        public static ushort DegToU16(float deg) {
            return (ushort)(deg * 65536 / 360);
        }

        public static float U16ToDeg(ushort u16) {
            return (float)u16 * 360 / 65536;
        }

        public void LoadModelData(string dir) {
            LoadModelDataFromID((int)modelID, dir);
        }

        public void LoadModelData(bool interior) {
            string modelPath = Filesystem.GetBuildingModelPath(interior, (int)modelID);

            if (string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath)) {
                MessageBox.Show("Building " + modelID + " could not be found in\n" + '"' + Path.GetDirectoryName(modelPath) + '"', 
                    "Building not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try {
                using (Stream fs = new FileStream(modelPath, FileMode.Open)) {
                    this.NSBMDFile = NSBMDLoader.LoadNSBMD(fs);
                }
            } catch (FileNotFoundException) {
                MessageBox.Show("Building " + modelID + " could not be found in\n" + '"' + Path.GetDirectoryName(modelPath) + '"',
                    "Building not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void LoadModelDataFromID(int modelID, string bmDir) {
            string modelPath = bmDir + "\\" + modelID.ToString("D4");

            if (string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath)) {
                MessageBox.Show("Building " + modelID + " could not be found in\n" + '"' + bmDir + '"',
                    "Building not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try {
                using (Stream fs = new FileStream(modelPath, FileMode.Open)) {
                    this.NSBMDFile = NSBMDLoader.LoadNSBMD(fs);
                }
            } catch (FileNotFoundException) {
                MessageBox.Show("Building " + modelID + " could not be found in\n" + '"' + bmDir + '"',
                    "Building not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
```

---

## 2. GameMatrix Structure (Complete)

```csharp
namespace LiTRE.ROMFiles {
    /// <summary>
    /// Class to store map matrix data from Pokémon NDS games
    /// </summary>
    public class GameMatrix : RomFile {
        #region Fields
        public static readonly string DefaultFilter = "Game Matrix File (*.mtx)|*.mtx";

        public bool hasHeadersSection { get; set; }
        public bool hasHeightsSection { get; set; }
        public string name { get; set; }
        public byte width { get; set; }
        public byte height { get; set; }
        public int? id { get; } = null;

        public ushort[,] headers;
        public byte[,] altitudes;
        public ushort[,] maps;

        public static readonly ushort EMPTY = 65535;
        #endregion Fields

        #region Constructors
        public GameMatrix(Stream data) {
            using (BinaryReader reader = new BinaryReader(data)) {
                /* Read matrix size and sections included */
                width = reader.ReadByte();
                height = reader.ReadByte();

                if (reader.ReadBoolean()) {
                    hasHeadersSection = true;
                }

                if (reader.ReadBoolean()) {
                    hasHeightsSection = true;
                }

                /* Read matrix's name */
                byte nameLength = reader.ReadByte();
                name = Encoding.UTF8.GetString(reader.ReadBytes(nameLength));

                /* Initialize section arrays */
                headers = new ushort[height, width];
                altitudes = new byte[height, width];
                maps = new ushort[height, width];

                /* Read sections */
                if (hasHeadersSection) {
                    for (int i = 0; i < height; i++) {
                        for (int j = 0; j < width; j++) {
                            headers[i, j] = reader.ReadUInt16();
                        }
                    }
                }

                if (hasHeightsSection) {
                    for (int i = 0; i < height; i++) {
                        for (int j = 0; j < width; j++) {
                            altitudes[i, j] = reader.ReadByte();
                        }
                    }
                }

                for (int i = 0; i < height; i++) {
                    for (int j = 0; j < width; j++) {
                        maps[i, j] = reader.ReadUInt16();
                    }
                }
            }
        }

        public GameMatrix(int ID) : this(new FileStream(RomInfo.gameDirs[DirNames.matrices].unpackedDir + "\\" + ID.ToString("D4"), FileMode.Open)) {
            this.id = ID;
        }

        public GameMatrix(GameMatrix copy, int newID) { 
            this.id = newID;
            this.name = copy.name;
            this.width = copy.width;
            this.height = copy.height;

            this.hasHeadersSection = copy.hasHeadersSection;
            this.hasHeightsSection = copy.hasHeightsSection;

            this.maps = (ushort[,])copy.maps.Clone();
            this.altitudes = (byte[,])copy.altitudes.Clone();
            this.headers = (ushort[,])copy.headers.Clone();
        }

        public GameMatrix() {
            this.name = "newMatrix";
            this.hasHeadersSection = false;
            this.hasHeightsSection = false;

            this.width = 1;
            this.height = 1;
            this.maps = new ushort[1, 1] { { 0 } };
        }
        #endregion

        #region Methods
        public void ResizeMatrix(int newHeight, int newWidth) {
            ushort[,] newHeaders = new ushort[newHeight, newWidth];
            byte[,] newAltitudes = new byte[newHeight, newWidth];
            ushort[,] newMaps = new ushort[newHeight, newWidth];

            // Copy existing data
            if (hasHeadersSection) {
                for (int i = 0; i < Math.Min(height, newHeight); i++) {
                    for (int j = 0; j < Math.Min(width, newWidth); j++) {
                        newHeaders[i, j] = headers[i, j];
                    }
                }
            }

            if (hasHeightsSection) {
                for (int i = 0; i < Math.Min(height, newHeight); i++) {
                    for (int j = 0; j < Math.Min(width, newWidth); j++) {
                        newAltitudes[i, j] = altitudes[i, j];
                    }
                }
            }

            // Copy maps with padding
            for (int i = 0; i < Math.Min(height, newHeight); i++) {
                for (int j = 0; j < Math.Min(width, newWidth); j++) {
                    newMaps[i, j] = maps[i, j];
                }
            }

            // Fill new cells with EMPTY
            if (newHeight > height) {
                for (int i = height; i < newHeight; i++) {
                    for (int j = 0; j < newWidth; j++) {
                        newMaps[i, j] = GameMatrix.EMPTY;
                    }
                }
            }

            if (newWidth > width) {
                for (int j = width; j < newWidth; j++) {
                    for (int i = 0; i < newHeight; i++) {
                        newMaps[i, j] = GameMatrix.EMPTY;
                    }
                }
            }

            // Replace old arrays
            headers = newHeaders;
            altitudes = newAltitudes;
            maps = newMaps;
            height = (byte)newHeight;
            width = (byte)newWidth;
        }

        public override string ToString() {
            return (this.id == null ? "" : id.ToString()) + ": " + this.name;
        }

        public override byte[] ToByteArray() {
            MemoryStream newData = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(newData)) {
                writer.Write(width);
                writer.Write(height);
                writer.Write(hasHeadersSection);
                writer.Write(hasHeightsSection);
                writer.Write(name);

                if (hasHeadersSection) {
                    for (int i = 0; i < height; i++) {
                        for (int j = 0; j < width; j++) {
                            writer.Write(headers[i, j]);
                        }
                    }
                }

                if (hasHeightsSection) {
                    for (int i = 0; i < height; i++) {
                        for (int j = 0; j < width; j++) {
                            writer.Write(altitudes[i, j]);
                        }
                    }
                }

                for (int i = 0; i < height; i++) {
                    for (int j = 0; j < width; j++) {
                        writer.Write(maps[i, j]);
                    }
                }
            }
            return newData.ToArray();
        }

        public void SaveToFileDefaultDir(int IDtoReplace, bool showSuccessMessage = true) {
            SaveToFileDefaultDir(DirNames.matrices, IDtoReplace, showSuccessMessage);
        }

        public void SaveToFileExplorePath(string suggestedFileName, bool showSuccessMessage = true) {
            SaveToFileExplorePath("Gen IV Matrix File", "mtx", suggestedFileName, showSuccessMessage);
        }
        #endregion
    }
}
```

---

## 3. MapEditor Initialization (Excerpt)

```csharp
public void SetupMapEditor(MainProgram parent, bool force=false)
{
    mapOpenGlControl.InitializeContexts();
    mapOpenGlControl.MakeCurrent();
    mapOpenGlControl.MouseWheel += new MouseEventHandler(mapOpenGlControl_MouseWheel);

    if (mapEditorIsReady && !force) { return; }
    mapEditorIsReady = true;
    this._parent = parent;

    if (selectMapComboBox.SelectedIndex > -1)
        Helpers.RenderMap(ref mapRenderer, ref buildingsRenderer, ref currentMapFile, 
                          ang, dist, elev, perspective, mapOpenGlControl.Width, mapOpenGlControl.Height, 
                          mapTexturesOn, bldTexturesOn);

    /* Extract essential NARCs */
    _parent.toolStripProgressBar.Visible = true;
    _parent.toolStripProgressBar.Maximum = 9;
    _parent.toolStripProgressBar.Value = 0;
    Helpers.statusLabelMessage("Attempting to unpack Map Editor NARCs... Please wait.");
    Update();

    DSUtils.TryUnpackNarcs(new List<DirNames> { 
        DirNames.maps,
        DirNames.exteriorBuildingModels,
        DirNames.buildingConfigFiles,
        DirNames.buildingTextures,
        DirNames.mapTextures,
        DirNames.areaData,
    });

    if (RomInfo.gameFamily == GameFamilies.HGSS) {
        DSUtils.TryUnpackNarcs(new List<DirNames> { DirNames.interiorBuildingModels });
    }

    Helpers.DisableHandlers();

    collisionPainterPictureBox.Image = new Bitmap(100, 100);
    typePainterPictureBox.Image = new Bitmap(100, 100);
    
    // Game-specific UI adjustments
    switch (RomInfo.gameFamily) {
        case GameFamilies.DP:
        case GameFamilies.Plat:
            mapPartsTabControl.TabPages.Remove(bgsTabPage);
            break;
        default:
            interiorbldRadioButton.Enabled = true;
            exteriorbldRadioButton.Enabled = true;
            break;
    }

    /* Add map names to combo box */
    selectMapComboBox.Items.Clear();
    int mapCount = _parent.romInfo.GetMapCount();

    for (int i = 0; i < mapCount; i++) {
        using (DSUtils.EasyReader reader = new DSUtils.EasyReader(
            RomInfo.gameDirs[DirNames.maps].unpackedDir + "\\" + i.ToString("D4"))) {
            
            switch (RomInfo.gameFamily) {
                case GameFamilies.DP:
                case GameFamilies.Plat:
                    reader.BaseStream.Position = 0x10 + reader.ReadUInt32() + reader.ReadUInt32();
                    break;
                default:
                    reader.BaseStream.Position = 0x12;
                    short bgsSize = reader.ReadInt16();
                    long backupPos = reader.BaseStream.Position;

                    reader.BaseStream.Position = 0;
                    reader.BaseStream.Position = backupPos + bgsSize + reader.ReadUInt32() + reader.ReadUInt32();
                    break;
            }

            reader.BaseStream.Position += 0x14;
            selectMapComboBox.Items.Add(i.ToString("D3") + MapHeader.nameSeparator + NSBUtils.ReadNSBMDname(reader));
        }
    }
    
    _parent.toolStripProgressBar.Value++;

    /* Fill building models list */
    updateBuildingListComboBox(false);

    /* Fill map textures list */
    mapTextureComboBox.Items.Clear();
    mapTextureComboBox.Items.Add("Untextured");
    for (int i = 0; i < _parent.romInfo.GetMapTexturesCount(); i++) {
        mapTextureComboBox.Items.Add("Map Texture Pack [" + i.ToString("D2") + "]");
    }
    _parent.toolStripProgressBar.Value++;

    /* Fill building textures list */
    buildTextureComboBox.Items.Clear();
    buildTextureComboBox.Items.Add("Untextured");
    for (int i = 0; i < _parent.romInfo.GetBuildingTexturesCount(); i++) {
        buildTextureComboBox.Items.Add("Building Texture Pack [" + i.ToString("D2") + "]");
    }

    _parent.toolStripProgressBar.Value++;

    collisionPainterComboBox.Items.Clear();
    foreach (string s in PokeDatabase.System.MapCollisionPainters.Values) {
        collisionPainterComboBox.Items.Add(s);
    }

    collisionTypePainterComboBox.Items.Clear();
    foreach (string s in PokeDatabase.System.MapCollisionTypePainters.Values) {
        collisionTypePainterComboBox.Items.Add(s);
    }

    _parent.toolStripProgressBar.Value++;

    /* Set controls' initial values */
    selectCollisionPanel.BackColor = Color.MidnightBlue;
    collisionTypePainterComboBox.SelectedIndex = 0;
    collisionPainterComboBox.SelectedIndex = 1;

    _parent.toolStripProgressBar.Value = 0;
    _parent.toolStripProgressBar.Visible = false;
    Helpers.EnableHandlers();

    //Default selections
    selectMapComboBox.SelectedIndex = 0;
    exteriorbldRadioButton.Checked = true;
    switch (RomInfo.gameFamily) {
        case GameFamilies.DP:
        case GameFamilies.Plat:
            mapTextureComboBox.SelectedIndex = 7;
            buildTextureComboBox.SelectedIndex = 1;
            break;
        case GameFamilies.HGSS:
            mapTextureComboBox.SelectedIndex = 3;
            buildTextureComboBox.SelectedIndex = 1;
            break;
        default:
            mapTextureComboBox.SelectedIndex = 2;
            buildTextureComboBox.SelectedIndex = 1;
            break;
    }

    Helpers.statusLabelMessage();
}
```

---

## 4. OpenGL Renderer Setup (MapEditor)

```csharp
private void SetupRenderer(float ang, float dist, float elev, float perspective, int width, int height) {
    //TODO: improve this
    Gl.glEnable(Gl.GL_RESCALE_NORMAL);
    Gl.glEnable(Gl.GL_COLOR_MATERIAL);
    Gl.glEnable(Gl.GL_DEPTH_TEST);
    Gl.glEnable(Gl.GL_NORMALIZE);
    Gl.glDisable(Gl.GL_CULL_FACE);
    Gl.glFrontFace(Gl.GL_CCW);
    Gl.glClearDepth(1);
    Gl.glEnable(Gl.GL_ALPHA_TEST);
    Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
    Gl.glEnable(Gl.GL_BLEND);
    Gl.glAlphaFunc(Gl.GL_GREATER, 0f);
    Gl.glClearColor(51f / 255f, 51f / 255f, 51f / 255f, 1f);
    
    float aspect;
    Gl.glViewport(0, 0, width, height);
    aspect = mapOpenGlControl.Width / mapOpenGlControl.Height;
    
    Gl.glMatrixMode(Gl.GL_PROJECTION);
    Gl.glLoadIdentity();
    Glu.gluPerspective(perspective, aspect, 0.2f, 500.0f);
    Gl.glTranslatef(0, 0, -dist);
    Gl.glRotatef(elev, 1, 0, 0);
    Gl.glRotatef(ang, 0, 1, 0);
    
    Gl.glMatrixMode(Gl.GL_MODELVIEW);
    Gl.glLoadIdentity();
    Gl.glTranslatef(0, 0, -dist);
    Gl.glRotatef(elev, 1, 0, 0);
    Gl.glRotatef(-ang, 0, 1, 0);
    
    // Setup lights
    Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_POSITION, new float[] { 1, 1, 1, 0 });
    Gl.glLightfv(Gl.GL_LIGHT1, Gl.GL_POSITION, new float[] { 1, 1, 1, 0 });
    Gl.glLightfv(Gl.GL_LIGHT2, Gl.GL_POSITION, new float[] { 1, 1, 1, 0 });
    Gl.glLightfv(Gl.GL_LIGHT3, Gl.GL_POSITION, new float[] { 1, 1, 1, 0 });
    
    Gl.glLoadIdentity();
    Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
    Gl.glColor3f(1.0f, 1.0f, 1.0f);
    Gl.glDepthMask(Gl.GL_TRUE);
    Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
}

private void ScaleTranslateRotateBuilding(Building building) {
    float fullXcoord = building.xPosition + building.xFraction / 65536f;
    float fullYcoord = building.yPosition + building.yFraction / 65536f;
    float fullZcoord = building.zPosition + building.zFraction / 65536f;

    float scaleFactor = building.NSBMDFile.models[0].modelScale / 1024;
    float translateFactor = 256 / building.NSBMDFile.models[0].modelScale;

    Gl.glScalef(scaleFactor * building.width, scaleFactor * building.height, scaleFactor * building.length);
    Gl.glTranslatef(fullXcoord * translateFactor / building.width, 
                    fullYcoord * translateFactor / building.height, 
                    fullZcoord * translateFactor / building.length);
    Gl.glRotatef(Building.U16ToDeg(building.xRotation), 1, 0, 0);
    Gl.glRotatef(Building.U16ToDeg(building.yRotation), 0, 1, 0);
    Gl.glRotatef(Building.U16ToDeg(building.zRotation), 0, 0, 1);
}
```

---

## 5. Texture Loading

```csharp
private void MW_LoadModelTextures(NSBMD model, string textureFolder, int fileID) {
    if (fileID < 0) {
        return;
    }
    string texturePath = textureFolder + "\\" + fileID.ToString("D4");
    model.materials = NSBTXLoader.LoadNsbtx(
        new MemoryStream(File.ReadAllBytes(texturePath)), 
        out model.Textures, 
        out model.Palettes);
    try {
        model.MatchTextures();
    } catch { }
}
```

---

## Key Takeaways

1. **MapFile** = 32x32 collision/type grids + buildings list + 3D model
2. **Building** = 48-byte structure with position (int+fraction), rotation, scale
3. **GameMatrix** = Variable-size grid of map references, optional headers/altitudes
4. **Rendering** = OpenGL with Tao.OpenGL, using NSBMDGlRenderer for 3D models
5. **Game Versions** = Separate header classes (HeaderDP, HeaderPt, HeaderHGSS)
6. **UI Pattern** = UserControl-based editors in tabbed main window
7. **Resource Management** = Archive unpacking, texture caching, handler enable/disable

