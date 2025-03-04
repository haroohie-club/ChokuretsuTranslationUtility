using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace HaruhiChokuretsuLib.Archive.Event;

/// <summary>
/// The settings section of the event file
/// </summary>
public class SettingsSection : IEventSection<EventFileSettings>
{
    /// <inheritdoc/>
    public string Name { get; set; }
    /// <inheritdoc />
    public List<EventFileSettings> Objects { get; set; } = [];

    /// <inheritdoc/>
    public void Initialize(byte[] data, int numObjects, string name, ILogger log, int offset)
    {
        Name = name;
        Objects = [new(data)];
    }

    /// <inheritdoc/>
    public string GetAsm(int indentation, ref int currentPointer, EventFile evt)
    {
        StringBuilder sb = new();
        sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
        for (int i = 0; i < Objects.Count; i++)
        {
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(Objects[i].EventNamePointer > 0 ? $"POINTER{currentPointer++}: " : "")}.word EVENTNAME");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {((evt.UnknownSection01?.Objects.Count ?? 0) > 0 ? 1 : 0)}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(evt.UnknownSection01 is not null ? $"POINTER{currentPointer++}: " : "")}.word UNKNOWNSECTION01_POINTER");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {((evt.InteractableObjectsSection?.Objects.Count ?? 0) > 0 ? 1 : 0)}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(evt.InteractableObjectsSection is not null ? $"POINTER{currentPointer++}: " : "")}.word INTERACTABLEOBJECTS_POINTER");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {((evt.UnknownSection03?.Objects.Count ?? 0) > 0 ? 1 : 0)}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(evt.UnknownSection03 is not null ? $"POINTER{currentPointer++}: " : "")}.word UNKNOWNSECTION03_POINTER");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {((evt.StartingChibisSection?.Objects.Count ?? 0) > 0 ? 1 : 0)}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(evt.StartingChibisSection is not null ? $"POINTER{currentPointer++}: " : "")}.word STARTINGCHIBIS_POINTER");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {((evt.MapCharactersSection?.Objects.Count ?? 0) > 0 ? 1 : 0)}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(evt.MapCharactersSection is not null ? $"POINTER{currentPointer++}: " : "")}.word MAPCHARACTERS_POINTER");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {((evt.UnknownSection06?.Objects.Count ?? 0) > 0 ? 1 : 0)}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(evt.UnknownSection06 is not null ? $"POINTER{currentPointer++}: " : "")}.word UNKNOWNSECTION06_POINTER");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {((evt.UnknownSection07?.Objects.Count ?? 0) > 0 ? 1 : 0)}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(evt.UnknownSection07 is not null ? $"POINTER{currentPointer++}: " : "")}.word UNKNOWNSECTION07_POINTER");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {(evt.ChoicesSection?.Objects.Count ?? 0) - 1}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(evt.ChoicesSection is not null ? $"POINTER{currentPointer++}: " : "")}.word CHOICES");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].Unused44}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].Unused48}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {((evt.UnknownSection09?.Objects.Count ?? 0) > 0 ? 1 : 0)}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(evt.UnknownSection09 is not null ? $"POINTER{currentPointer++}: " : "")}.word UNKNOWNSECTION09");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {((evt.UnknownSection10?.Objects.Count ?? 0) > 1 ? 1 : 0)}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(evt.UnknownSection10 is not null ? $"POINTER{currentPointer++}: " : "")}.word UNKNOWNSECTION10");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {(evt.LabelsSection?.Objects.Count ?? 1) - 1}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(Objects[i].LabelsSectionPointer > 0 ? $"POINTER{currentPointer++}: " : "")}.word LABELS");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {evt.DialogueSection.Objects.Count - 1}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(Objects[i].DialogueSectionPointer > 0 ? $"POINTER{currentPointer++}: " : "")}.word DIALOGUESECTION");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {evt.ConditionalsSection?.Objects.Count - 1 ?? 0}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(Objects[i].ConditionalsSectionPointer > 0 ? $"POINTER{currentPointer++}: " : "")}.word CONDITIONALS");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {evt.ScriptSections.Count}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(Objects[i].ScriptSectionDefinitionsSectionPointer > 0 ? $"POINTER{currentPointer++}: " : "")}.word SCRIPTDEFINITIONS");
            for (i = 0; i < 43; i++)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word 0");
            }
        }
        sb.AppendLine();

        return sb.ToString();
    }
}

/// <summary>
/// A pointer section of the event file containing pointers to other sections
/// </summary>
public class PointerSection : IEventSection<PointerStruct>
{
    /// <inheritdoc/>
    public string Name { get; set; }
    /// <inheritdoc/>
    public List<PointerStruct> Objects { get; set; } = [];

    /// <inheritdoc/>
    public void Initialize(byte[] data, int numObjects, string name, ILogger log, int offset)
    {
        Name = name;
        if (numObjects != data.Length / 12)
        {
            log.LogError($"{Name} section in event file has mismatch in number of arguments.");
            return;
        }
            
        for (int i = 0; i < numObjects; i++)
        {
            Objects.Add(new()
            {
                Padding1 = IO.ReadInt(data, i * 12),
                Pointer = IO.ReadInt(data, i * 12 + 4),
                ItemCount = IO.ReadInt(data, i * 12 + 8),
            });
        }
    }

    internal static (int sectionIndex, PointerSection section) ParseSection(List<EventFile.SectionDef> eventFileSections, int pointer, string name, byte[] data, ILogger log)
    {
        int sectionIndex = eventFileSections.FindIndex(s => s.Pointer == pointer);
        PointerSection section = new();
        section.Initialize(data[eventFileSections[sectionIndex].Pointer..eventFileSections[sectionIndex + 1].Pointer],
            eventFileSections[sectionIndex].ItemCount,
            $"{name}_POINTER", log, eventFileSections[sectionIndex].Pointer);
        return (sectionIndex, section);
    }

    internal static PointerSection GetForSection<T>(IEventSection<T> section)
    {
        return new() 
        { 
            Name = $"{section.Name}_POINTER",
            Objects =
            [
                new() { Pointer = 1, ItemCount = section.Objects.Count - 1 }, // doesn't matter since we're only creating this for the ASM output,
            ]
        };
    }

    /// <inheritdoc/>
    public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
    {
        StringBuilder sb = new();
        sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
        foreach (PointerStruct pointerStruct in Objects)
        {
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {pointerStruct.Padding1}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(pointerStruct.Pointer == 0 ? ".word 0" : $"POINTER{currentPointer++}: .word {Name[..Name.IndexOf('_')]}")}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {pointerStruct.ItemCount}");
        }
        for (int i = 0; i < 3; i++)
        {
            sb.AppendLine(".word 0");
        }
        sb.AppendLine();

        return sb.ToString();
    }
}

/// <summary>
/// A struct used by pointer sections to define pointers to other sections
/// </summary>
public struct PointerStruct
{
    /// <summary>
    /// Padding
    /// </summary>
    public int Padding1 { get; set; }
    /// <summary>
    /// The pointer
    /// </summary>
    public int Pointer { get; set; }
    /// <summary>
    /// Number of items in the section being pointed to
    /// </summary>
    public int ItemCount { get; set; }

    /// <inheritdoc/>
    public readonly override string ToString()
    {
        return $"0x{Pointer:X4}";
    }
}

/// <summary>
/// An "integer section" used by sections which only contain integers
/// </summary>
public class IntegerSection : IEventSection<int>
{
    /// <inheritdoc/>
    public string Name { get; set; }
    /// <inheritdoc/>
    public List<int> Objects { get; set; } = [];

    /// <inheritdoc/>
    public void Initialize(byte[] data, int numObjects, string name, ILogger log, int offset)
    {
        Name = name;
        if (numObjects != data.Length / 4)
        {
            log.LogError($"{Name} section in event file has mismatch in number of arguments.");
            return;
        }

        for (int i = 0; i < numObjects; i++)
        {
            Objects.Add(IO.ReadInt(data, i * 4));
        }
    }

    /// <inheritdoc/>
    public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
    {
        StringBuilder sb = new();
        sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
        for (int i = 0; i < Objects.Count; i++)
        {
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i]}");
        }
        sb.AppendLine();

        return sb.ToString();
    }
}

/// <summary>
/// The section containing definitions for interactable objects
/// </summary>
public class InteractableObjectsSection : IEventSection<InteractableObjectEntry>
{
    /// <inheritdoc/>
    public string Name { get; set; }
    /// <inheritdoc/>
    public List<InteractableObjectEntry> Objects { get; set; } = [];

    /// <inheritdoc/>
    public void Initialize(byte[] data, int numObjects, string name, ILogger log, int offset)
    {
        Name = name;
        if (numObjects != data.Length / 6)
        {
            log.LogError($"{Name} section in event file has mismatch in number of arguments.");
            return;
        }

        for (int i = 0; i < numObjects; i++)
        {
            Objects.Add(new()
            {
                ObjectId = IO.ReadShort(data, i * 6),
                ScriptBlock = IO.ReadShort(data, i * 6 + 2),
                Padding = IO.ReadShort(data, i * 6 + 4),
            });
        }
    }

    /// <inheritdoc/>
    public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
    {
        StringBuilder sb = new();
        sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
        foreach (InteractableObjectEntry io in Objects)
        {
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.short {io.ObjectId}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.short {io.ScriptBlock}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.short {io.Padding}");
        }
        if (Objects.Count % 2 == 1)
        {
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.skip 2");
        }
        sb.AppendLine();

        return sb.ToString();
    }
}

/// <summary>
/// An entry for interactable objects
/// </summary>
public class InteractableObjectEntry
{
    /// <summary>
    /// An index containing the interactable object's ID
    /// </summary>
    public short ObjectId { get; set; }
    /// <summary>
    /// The script block the interactable object triggers
    /// </summary>
    public short ScriptBlock { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    public short Padding { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"({ObjectId}, {ScriptBlock}, {Padding})";
    }
}

/// <summary>
/// Unknown
/// </summary>
public class Unknown03Section : IEventSection<Unknown03SectionEntry>
{
    /// <inheritdoc/>
    public string Name { get; set; }
    /// <inheritdoc/>
    public List<Unknown03SectionEntry> Objects { get; set; } = [];

    /// <inheritdoc/>
    public void Initialize(byte[] data, int numObjects, string name, ILogger log, int offset)
    {
        Name = name;
        if (numObjects != data.Length / 12)
        {
            log.LogError($"{Name} section in event file has mismatch in number of arguments.");
            return;
        }

        for (int i = 0; i < numObjects; i++)
        {
            Objects.Add(new()
            {
                UnknownInt1 = IO.ReadInt(data, i * 12),
                UnknownInt2 = IO.ReadInt(data, i * 12 + 4),
                UnknownInt3 = IO.ReadInt(data, i * 12 + 8),
            });
        }
    }

    /// <inheritdoc/>
    public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
    {
        StringBuilder sb = new();
        sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
        foreach (Unknown03SectionEntry u3 in Objects)
        {
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {u3.UnknownInt1}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {u3.UnknownInt2}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {u3.UnknownInt3}");
        }
        sb.AppendLine();

        return sb.ToString();
    }
}

/// <summary>
/// Unknown
/// </summary>
public class Unknown03SectionEntry
{
    /// <summary>
    /// Unknown
    /// </summary>
    public int UnknownInt1 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int UnknownInt2 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int UnknownInt3 { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"({UnknownInt1}, {UnknownInt2}, {UnknownInt3})";
    }
}

/// <summary>
/// The section defining the chibis which appear on the top screen at the start of the event
/// </summary>
public class StartingChibisSection : IEventSection<StartingChibiEntry>
{
    /// <inheritdoc/>
    public string Name { get; set; }
    /// <inheritdoc/>
    public List<StartingChibiEntry> Objects { get; set; } = [];

    /// <inheritdoc/>
    public void Initialize(byte[] data, int numObjects, string name, ILogger log, int offset)
    {
        Name = name;
        if (numObjects != data.Length / 12)
        {
            log.LogError($"{Name} section in event file has mismatch in number of arguments.");
            return;
        }

        for (int i = 0; i < numObjects; i++)
        {
            Objects.Add(new()
            {
                ChibiIndex = IO.ReadShort(data, i * 12),
                UnknownShort2 = IO.ReadShort(data, i * 12 + 2),
                UnknownShort3 = IO.ReadShort(data, i * 12 + 4),
                UnknownShort4 = IO.ReadShort(data, i * 12 + 6),
                UnknownShort5 = IO.ReadShort(data, i * 12 + 8),
                UnknownShort6 = IO.ReadShort(data, i * 12 + 10),
            });
        }
    }

    /// <inheritdoc/>
    public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
    {
        StringBuilder sb = new();
        sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
        foreach (StartingChibiEntry chibi in Objects)
        {
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.short {chibi.ChibiIndex}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {chibi.UnknownShort2}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {chibi.UnknownShort3}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {chibi.UnknownShort4}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {chibi.UnknownShort5}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {chibi.UnknownShort6}");
        }
        sb.AppendLine();

        return sb.ToString();
    }
}

/// <summary>
/// An entry defining a starting chibi
/// </summary>
public class StartingChibiEntry
{
    /// <summary>
    /// The index of the chibi into CHIBI.S in dat.bin
    /// </summary>
    public short ChibiIndex { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public short UnknownShort2 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public short UnknownShort3 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public short UnknownShort4 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public short UnknownShort5 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public short UnknownShort6 { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return ChibiIndex.ToString();
    }
}

/// <summary>
/// The section defining which characters appear on the map for the investigation phase
/// </summary>
public class MapCharactersSection : IEventSection<MapCharactersSectionEntry>
{
    /// <inheritdoc/>
    public string Name { get; set; }
    /// <inheritdoc/>
    public List<MapCharactersSectionEntry> Objects { get; set; } = [];

    /// <inheritdoc/>
    public void Initialize(byte[] data, int numObjects, string name, ILogger log, int offset)
    {
        Name = name;
        if (numObjects != data.Length / 14)
        {
            log.LogError($"{Name} section in event file has mismatch in number of arguments.");
            return;
        }

        for (int i = 0; i < numObjects; i++)
        {
            Objects.Add(new()
            {
                CharacterIndex = IO.ReadInt(data, i * 14),
                FacingDirection = IO.ReadShort(data, i * 14 + 4),
                X = IO.ReadShort(data, i * 14 + 6),
                Y = IO.ReadShort(data, i * 14 + 8),
                TalkScriptBlock = IO.ReadShort(data, i * 14 + 10),
                Padding = IO.ReadShort(data, i * 14 + 12),
            });
        }
    }

    /// <inheritdoc/>
    public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
    {
        StringBuilder sb = new();
        sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
        foreach (MapCharactersSectionEntry mapChar in Objects)
        {
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {mapChar.CharacterIndex}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {mapChar.FacingDirection}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {mapChar.X}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {mapChar.Y}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {mapChar.TalkScriptBlock}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {mapChar.Padding}");
        }
        if (Objects.Count % 2 == 1)
        {
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.skip 2");
        }
        sb.AppendLine();

        return sb.ToString();
    }
}
    
/// <summary>
/// A map character entry in the map characters section of an event file
/// </summary>
public class MapCharactersSectionEntry
{
    /// <summary>
    /// The index of character as defined by their chibi in CHIBI.S
    /// </summary>
    public int CharacterIndex { get; set; }
    /// <summary>
    /// The direction the character faces by default (from 0 to 3, down left, down right, up left, up right)
    /// </summary>
    public short FacingDirection { get; set; }
    /// <summary>
    /// The X position of the chibi on the map (in terms of tiles)
    /// </summary>
    public short X { get; set; }
    /// <summary>
    /// The Y position of the chibi on the map (in terms of tiles)
    /// </summary>
    public short Y { get; set; }
    /// <summary>
    /// The script block that triggers when talking to the character
    /// </summary>
    public short TalkScriptBlock { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    public short Padding { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{CharacterIndex}: ({FacingDirection}, {X}, {Y}, {TalkScriptBlock}, {Padding})";
    }
}

/// <summary>
/// Unknown
/// </summary>
public class Unknown07Section : IEventSection<Unknown07SectionEntry>
{
    /// <inheritdoc/>
    public string Name { get; set; }
    /// <inheritdoc/>
    public List<Unknown07SectionEntry> Objects { get; set; } = [];

    /// <inheritdoc/>
    public void Initialize(byte[] data, int numObjects, string name, ILogger log, int offset)
    {
        Name = name;
        if (numObjects != data.Length / 4)
        {
            log.LogError($"{Name} section in event file has mismatch in number of arguments.");
            return;
        }

        for (int i = 0; i < numObjects; i++)
        {
            Objects.Add(new()
            {
                UnknownShort1 = IO.ReadShort(data, i * 4),
                UnknownShort2 = IO.ReadShort(data, i * 4 + 2),
            });
        }
    }

    /// <inheritdoc/>
    public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
    {
        StringBuilder sb = new();
        sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
        foreach (Unknown07SectionEntry u7 in Objects)
        {
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.short {u7.UnknownShort1}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.short {u7.UnknownShort2}");
        }
        sb.AppendLine();

        return sb.ToString();
    }
}

/// <summary>
/// Unknown
/// </summary>
public class Unknown07SectionEntry
{
    /// <summary>
    /// Unknown
    /// </summary>
    public short UnknownShort1 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public short UnknownShort2 { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"({UnknownShort1}, {UnknownShort2})";
    }
}

/// <summary>
/// The section that defines the choices used by the SELECT command in event files
/// </summary>
public class ChoicesSection : IEventSection<ChoicesSectionEntry>
{
    /// <inheritdoc/>
    public string Name { get; set; }
    /// <inheritdoc/>
    public List<ChoicesSectionEntry> Objects { get; set; } = [];

    /// <inheritdoc/>
    public void Initialize(byte[] data, int numObjects, string name, ILogger log, int offset = -1)
    {
        Name = name;

        for (int i = 0; i < numObjects; i++)
        {
            int textOffset = IO.ReadInt(data, i * 20 + 12) - offset;
            Objects.Add(new()
            {
                Id = IO.ReadInt(data, i * 20),
                Padding1 = IO.ReadInt(data, i * 20 + 4),
                Padding2 = IO.ReadInt(data, i * 20 + 8),
                Text = textOffset == 0 ? string.Empty : IO.ReadShiftJisString(data, textOffset),
                Padding3 = IO.ReadInt(data, i * 20 + 16),
            });
        }
    }

    /// <inheritdoc/>
    public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
    {
        StringBuilder sb = new();
        sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
        for (int i = 0; i < Objects.Count; i++)
        {
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].Id}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].Padding1}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].Padding2}");
            if (Objects[i].Id > 0)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}POINTER{currentPointer++}: .word CHOICE{i:D2}");
            }
            else
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word 0");
            }
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].Padding3}");
        }
        for (int i = 0; i < Objects.Count; i++)
        {
            if (Objects[i].Id > 0)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}CHOICE{i:D2}: .string \"{Objects[i].Text.EscapeShiftJIS()}\"");
                sb.AsmPadString(Objects[i].Text, Encoding.GetEncoding("Shift-JIS"));
            }
        }
        sb.AppendLine();

        return sb.ToString();
    }
}

/// <summary>
/// A choice as defined in an event file
/// </summary>
public class ChoicesSectionEntry
{
    /// <summary>
    /// The ID of the choice as will be referenced by the SELECT command
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    public int Padding1 { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    public int Padding2 { get; set; }
    /// <summary>
    /// The text of the choice
    /// </summary>
    public string Text { get; set; }
    /// <summary>
    /// Padding
    /// </summary>
    public int Padding3 { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Id}: {Text}";
    }
}

/// <summary>
/// Unknown
/// </summary>
public class Unknown08Section : IEventSection<Unknown08SectionEntry>
{
    /// <inheritdoc/>
    public string Name { get; set; }
    /// <inheritdoc/>
    public List<Unknown08SectionEntry> Objects { get; set; } = [];

    /// <inheritdoc/>
    public void Initialize(byte[] data, int numObjects, string name, ILogger log, int offset)
    {
        Name = name;
        if (numObjects != data.Length / 16)
        {
            log.LogError($"{Name} section in event file has mismatch in number of arguments.");
            return;
        }

        for (int i = 0; i < numObjects; i++)
        {
            Objects.Add(new()
            {
                UnknownInt1 = IO.ReadInt(data, i * 4),
                UnknownInt2 = IO.ReadInt(data, i * 4 + 4),
                UnknownInt3 = IO.ReadInt(data, i * 4 + 8),
                UnknownInt4 = IO.ReadInt(data, i * 4 + 12),
            });
        }
    }

    /// <inheritdoc/>
    public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
    {
        StringBuilder sb = new();
        sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
        foreach (Unknown08SectionEntry u8 in Objects)
        {
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {u8.UnknownInt1}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {u8.UnknownInt2}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {u8.UnknownInt3}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {u8.UnknownInt4}");
        }
        sb.AppendLine();

        return sb.ToString();
    }
}

/// <summary>
/// Unknown
/// </summary>
public class Unknown08SectionEntry
{
    /// <summary>
    /// Unknown
    /// </summary>
    public int UnknownInt1 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int UnknownInt2 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int UnknownInt3 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int UnknownInt4 { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"({UnknownInt1}, {UnknownInt2}, {UnknownInt3}, {UnknownInt4})";
    }
}

/// <summary>
/// Unknown
/// </summary>
public class Unknown09Section : IEventSection<Unknown09SectionEntry>
{
    /// <inheritdoc/>
    public string Name { get; set; }
    /// <inheritdoc/>
    public List<Unknown09SectionEntry> Objects { get; set; } = [];

    /// <inheritdoc/>
    public void Initialize(byte[] data, int numObjects, string name, ILogger log, int offset)
    {
        Name = name;
        if (numObjects != data.Length / 8)
        {
            log.LogError($"{Name} section in event file has mismatch in number of arguments.");
            return;
        }

        for (int i = 0; i < numObjects; i++)
        {
            Objects.Add(new()
            {
                UnknownInt1 = IO.ReadInt(data, i * 4),
                UnknownInt2 = IO.ReadInt(data, i * 4 + 4),
            });
        }
    }

    /// <inheritdoc/>
    public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
    {
        StringBuilder sb = new();
        sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
        foreach (Unknown09SectionEntry u9 in Objects)
        {
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {u9.UnknownInt1}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {u9.UnknownInt2}");
        }
        sb.AppendLine();

        return sb.ToString();
    }
}

/// <summary>
/// Unknown
/// </summary>
public class Unknown09SectionEntry
{
    /// <summary>
    /// Unknown
    /// </summary>
    public int UnknownInt1 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int UnknownInt2 { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"({UnknownInt1}, {UnknownInt2})";
    }
}

/// <summary>
/// Unknown
/// </summary>
public class Unknown10Section : IEventSection<Unknown10SectionEntry>
{
    /// <inheritdoc/>
    public string Name { get; set; }
    /// <inheritdoc/>
    public List<Unknown10SectionEntry> Objects { get; set; } = [];

    /// <inheritdoc/>
    public void Initialize(byte[] data, int numObjects, string name, ILogger log, int offset)
    {
        Name = name;
        if (numObjects != data.Length / 8)
        {
            log.LogError($"{Name} section in event file has mismatch in number of arguments.");
            return;
        }

        for (int i = 0; i < numObjects; i++)
        {
            Objects.Add(new()
            {
                UnknownInt1 = IO.ReadInt(data, i * 4),
                UnknownInt2 = IO.ReadInt(data, i * 4 + 4),
            });
        }
    }

    /// <inheritdoc/>
    public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
    {
        StringBuilder sb = new();
        sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
        foreach (Unknown10SectionEntry u10 in Objects)
        {
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {u10.UnknownInt1}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {u10.UnknownInt2}");
        }
        sb.AppendLine();

        return sb.ToString();
    }
}

/// <summary>
/// Unknown
/// </summary>
public class Unknown10SectionEntry
{
    /// <summary>
    /// Unknown
    /// </summary>
    public int UnknownInt1 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int UnknownInt2 { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"({UnknownInt1}, {UnknownInt2})";
    }
}

/// <summary>
/// The section that defines the names of the different script blocks
/// </summary>
public class LabelsSection : IEventSection<LabelsSectionEntry>
{
    /// <inheritdoc/>
    public string Name { get; set; }
    /// <inheritdoc/>
    public List<LabelsSectionEntry> Objects { get; set; } = [];

    /// <inheritdoc/>
    public void Initialize(byte[] data, int numObjects, string name, ILogger log, int offset)
    {
        Name = name;

        for (int i = 0; i < numObjects; i++)
        {
            int nameOffset = IO.ReadInt(data, i * 8 + 4) - offset;
            Objects.Add(new()
            {
                Id = IO.ReadShort(data, i * 8 + 2),
                Name = nameOffset > 0 ? IO.ReadShiftJisString(data, nameOffset) : string.Empty,
            });
        }
    }

    /// <inheritdoc/>
    public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
    {
        StringBuilder sb = new();
        sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
        for (int i = 0; i < Objects.Count; i++)
        {
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.skip 2");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.short {Objects[i].Id}");
            if (Objects[i].Id > 0)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}POINTER{currentPointer++}: .word LABEL{i:D2}");
            }
            else
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word 0");
            }
        }
        for (int i = 0; i < Objects.Count; i++)
        {
            if (Objects[i].Id > 0)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}LABEL{i:D2}: .string \"{Objects[i].Name}\"");
                sb.AsmPadString(Objects[i].Name, Encoding.ASCII);
            }
        }
        sb.AppendLine();

        return sb.ToString();
    }
}

/// <summary>
/// A label for a script block in an event file
/// </summary>
public class LabelsSectionEntry
{
    /// <summary>
    /// The position of the script block
    /// </summary>
    public short Id { get; set; }
    /// <summary>
    /// The name of the script block
    /// </summary>
    public string Name { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Id}: {Name}";
    }
}

/// <summary>
/// A dramatis personae (character name) section
/// While referenced by dialogue lines, they aren't actually used in-game and thus are probably
/// remnants of dev tooling
/// </summary>
public class DramatisPersonaeSection : IEventSection<string>
{
    /// <inheritdoc/>
    public string Name { get; set; }
    /// <inheritdoc/>
    public List<string> Objects { get; set; } = [];
    /// <summary>
    /// The offset at which the dramatis personae section appears in the file
    /// </summary>
    public int Offset { get; set; }
    /// <summary>
    /// The index (order of appearance) of this dramatis persona
    /// </summary>
    public int Index { get; set; }

    /// <inheritdoc/>
    public void Initialize(byte[] data, int numObjects, string name, ILogger log, int offset)
    {
        Name = name;
        Offset = offset;

        Objects.Add(IO.ReadShiftJisString(data, 0x00));
    }

    /// <inheritdoc/>
    public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
    {
        StringBuilder sb = new();
        sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
        sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.string \"{Objects[0].EscapeShiftJIS()}\"");
        sb.AsmPadString(Objects[0], Encoding.GetEncoding("Shift-JIS"));
        sb.AppendLine();

        return sb.ToString();
    }
}

/// <summary>
/// The dialogue section containing dialogue lines used in an event file
/// </summary>
public class DialogueSection : IEventSection<DialogueLine>
{
    /// <inheritdoc/>
    public string Name { get; set; }
    /// <inheritdoc/>
    public List<DialogueLine> Objects { get; set; } = [];

    /// <inheritdoc/>
    public void Initialize(byte[] data, int numObjects, string name, ILogger log, int offset)
    {
        Name = name;

        for (int i = 0; i < numObjects; i++)
        {
            int dramatisPersonaeOffset = IO.ReadInt(data, offset + i * 12 + 4);
            int dialoguePointer = IO.ReadInt(data, offset + i * 12 + 8);
            Objects.Add(new((Speaker)IO.ReadInt(data, offset + i * 12),
                IO.ReadShiftJisString(data, dramatisPersonaeOffset),
                dramatisPersonaeOffset,
                dialoguePointer,
                data.ToArray()));
        }
    }

    /// <summary>
    /// Initializes the dramatis personae indices for dialogue lines
    /// </summary>
    /// <param name="dramatisPersonae">List of dramatis personae sections</param>
    internal void InitializeDramatisPersonaeIndices(List<DramatisPersonaeSection> dramatisPersonae)
    {
        for (int i = 0; i < Objects.Count; i++)
        {
            if ((int)Objects[i].Speaker > 0)
            {
                Objects[i].SpeakerIndex = dramatisPersonae.First(d => d.Offset == Objects[i].SpeakerPointer).Index;
            }
        }
    }

    /// <inheritdoc/>
    public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
    {
        StringBuilder sb = new();
        sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
        for (int i = 0; i < Objects.Count; i++)
        {
            int speakerInt = (int)Objects[i].Speaker;
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.int {speakerInt}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(speakerInt > 0 ? $"POINTER{currentPointer++}: .word DRAMATISPERSONAE{Objects[i].SpeakerIndex}" : ".word 0")}");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(speakerInt > 0 && Objects[i].Pointer > 0 ? $"POINTER{currentPointer++}: .word DIALOGUELINE{i:D3}" : ".word 0")}");
        }
        for (int i = 0; i < Objects.Count; i++)
        {
            if ((int)Objects[i].Speaker > 0 && Objects[i].Pointer > 0)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}DIALOGUELINE{i:D3}: .string \"{Objects[i].Text.EscapeShiftJIS()}\"");
                sb.AsmPadString(Objects[i].Text, Encoding.GetEncoding("Shift-JIS"));
            }
        }
        sb.AppendLine();

        return sb.ToString();
    }
}

/// <summary>
/// The section of conditionals used in an event file
/// </summary>
public class ConditionalSection : IEventSection<string>
{
    /// <inheritdoc/>
    public string Name { get; set; }
    /// <inheritdoc/>
    public List<string> Objects { get; set; } = [];

    /// <inheritdoc/>
    public void Initialize(byte[] data, int numObjects, string name, ILogger log, int offset)
    {
        Name = name;

        for (int i = 0; i < numObjects; i++)
        {
            int pointer = IO.ReadInt(data, offset + i * 4);
            Objects.Add(pointer > 0
                ? IO.ReadAsciiString(data, pointer)
                : null);
        }
    }

    /// <inheritdoc/>
    public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
    {
        StringBuilder sb = new();
        sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
        for (int i = 0; i < Objects.Count; i++)
        {
            if (!string.IsNullOrEmpty(Objects[i]))
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}POINTER{currentPointer++}: .word CONDITIONAL_{i:D2}");
            }
            else
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word 0");
            }
        }
        for (int i = 0; i < Objects.Count; i++)
        {
            if (!string.IsNullOrEmpty(Objects[i]))
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}CONDITIONAL_{i:D2}: .string \"{Objects[i]}\"");
                sb.AsmPadString(Objects[i], Encoding.ASCII);
            }
        }
        sb.AppendLine();

        return sb.ToString();
    }
}

/// <summary>
/// The section which defines the various script sections in an event file
/// </summary>
public class ScriptSectionDefinitionsSection : IEventSection<ScriptSectionDefinition>
{
    /// <inheritdoc/>
    public string Name { get; set; }
    /// <summary>
    /// The labels associated with the script sections
    /// </summary>
    public List<string> Labels { get; set; }
    /// <inheritdoc/>
    public List<ScriptSectionDefinition> Objects { get; set; } = [];

    /// <inheritdoc/>
    public void Initialize(byte[] data, int numObjects, string name, ILogger log, int offset)
    {
        Name = name;
        for (int i = 0; i < numObjects; i++)
        {
            Objects.Add(new()
            {
                Name = string.IsNullOrEmpty(Labels[i]) ? $"SCRIPT{i:D2}" : Labels[i],
                NumCommands = IO.ReadInt(data, i * 8),
                Pointer = IO.ReadInt(data, i * 8 + 4),
            });
        }
    }

    /// <inheritdoc/>
    public string GetAsm(int indentation, ref int currentPointer, EventFile evt)
    {
        StringBuilder sb = new();
        sb.AppendLine($"{Helpers.Indent(indentation)}{Name}:");
        foreach (ScriptSection section in evt.ScriptSections)
        {
            sb.AppendLine($"{Helpers.Indent(indentation + 3)}.word {section.Objects.Count}");
            if (section.Objects.Count > 0)
            {
                sb.AppendLine($"{Helpers.Indent(indentation + 3)}POINTER{currentPointer++}: .word {section.Name}");
            }
            else
            {
                sb.AppendLine($"{Helpers.Indent(indentation + 3)}.word 0");
            }
        }
        sb.AppendLine($"{Helpers.Indent(indentation + 3)}.skip 8");
        sb.AppendLine();

        return sb.ToString();
    }
}

/// <summary>
/// A definition of a script section in an event file
/// </summary>
public class ScriptSectionDefinition
{
    /// <summary>
    /// The name of the script section (determined in the labels section)
    /// </summary>
    public string Name { get; internal set; }
    /// <summary>
    /// The number of commands in the script section
    /// </summary>
    public int NumCommands { get; internal set; }
    internal int Pointer { get; set; }
}

/// <summary>
/// A script section used in an event file
/// </summary>
public class ScriptSection : IEventSection<ScriptCommandInvocation>
{
    /// <inheritdoc/>
    public string Name { get; set; }
    /// <inheritdoc/>
    public List<ScriptCommandInvocation> Objects { get; set; } = [];
    /// <summary>
    /// List of all commands available to the script section (just all the commands, here for ease of access)
    /// </summary>
    [JsonIgnore]
    public List<ScriptCommand> CommandsAvailable { get; set; }

    /// <inheritdoc/>
    public void Initialize(byte[] data, int numObjects, string name, ILogger log, int offset)
    {
        Name = name;

        for (int i = 0; i < numObjects; i++)
        {
            Objects.Add(new(data[(i * 0x24)..((i + 1) * 0x24)], CommandsAvailable));
        }
    }

    /// <inheritdoc/>
    public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
    {
        StringBuilder sb = new();
        sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");

        foreach (ScriptCommandInvocation command in Objects)
        {
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{command.Command.Mnemonic} {string.Join(", ", command.Parameters.Take(command.Command.Parameters.Length))}");
        }
        sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.skip 0x24");
        sb.AppendLine();

        return sb.ToString();
    }
}

/// <summary>
/// The event name section defining the title of the event
/// </summary>
public class EventNameSection : IEventSection<string>
{
    /// <inheritdoc/>
    public string Name { get; set; }
    /// <inheritdoc/>
    public List<string> Objects { get; set; } = [];

    /// <inheritdoc/>
    public void Initialize(byte[] data, int numObjects, string name, ILogger log, int offset)
    {
        Name = name;
        Objects.Add(IO.ReadAsciiString(data, 0));
    }

    /// <inheritdoc/>
    public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
    {
        StringBuilder sb = new();
        sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}: .string \"{Objects[0]}\"");
        sb.AsmPadString(Objects[0], Encoding.ASCII);
        sb.AppendLine();

        return sb.ToString();
    }
}