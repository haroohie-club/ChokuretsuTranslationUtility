using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data
{
    public class SystemTextureFile : DataFile
    {
        public int NumSections { get; set; }
        public int EndPointersOffset { get; set; }
        public int HeaderEndOffset { get; set; }
        public List<DataFileSection> SectionOffsetsAndCounts { get; set; } = new();

        public List<SystemTexture> SystemTextures { get; set; } = new();
        public List<short> LoadOrders { get; set; } = new();

        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            _log = log;

            NumSections = IO.ReadInt(decompressedData, 0x00);
            if (NumSections != 2)
            {
                _log.LogError($"SYSTEX.S should only have 2 sections but {NumSections} were detected.");
                return;
            }
            EndPointersOffset = IO.ReadInt(decompressedData, 0x04);
            HeaderEndOffset = IO.ReadInt(decompressedData, 0x08);
            // There are only two sections
            SectionOffsetsAndCounts.Add(new() { Name = "SYSTEMTEXTURES", Offset = IO.ReadInt(decompressedData, 0x0C), ItemCount = IO.ReadInt(decompressedData, 0x10) });
            SectionOffsetsAndCounts.Add(new() { Name = "LOADORDER", Offset = IO.ReadInt(decompressedData, 0x14), ItemCount = IO.ReadInt(decompressedData, 0x18) });

            for (int i = 0; i < SectionOffsetsAndCounts[0].ItemCount; i++)
            {
                SystemTextures.Add(new(decompressedData.Skip(SectionOffsetsAndCounts[0].Offset + 0x2C * i).Take(0x2C)));
            }

            for (int i = 0; i < SectionOffsetsAndCounts[1].ItemCount; i++)
            {
                LoadOrders.Add(IO.ReadShort(decompressedData, SectionOffsetsAndCounts[1].Offset + 2 * i));
            }
        }

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
                sb.AppendLine($"{Helpers.Indent(3)}.short {includes["GRPBIN"].FirstOrDefault(index => index.Value == SystemTextures[i].GrpIndex)?.Name ?? $"{SystemTextures[i].GrpIndex}"}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].Tpage}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].GraphicType}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].ValidateTex}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].LoadMethod}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].Unknown0E}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].MaxVram}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].Unknown12}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].Unknown14}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].Unknown16}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {includes["GRPBIN"].FirstOrDefault(index => index.Value == SystemTextures[i].AnimationIndex)?.Name ?? $"{SystemTextures[i].AnimationIndex}"}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].Unknown1A}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].Unknown1C}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].Unknown1E}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].Unknown20}");
                sb.AppendLine($"{Helpers.Indent(3)}.short {SystemTextures[i].Unknown22}");
                sb.AppendLine($"{Helpers.Indent(3)}.word {SystemTextures[i].Unknown24}");
                sb.AppendLine($"{Helpers.Indent(3)}.word {SystemTextures[i].Unknown28}");
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

    public class SystemTexture
    {
        public string Name { get; set; }
        public SysTexScreen Screen { get; set; }
        public short GrpIndex { get; set; }
        public short Tpage { get; set; }
        public short GraphicType { get; set; }
        public short ValidateTex { get; set; }
        public short LoadMethod { get; set; }
        public short Unknown0E { get; set; }
        public short MaxVram { get; set; }
        public short Unknown12 { get; set; }
        public short Unknown14 { get; set; }
        public short Unknown16 { get; set; }
        public short AnimationIndex { get; set; }
        public short Unknown1A { get; set; }
        public short Unknown1C { get; set; }
        public short Unknown1E { get; set; }
        public short Unknown20 { get; set; }
        public short Unknown22 { get; set; }
        public int Unknown24 { get; set; }
        public int Unknown28 { get; set; }

        public SystemTexture(IEnumerable<byte> data)
        {
            Screen = (SysTexScreen)IO.ReadInt(data, 0x00);
            GrpIndex = IO.ReadShort(data, 0x04);
            Tpage = IO.ReadShort(data, 0x06);
            GraphicType = IO.ReadShort(data, 0x08);
            ValidateTex = IO.ReadShort(data, 0x0A);
            LoadMethod = IO.ReadShort(data, 0x0C);
            Unknown0E = IO.ReadShort(data, 0x0E);
            MaxVram = IO.ReadShort(data, 0x10);
            Unknown12 = IO.ReadShort(data, 0x12);
            Unknown14 = IO.ReadShort(data, 0x14);
            Unknown16 = IO.ReadShort(data, 0x16);
            AnimationIndex = IO.ReadShort(data, 0x18);
            Unknown1A = IO.ReadShort(data, 0x1A);
            Unknown1C = IO.ReadShort(data, 0x1C);
            Unknown1E = IO.ReadShort(data, 0x1E);
            Unknown20 = IO.ReadShort(data, 0x20);
            Unknown22 = IO.ReadShort(data, 0x22);
            Unknown24 = IO.ReadShort(data, 0x24);
            Unknown28 = IO.ReadShort(data, 0x28);
        }
    }

    public enum SysTexScreen
    {
        BOTTOM_SCREEN,
        TOP_SCREEN,
    }
}
