using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data
{
    /// <summary>
    /// Representation of SYSTEX.S in dat.bin
    /// </summary>
    public class SystemTextureFile : DataFile
    {
        internal int NumSections { get; set; }
        internal int EndPointersOffset { get; set; }
        internal int HeaderEndOffset { get; set; }
        /// <summary>
        /// Data file sections
        /// </summary>
        public List<DataFileSection> SectionOffsetsAndCounts { get; set; } = [];

        /// <summary>
        /// The list of system textures
        /// </summary>
        public List<SystemTexture> SystemTextures { get; set; } = [];
        /// <summary>
        /// A list of indices into the system texture list which determine the order in which certain system textures are loaded
        /// </summary>
        public List<short> LoadOrders { get; set; } = [];

        /// <inheritdoc/>
        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            Log = log;

            NumSections = IO.ReadInt(decompressedData, 0x00);
            if (NumSections != 2)
            {
                Log.LogError($"SYSTEX.S should only have 2 sections but {NumSections} were detected.");
                return;
            }
            EndPointersOffset = IO.ReadInt(decompressedData, 0x04);
            HeaderEndOffset = IO.ReadInt(decompressedData, 0x08);
            // There are only two sections
            SectionOffsetsAndCounts.Add(new() { Name = "SYSTEMTEXTURES", Offset = IO.ReadInt(decompressedData, 0x0C), ItemCount = IO.ReadInt(decompressedData, 0x10) });
            SectionOffsetsAndCounts.Add(new() { Name = "LOADORDER", Offset = IO.ReadInt(decompressedData, 0x14), ItemCount = IO.ReadInt(decompressedData, 0x18) });

            for (int i = 0; i < SectionOffsetsAndCounts[0].ItemCount; i++)
            {
                SystemTextures.Add(new(decompressedData[(SectionOffsetsAndCounts[0].Offset + 0x2C * i)..(SectionOffsetsAndCounts[0].Offset + 0x2C * (i + 1))]));
            }

            for (int i = 0; i < SectionOffsetsAndCounts[1].ItemCount; i++)
            {
                LoadOrders.Add(IO.ReadShort(decompressedData, SectionOffsetsAndCounts[1].Offset + 2 * i));
            }
        }

        /// <inheritdoc/>
        public override string GetSource(Dictionary<string, IncludeEntry[]> includes)
        {
            StringBuilder sb = new();

            sb.AppendLine(".include \"GRPBIN.INC\"");
            sb.AppendLine($".set {SysTexScreen.BOTTOM_SCREEN}, {(int)SysTexScreen.BOTTOM_SCREEN}");
            sb.AppendLine($".set {SysTexScreen.TOP_SCREEN}, {(int)SysTexScreen.TOP_SCREEN}");
            sb.AppendLine();

            sb.AppendLine($".word {NumSections}");
            sb.AppendLine($".word END_POINTERS");
            sb.AppendLine($".word FILE_START");
            foreach (DataFileSection section in SectionOffsetsAndCounts)
            {
                sb.AppendLine($".word {section.Name}");
                sb.AppendLine($".word {section.ItemCount}");
            }
            sb.AppendLine("FILE_START:");
            sb.AppendLine();

            sb.AppendLine($"{SectionOffsetsAndCounts[0].Name}:");
            for (int i = 0; i < SystemTextures.Count; i++)
            {
                SystemTextures[i].Name = $"{includes["GRPBIN"].FirstOrDefault(index => index.Value == SystemTextures[i].GrpIndex)?.Name ?? $"UNKNOWN{i:D2}"}_ENTRY_{i:D3}";
                sb.AppendLine($"{Helpers.Indent(3)}.set {SystemTextures[i].Name}, {i}");
                sb.AppendLine($"{Helpers.Indent(3)}.word {SystemTextures[i].Screen}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {includes["GRPBIN"].FirstOrDefault(index => index.Value == SystemTextures[i].GrpIndex)?.Name ?? $"{SystemTextures[i].GrpIndex}"} @ {SystemTextures[i].GrpIndex:X3}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].Tpage} @ {nameof(SystemTexture.Tpage)}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].PaletteNumber} @ {nameof(SystemTexture.PaletteNumber)}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].ValidateTex} @ {nameof(SystemTexture.ValidateTex)}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].LoadMethod} @ {nameof(SystemTexture.LoadMethod)}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].Unknown0E} @ {nameof(SystemTexture.Unknown0E)}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].MaxVram} @ {nameof(SystemTexture.MaxVram)}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].Unknown12} @ {nameof(SystemTexture.Unknown12)}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].Unknown14} @ {nameof(SystemTexture.Unknown14)}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].Unknown16} @ {nameof(SystemTexture.Unknown16)}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {includes["GRPBIN"].FirstOrDefault(index => index.Value == SystemTextures[i].AnimationIndex)?.Name ?? $"{SystemTextures[i].AnimationIndex}"} @ {nameof(SystemTexture.AnimationIndex)}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].TileWidth} @ {nameof(SystemTexture.TileWidth)}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].TileHeight} @ {nameof(SystemTexture.TileHeight)}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].Unknown1E} @ {nameof(SystemTexture.Unknown1E)}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].Unknown20} @ {nameof(SystemTexture.Unknown20)}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].Unknown22} @ {nameof(SystemTexture.Unknown22)}");
                sb.AppendLine($"{Helpers.Indent(3)}.word {SystemTextures[i].Unknown24} @ {nameof(SystemTexture.Unknown24)}");
                sb.AppendLine($"{Helpers.Indent(3)}.word {SystemTextures[i].Unknown28} @ {nameof(SystemTexture.Unknown28)}");
                sb.AppendLine();
            }

            sb.AppendLine($"{SectionOffsetsAndCounts[1].Name}:");
            for (int i = 0; i < SectionOffsetsAndCounts[1].ItemCount; i++)
            {
                sb.AppendLine($"LOAD_ENTRY_{i:X3}: .short {SystemTextures[LoadOrders[i]].Name}");
            }

            sb.AppendLine($"END_POINTERS:");
            sb.AppendLine();

            return sb.ToString();
        }
    }

    /// <summary>
    /// A representation of a system texture defined in SYSTEX.S
    /// </summary>
    public class SystemTexture(byte[] data)
    {
        /// <summary>
        /// The name of the system texture (defined for readability based on the textures)
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The screen for which the texture is optimized
        /// </summary>
        public SysTexScreen Screen { get; set; } = (SysTexScreen)IO.ReadInt(data, 0x00);
        /// <summary>
        /// The grp.bin index of the graphics file to use as the texture
        /// </summary>
        public short GrpIndex { get; set; } = IO.ReadShort(data, 0x04);
        /// <summary>
        /// The name of this parameter is known through debug strings found in the binary, but exactly what it does is not quite understood at this point
        /// </summary>
        public short Tpage { get; set; } = IO.ReadShort(data, 0x06);
        /// <summary>
        /// The index of the palette to use in 4bpp/16-color images (0-15)
        /// </summary>
        public short PaletteNumber { get; set; } = IO.ReadShort(data, 0x08);
        /// <summary>
        /// If false, the game will skip some of the validation routines while loading the texture
        /// </summary>
        public short ValidateTex { get; set; } = IO.ReadShort(data, 0x0A);
        /// <summary>
        /// The method by which the texture should be loaded (not well-understood at this point)
        /// </summary>
        public short LoadMethod { get; set; } = IO.ReadShort(data, 0x0C);
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown0E { get; set; } = IO.ReadShort(data, 0x0E);
        /// <summary>
        /// The name of this parameter is known through debug strings found in the binary, but exactly what it does is not quite understood at this point
        /// </summary>
        public ushort MaxVram { get; set; } = IO.ReadUShort(data, 0x10);
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown12 { get; set; } = IO.ReadShort(data, 0x12);
        /// <summary>
        /// Unknown
        /// </summary>
        public ushort Unknown14 { get; set; } = IO.ReadUShort(data, 0x14);
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown16 { get; set; } = IO.ReadShort(data, 0x16);
        /// <summary>
        /// The index of the animation used by the system texture
        /// </summary>
        public short AnimationIndex { get; set; } = IO.ReadShort(data, 0x18);
        /// <summary>
        /// If a texture optimized for the top screen, the width of the tiles (used by the OAM)
        /// </summary>
        public short TileWidth { get; set; } = IO.ReadShort(data, 0x1A);
        /// <summary>
        /// If a texture optimized for the top screen, the height of the tiles (used by the OAM)
        /// </summary>
        public short TileHeight { get; set; } = IO.ReadShort(data, 0x1C);
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown1E { get; set; } = IO.ReadShort(data, 0x1E);
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown20 { get; set; } = IO.ReadShort(data, 0x20);
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown22 { get; set; } = IO.ReadShort(data, 0x22);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown24 { get; set; } = IO.ReadInt(data, 0x24);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown28 { get; set; } = IO.ReadInt(data, 0x28);
    }

    /// <summary>
    /// The screen the texture is optimized for
    /// </summary>
    public enum SysTexScreen
    {
        /// <summary>
        /// NDS bottom screen
        /// </summary>
        BOTTOM_SCREEN,
        /// <summary>
        /// NDS top screen
        /// </summary>
        TOP_SCREEN,
    }
}
