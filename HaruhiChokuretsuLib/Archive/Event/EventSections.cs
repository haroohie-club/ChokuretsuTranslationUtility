using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Event
{
    public class GenericSection : IEventSection<object>, IConvertible
    {
        public string Name { get; set; }
        public List<byte> Data { get; set; }
        public int NumObjects { get; set; }
        public int ObjectLength { get; set; }
        public List<object> Objects { get; set; }
        public Type SectionType { get; set; }
        public Type ObjectType { get; set; }

        public void Initialize(IEnumerable<byte> data, int numObjects, string name, int offset)
        {
        }

        public IEventSection<object> GetGeneric()
        {
            return this;
        }

        public string GetAsm(int indentation, ref int currentPointer)
        {
            return string.Empty;
        }

        public TypeCode GetTypeCode()
        {
            return TypeCode.Object;
        }

        private static T ThrowNotSupported<T>()
        {
            return (T)ThrowNotSupported(typeof(T));
        }
        private static object ThrowNotSupported(Type type)
        {
            throw new InvalidCastException($"Converting type \"{typeof(GenericSection)}\" to type \"{type}\" is not supported.");
        }

        bool IConvertible.ToBoolean(IFormatProvider provider) => ThrowNotSupported<bool>();
        char IConvertible.ToChar(IFormatProvider provider) => ThrowNotSupported<char>();
        sbyte IConvertible.ToSByte(IFormatProvider provider) => ThrowNotSupported<sbyte>();
        byte IConvertible.ToByte(IFormatProvider provider) => ThrowNotSupported<byte>();
        short IConvertible.ToInt16(IFormatProvider provider) => ThrowNotSupported<short>();
        ushort IConvertible.ToUInt16(IFormatProvider provider) => ThrowNotSupported<ushort>();
        int IConvertible.ToInt32(IFormatProvider provider) => ThrowNotSupported<int>();
        uint IConvertible.ToUInt32(IFormatProvider provider) => ThrowNotSupported<uint>();
        long IConvertible.ToInt64(IFormatProvider provider) => ThrowNotSupported<long>();
        ulong IConvertible.ToUInt64(IFormatProvider provider) => ThrowNotSupported<ulong>();
        float IConvertible.ToSingle(IFormatProvider provider) => ThrowNotSupported<float>();
        double IConvertible.ToDouble(IFormatProvider provider) => ThrowNotSupported<double>();
        decimal IConvertible.ToDecimal(IFormatProvider provider) => ThrowNotSupported<decimal>();
        DateTime IConvertible.ToDateTime(IFormatProvider provider) => ThrowNotSupported<DateTime>();
        string IConvertible.ToString(IFormatProvider provider) => ThrowNotSupported<string>();

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            if (conversionType == typeof(SettingsSection))
            {
                return new SettingsSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Select(o => (EventFileSettings)Convert.ChangeType(o, ObjectType)).ToList(), SectionType = SectionType, ObjectType = ObjectType };
            }
            else if (conversionType == typeof(PointerSection))
            {
                return new PointerSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Select(o => (PointerStruct)Convert.ChangeType(o, ObjectType)).ToList(), SectionType = SectionType, ObjectType = ObjectType };
            }
            else if (conversionType == typeof(IntegerSection))
            {
                return new IntegerSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Select(o => (int)Convert.ChangeType(o, ObjectType)).ToList(), SectionType = SectionType, ObjectType = ObjectType };
            }
            else if (conversionType == typeof(Unknown02Section))
            {
                return new Unknown02Section() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Select(o => (Unknown02SectionEntry)Convert.ChangeType(o, ObjectType)).ToList(), SectionType = SectionType, ObjectType = ObjectType };
            }
            else if (conversionType == typeof(Unknown03Section))
            {
                return new Unknown03Section() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Select(o => (Unknown03SectionEntry)Convert.ChangeType(o, ObjectType)).ToList(), SectionType = SectionType, ObjectType = ObjectType };
            }
            else if (conversionType == typeof(StartingChibisSection))
            {
                return new StartingChibisSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Select(o => (StartingChibiEntry)Convert.ChangeType(o, ObjectType)).ToList(), SectionType = SectionType, ObjectType = ObjectType };
            }
            else if (conversionType == typeof(MapCharactersSection))
            {
                return new MapCharactersSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Select(o => (MapCharactersSectionEntry)Convert.ChangeType(o, ObjectType)).ToList(), SectionType = SectionType, ObjectType = ObjectType };
            }
            else if (conversionType == typeof(Unknown07Section))
            {
                return new Unknown07Section() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Select(o => (Unknown07SectionEntry)Convert.ChangeType(o, ObjectType)).ToList(), SectionType = SectionType, ObjectType = ObjectType };
            }
            else if (conversionType == typeof(ChoicesSection))
            {
                return new ChoicesSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Select(o => (ChoicesSectionEntry)Convert.ChangeType(o, ObjectType)).ToList(), SectionType = SectionType, ObjectType = ObjectType };
            }
            else if (conversionType == typeof(Unknown08Section))
            {
                return new Unknown08Section() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Select(o => (Unknown08SectionEntry)Convert.ChangeType(o, ObjectType)).ToList(), SectionType = SectionType, ObjectType = ObjectType };
            }
            else if (conversionType == typeof(Unknown09Section))
            {
                return new Unknown09Section() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Select(o => (Unknown09SectionEntry)Convert.ChangeType(o, ObjectType)).ToList(), SectionType = SectionType, ObjectType = ObjectType };
            }
            else if (conversionType == typeof(Unknown10Section))
            {
                return new Unknown10Section() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Select(o => (Unknown10SectionEntry)Convert.ChangeType(o, ObjectType)).ToList(), SectionType = SectionType, ObjectType = ObjectType };
            }
            else if (conversionType == typeof(LabelsSection))
            {
                return new LabelsSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Select(o => (FlagsSectionEntry)Convert.ChangeType(o, ObjectType)).ToList(), SectionType = SectionType, ObjectType = ObjectType };
            }
            else if (conversionType == typeof(DramatisPersonaeSection))
            {
                return new DramatisPersonaeSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Select(o => (string)Convert.ChangeType(o, ObjectType)).ToList(), SectionType = SectionType, ObjectType = ObjectType };
            }
            else if (conversionType == typeof(DialogueSection))
            {
                return new DialogueSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Select(o => (DialogueLine)Convert.ChangeType(o, ObjectType)).ToList(), SectionType = SectionType, ObjectType = ObjectType };
            }
            else if (conversionType == typeof(ConditionalSection))
            {
                return new ConditionalSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Select(o => (string)Convert.ChangeType(o, ObjectType)).ToList(), SectionType = SectionType, ObjectType = ObjectType };
            }
            else if (conversionType == typeof(ScriptSectionDefinitionsSection))
            {
                return new ScriptSectionDefinitionsSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Select(o => (ScriptSectionDefinition)Convert.ChangeType(o, ObjectType)).ToList(), SectionType = SectionType, ObjectType = ObjectType };
            }
            else if (conversionType == typeof(ScriptSection))
            {
                return new ScriptSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Select(o => (ScriptCommandInvocation)Convert.ChangeType(o, ObjectType)).ToList(), SectionType = SectionType, ObjectType = ObjectType };
            }
            else if (conversionType == typeof(EventNameSection))
            {
                return new EventNameSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Select(o => (string)Convert.ChangeType(o, ObjectType)).ToList(), SectionType = SectionType, ObjectType = ObjectType };
            }

            return ThrowNotSupported(conversionType);
        }
    }

    public class SettingsSection : IEventSection<EventFileSettings>
    {
        public string Name { get; set; }
        public List<byte> Data { get; set; }
        public int NumObjects { get; set; }
        public int ObjectLength { get; set; }
        public List<EventFileSettings> Objects { get; set; } = new();
        public Type SectionType { get; set; }
        public Type ObjectType { get; set; }

        public void Initialize(IEnumerable<byte> data, int numObjects, string name, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectLength = 0x128;
            SectionType = typeof(SettingsSection);
            ObjectType = typeof(EventFileSettings);

            Objects = new() { new(data) };
        }

        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        public string GetAsm(int indentation, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
            for (int i = 0; i < NumObjects; i++)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(Objects[i].EventNamePointer > 0 ? $"POINTER{currentPointer++}: " : "")}.word EVENTNAME");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].NumUnknown01}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(Objects[i].UnknownSection01Pointer > 0 ? $"POINTER{currentPointer++}: " : "")}.word UNKNOWNSECTION01_POINTER");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].NumUnknown02}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(Objects[i].UnknownSection02Pointer > 0 ? $"POINTER{currentPointer++}: " : "")}.word UNKNOWNSECTION02_POINTER");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].NumUnknown03}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(Objects[i].UnknownSection03Pointer > 0 ? $"POINTER{currentPointer++}: " : "")}.word UNKNOWNSECTION03_POINTER");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].NumStartingChibisSections}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(Objects[i].StartingChibisSectionPointer > 0 ? $"POINTER{currentPointer++}: " : "")}.word STARTINGCHIBIS_POINTER");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].NumMapCharacterSections}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(Objects[i].MapCharactersSectionPointer > 0 ? $"POINTER{currentPointer++}: " : "")}.word MAPCHARACTERS_POINTER");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].NumUnknown06}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(Objects[i].UnknownSection06Pointer > 0 ? $"POINTER{currentPointer++}: " : "")}.word UNKNOWNSECTION06_POINTER");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].NumUnknown07}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(Objects[i].UnknownSection07Pointer > 0 ? $"POINTER{currentPointer++}: " : "")}.word UNKNOWNSECTION07_POINTER");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].NumChoices}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(Objects[i].ChoicesSectionPointer > 0 ? $"POINTER{currentPointer++}: " : "")}.word CHOICES");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].Unused44}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].Unused48}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].NumUnknown09}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(Objects[i].UnknownSection09Pointer > 0 ? $"POINTER{currentPointer++}: " : "")}.word UNKNOWNSECTION09");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].NumUnknown10}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(Objects[i].UnknownSection10Pointer > 0 ? $"POINTER{currentPointer++}: " : "")}.word UNKNOWNSECTION10");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].NumLabels}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(Objects[i].LabelsSectionPointer > 0 ? $"POINTER{currentPointer++}: " : "")}.word LABELS");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].NumDialogueEntries}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(Objects[i].DialogueSectionPointer > 0 ? $"POINTER{currentPointer++}: " : "")}.word DIALOGUESECTION");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].NumConditionals}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(Objects[i].ConditionalsSectionPointer > 0 ? $"POINTER{currentPointer++}: " : "")}.word CONDITIONALS");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].NumScriptSections}");
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

    public class PointerSection : IEventSection<PointerStruct>
    {
        public string Name { get; set; }
        public List<byte> Data { get; set; }
        public int NumObjects { get; set; }
        public int ObjectLength { get; set; }
        public List<PointerStruct> Objects { get; set; } = new();
        public Type SectionType { get; set; }
        public Type ObjectType { get; set; }

        public void Initialize(IEnumerable<byte> data, int numObjects, string name, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectLength = 12;
            if (NumObjects != Data.Count / ObjectLength)
            {
                throw new ArgumentException($"{Name} section in event file has mismatch in number of arguments.");
            }
            SectionType = typeof(PointerSection);
            ObjectType = typeof(PointerStruct);

            for (int i = 0; i < NumObjects; i++)
            {
                Objects.Add(new()
                {
                    Padding1 = BitConverter.ToInt32(data.Skip(i * ObjectLength).Take(4).ToArray()),
                    Pointer = BitConverter.ToInt32(data.Skip(i * ObjectLength + 4).Take(4).ToArray()),
                    Padding2 = BitConverter.ToInt32(data.Skip(i * ObjectLength + 8).Take(4).ToArray()),
                });
            }
        }

        public static (int sectionIndex, PointerSection section) ParseSection(List<EventFileSection> eventFileSections, int pointer, string name, IEnumerable<byte> data)
        {
            int sectionIndex = eventFileSections.FindIndex(s => s.Pointer == pointer);
            PointerSection section = new();
            section.Initialize(data.Skip(eventFileSections[sectionIndex].Pointer)
                .Take(eventFileSections[sectionIndex + 1].Pointer - eventFileSections[sectionIndex].Pointer),
                eventFileSections[sectionIndex].ItemCount,
                $"{name}_POINTER", eventFileSections[sectionIndex].Pointer);
            return (sectionIndex, section);
        }

        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        public string GetAsm(int indentation, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
            for (int i = 0; i < NumObjects; i++)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].Padding1}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(Objects[i].Pointer == 0 ? ".word 0" : $"POINTER{currentPointer++}: .word {Name[0..Name.IndexOf('_')]}")}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].Padding2}");
            }
            sb.AppendLine();

            return sb.ToString();
        }
    }

    public struct PointerStruct
    {
        public int Padding1 { get; set; }
        public int Pointer { get; set; }
        public int Padding2 { get; set; }

        public override string ToString()
        {
            return $"0x{Pointer:X4}";
        }
    }

    public class IntegerSection : IEventSection<int>
    {
        public string Name { get; set; }
        public List<byte> Data { get; set; }
        public int NumObjects { get; set; }
        public int ObjectLength { get; set; }
        public List<int> Objects { get; set; } = new();
        public Type SectionType { get; set; }
        public Type ObjectType { get; set; }

        public void Initialize(IEnumerable<byte> data, int numObjects, string name, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectLength = 4;
            if (NumObjects != Data.Count / ObjectLength)
            {
                throw new ArgumentException($"{Name} section in event file has mismatch in number of arguments.");
            }
            SectionType = typeof(IntegerSection);
            ObjectType = typeof(int);

            for (int i = 0; i < NumObjects; i++)
            {
                Objects.Add(BitConverter.ToInt32(data.Skip(i * 4).Take(4).ToArray()));
            }
        }

        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        public string GetAsm(int indentation, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
            for (int i = 0; i < NumObjects; i++)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i]}");
            }
            sb.AppendLine();

            return sb.ToString();
        }
    }

    public class Unknown02Section : IEventSection<Unknown02SectionEntry>
    {
        public string Name { get; set; }
        public List<byte> Data { get; set; }
        public int NumObjects { get; set; }
        public int ObjectLength { get; set; }
        public List<Unknown02SectionEntry> Objects { get; set; } = new();
        public Type SectionType { get; set; }
        public Type ObjectType { get; set; }

        public void Initialize(IEnumerable<byte> data, int numObjects, string name, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectLength = 6;
            if (NumObjects != Data.Count / ObjectLength)
            {
                throw new ArgumentException($"{Name} section in event file has mismatch in number of arguments.");
            }
            SectionType = typeof(Unknown02Section);
            ObjectType = typeof(Unknown02SectionEntry);

            for (int i = 0; i < NumObjects; i++)
            {
                Objects.Add(new()
                {
                    UnknownShort1 = BitConverter.ToInt16(data.Skip(i * 6).Take(2).ToArray()),
                    UnknownShort2 = BitConverter.ToInt16(data.Skip(i * 6 + 2).Take(2).ToArray()),
                    UnknownShort3 = BitConverter.ToInt16(data.Skip(i * 6 + 4).Take(2).ToArray()),
                });
            }
        }

        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        public string GetAsm(int indentation, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
            for (int i = 0; i < NumObjects; i++)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.short {Objects[i].UnknownShort1}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.short {Objects[i].UnknownShort2}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.short {Objects[i].UnknownShort3}");
            }
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.skip 2");
            sb.AppendLine();

            return sb.ToString();
        }
    }

    public struct Unknown02SectionEntry
    {
        public short UnknownShort1 { get; set; }
        public short UnknownShort2 { get; set; }
        public short UnknownShort3 { get; set; }

        public override string ToString()
        {
            return $"({UnknownShort1}, {UnknownShort2}, {UnknownShort3})";
        }
    }

    public class Unknown03Section : IEventSection<Unknown03SectionEntry>
    {
        public string Name { get; set; }
        public List<byte> Data { get; set; }
        public int NumObjects { get; set; }
        public int ObjectLength { get; set; }
        public List<Unknown03SectionEntry> Objects { get; set; } = new();
        public Type SectionType { get; set; }
        public Type ObjectType { get; set; }

        public void Initialize(IEnumerable<byte> data, int numObjects, string name, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectLength = 12;
            if (NumObjects != Data.Count / ObjectLength)
            {
                throw new ArgumentException($"{Name} section in event file has mismatch in number of arguments.");
            }
            SectionType = typeof(Unknown03Section);
            ObjectType = typeof(Unknown03SectionEntry);

            for (int i = 0; i < NumObjects; i++)
            {
                Objects.Add(new()
                {
                    UnknownInt1 = BitConverter.ToInt16(data.Skip(i * 12).Take(4).ToArray()),
                    UnknownInt2 = BitConverter.ToInt16(data.Skip(i * 12 + 4).Take(4).ToArray()),
                    UnknownInt3 = BitConverter.ToInt16(data.Skip(i * 12 + 8).Take(4).ToArray()),
                });
            }
        }

        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        public string GetAsm(int indentation, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
            for (int i = 0; i < NumObjects; i++)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].UnknownInt1}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].UnknownInt2}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].UnknownInt3}");
            }
            sb.AppendLine();

            return sb.ToString();
        }
    }

    public struct Unknown03SectionEntry
    {
        public int UnknownInt1 { get; set; }
        public int UnknownInt2 { get; set; }
        public int UnknownInt3 { get; set; }

        public override string ToString()
        {
            return $"({UnknownInt1}, {UnknownInt2}, {UnknownInt3})";
        }
    }

    public class StartingChibisSection : IEventSection<StartingChibiEntry>
    {
        public string Name { get; set; }
        public List<byte> Data { get; set; }
        public int NumObjects { get; set; }
        public int ObjectLength { get; set; }
        public List<StartingChibiEntry> Objects { get; set; } = new();
        public Type SectionType { get; set; }
        public Type ObjectType { get; set; }

        public void Initialize(IEnumerable<byte> data, int numObjects, string name, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectLength = 12;
            if (NumObjects != Data.Count / ObjectLength)
            {
                throw new ArgumentException($"{Name} section in event file has mismatch in number of arguments.");
            }
            SectionType = typeof(StartingChibisSection);
            ObjectType = typeof(StartingChibiEntry);

            for (int i = 0; i < NumObjects; i++)
            {
                Objects.Add(new()
                {
                    ChibiIndex = BitConverter.ToInt16(data.Skip(i * 12).Take(2).ToArray()),
                    UnknownShort2 = BitConverter.ToInt16(data.Skip(i * 12 + 2).Take(2).ToArray()),
                    UnknownShort3 = BitConverter.ToInt16(data.Skip(i * 12 + 4).Take(2).ToArray()),
                    UnknownShort4 = BitConverter.ToInt16(data.Skip(i * 12 + 6).Take(2).ToArray()),
                    UnknownShort5 = BitConverter.ToInt16(data.Skip(i * 12 + 8).Take(2).ToArray()),
                    UnknownShort6 = BitConverter.ToInt16(data.Skip(i * 12 + 10).Take(2).ToArray()),
                });
            }
        }

        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        public string GetAsm(int indentation, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
            for (int i = 0; i < NumObjects; i++)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.short {Objects[i].ChibiIndex}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {Objects[i].UnknownShort2}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {Objects[i].UnknownShort3}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {Objects[i].UnknownShort4}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {Objects[i].UnknownShort5}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {Objects[i].UnknownShort6}");
            }
            sb.AppendLine();

            return sb.ToString();
        }
    }

    public struct StartingChibiEntry
    {
        public short ChibiIndex { get; set; }
        public short UnknownShort2 { get; set; }
        public short UnknownShort3 { get; set; }
        public short UnknownShort4 { get; set; }
        public short UnknownShort5 { get; set; }
        public short UnknownShort6 { get; set; }

        public override string ToString()
        {
            return ChibiIndex.ToString();
        }
    }

    public class MapCharactersSection : IEventSection<MapCharactersSectionEntry>
    {
        public string Name { get; set; }
        public List<byte> Data { get; set; }
        public int NumObjects { get; set; }
        public int ObjectLength { get; set; }
        public List<MapCharactersSectionEntry> Objects { get; set; } = new();
        public Type SectionType { get; set; }
        public Type ObjectType { get; set; }

        public void Initialize(IEnumerable<byte> data, int numObjects, string name, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectLength = 14;
            if (NumObjects != Data.Count / ObjectLength)
            {
                throw new ArgumentException($"{Name} section in event file has mismatch in number of arguments.");
            }
            SectionType = typeof(MapCharactersSection);
            ObjectType = typeof(MapCharactersSectionEntry);

            for (int i = 0; i < NumObjects; i++)
            {
                Objects.Add(new()
                {
                    CharacterIndex = BitConverter.ToInt32(data.Skip(i * 14).Take(4).ToArray()),
                    FacingDirection = BitConverter.ToInt16(data.Skip(i * 14 + 4).Take(2).ToArray()),
                    X = BitConverter.ToInt16(data.Skip(i * 14 + 6).Take(2).ToArray()),
                    Y = BitConverter.ToInt16(data.Skip(i * 14 + 8).Take(2).ToArray()),
                    TalkScriptBlock = BitConverter.ToInt16(data.Skip(i * 14 + 10).Take(2).ToArray()),
                    Padding = BitConverter.ToInt16(data.Skip(i * 14 + 12).Take(2).ToArray()),
                });
            }
        }

        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        public string GetAsm(int indentation, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
            for (int i = 0; i < NumObjects; i++)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].CharacterIndex}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {Objects[i].FacingDirection}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {Objects[i].X}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {Objects[i].Y}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {Objects[i].TalkScriptBlock}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {Objects[i].Padding}");
            }
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.skip 2");
            sb.AppendLine();

            return sb.ToString();
        }
    }

    public struct MapCharactersSectionEntry
    {
        public int CharacterIndex { get; set; }
        public short FacingDirection { get; set; }
        public short X { get; set; }
        public short Y { get; set; }
        public short TalkScriptBlock { get; set; }
        public short Padding { get; set; }

        public override string ToString()
        {
            return $"{CharacterIndex}: ({FacingDirection}, {X}, {Y}, {TalkScriptBlock}, {Padding})";
        }
    }

    public class Unknown07Section : IEventSection<Unknown07SectionEntry>
    {
        public string Name { get; set; }
        public List<byte> Data { get; set; }
        public int NumObjects { get; set; }
        public int ObjectLength { get; set; }
        public List<Unknown07SectionEntry> Objects { get; set; } = new();
        public Type SectionType { get; set; }
        public Type ObjectType { get; set; }

        public void Initialize(IEnumerable<byte> data, int numObjects, string name, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectLength = 4;
            if (NumObjects != Data.Count / ObjectLength)
            {
                throw new ArgumentException($"{Name} section in event file has mismatch in number of arguments.");
            }
            SectionType = typeof(Unknown07Section);
            ObjectType = typeof(Unknown07SectionEntry);

            for (int i = 0; i < NumObjects; i++)
            {
                Objects.Add(new()
                {
                    UnknownShort1 = BitConverter.ToInt16(data.Skip(i * 4).Take(2).ToArray()),
                    UnknownShort2 = BitConverter.ToInt16(data.Skip(i * 4 + 2).Take(2).ToArray()),
                });
            }
        }

        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        public string GetAsm(int indentation, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
            for (int i = 0; i < NumObjects; i++)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.short {Objects[i].UnknownShort1}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.short {Objects[i].UnknownShort2}");
            }
            sb.AppendLine();

            return sb.ToString();
        }
    }

    public struct Unknown07SectionEntry
    {
        public short UnknownShort1 { get; set; }
        public short UnknownShort2 { get; set; }

        public override string ToString()
        {
            return $"({UnknownShort1}, {UnknownShort2})";
        }
    }

    public class ChoicesSection : IEventSection<ChoicesSectionEntry>
    {
        public string Name { get; set; }
        public List<byte> Data { get; set; }
        public int NumObjects { get; set; }
        public int ObjectLength { get; set; }
        public List<ChoicesSectionEntry> Objects { get; set; } = new();
        public Type SectionType { get; set; }
        public Type ObjectType { get; set; }

        public void Initialize(IEnumerable<byte> data, int numObjects, string name, int offset = -1)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            SectionType = typeof(ChoicesSection);
            ObjectType = typeof(ChoicesSectionEntry);

            for (int i = 0; i < NumObjects; i++)
            {
                int textOffset = BitConverter.ToInt32(data.Skip(i * 20 + 12).Take(4).ToArray()) - offset;
                Objects.Add(new()
                {
                    Id = BitConverter.ToInt32(data.Skip(i * 20).Take(4).ToArray()),
                    Padding1 = BitConverter.ToInt32(data.Skip(i * 20 + 4).Take(4).ToArray()),
                    Padding2 = BitConverter.ToInt32(data.Skip(i * 20 + 8).Take(4).ToArray()),
                    Text = textOffset == 0 ? string.Empty : Encoding.GetEncoding("Shift-JIS").GetString(
                        data.Skip(textOffset).TakeWhile(b => b != 0x00).ToArray()),
                    Padding3 = BitConverter.ToInt32(data.Skip(i * 20 + 16).Take(4).ToArray()),
                });
            }
        }

        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        public string GetAsm(int indentation, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
            for (int i = 0; i < NumObjects; i++)
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
            for (int i = 0; i < NumObjects; i++)
            {
                if (Objects[i].Id > 0)
                {
                    sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}CHOICE{i:D2}: .string \"{Objects[i].Text.EscapeShiftJIS()}\"");
                    Objects[i].Text.AsmPadShiftJISString(sb);
                }
            }
            sb.AppendLine();

            return sb.ToString();
        }
    }

    public struct ChoicesSectionEntry
    {
        public int Id { get; set; }
        public int Padding1 { get; set; }
        public int Padding2 { get; set; }
        public string Text { get; set; }
        public int Padding3 { get; set; }

        public override string ToString()
        {
            return $"{Id}: {Text}";
        }
    }

    public class Unknown08Section : IEventSection<Unknown08SectionEntry>
    {
        public string Name { get; set; }
        public List<byte> Data { get; set; }
        public int NumObjects { get; set; }
        public int ObjectLength { get; set; }
        public List<Unknown08SectionEntry> Objects { get; set; } = new();
        public Type SectionType { get; set; }
        public Type ObjectType { get; set; }

        public void Initialize(IEnumerable<byte> data, int numObjects, string name, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectLength = 16;
            if (NumObjects != Data.Count / ObjectLength)
            {
                throw new ArgumentException($"{Name} section in event file has mismatch in number of arguments.");
            }
            SectionType = typeof(Unknown08Section);
            ObjectType = typeof(Unknown08SectionEntry);

            for (int i = 0; i < NumObjects; i++)
            {
                Objects.Add(new()
                {
                    UnknownInt1 = BitConverter.ToInt32(data.Skip(i * 4).Take(4).ToArray()),
                    UnknownInt2 = BitConverter.ToInt32(data.Skip(i * 4 + 4).Take(4).ToArray()),
                    UnknownInt3 = BitConverter.ToInt32(data.Skip(i * 4 + 8).Take(4).ToArray()),
                    UnknownInt4 = BitConverter.ToInt32(data.Skip(i * 4 + 12).Take(4).ToArray()),
                });
            }
        }

        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        public string GetAsm(int indentation, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
            for (int i = 0; i < NumObjects; i++)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].UnknownInt1}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].UnknownInt2}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].UnknownInt3}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].UnknownInt4}");
            }
            sb.AppendLine();

            return sb.ToString();
        }
    }

    public struct Unknown08SectionEntry
    {
        public int UnknownInt1 { get; set; }
        public int UnknownInt2 { get; set; }
        public int UnknownInt3 { get; set; }
        public int UnknownInt4 { get; set; }

        public override string ToString()
        {
            return $"({UnknownInt1}, {UnknownInt2}, {UnknownInt3}, {UnknownInt4})";
        }
    }

    public class Unknown09Section : IEventSection<Unknown09SectionEntry>
    {
        public string Name { get; set; }
        public List<byte> Data { get; set; }
        public int NumObjects { get; set; }
        public int ObjectLength { get; set; }
        public List<Unknown09SectionEntry> Objects { get; set; } = new();
        public Type SectionType { get; set; }
        public Type ObjectType { get; set; }

        public void Initialize(IEnumerable<byte> data, int numObjects, string name, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectLength = 8;
            if (NumObjects != Data.Count / ObjectLength)
            {
                throw new ArgumentException($"{Name} section in event file has mismatch in number of arguments.");
            }
            SectionType = typeof(Unknown09Section);
            ObjectType = typeof(Unknown09SectionEntry);

            for (int i = 0; i < NumObjects; i++)
            {
                Objects.Add(new()
                {
                    UnknownInt1 = BitConverter.ToInt32(data.Skip(i * 4).Take(4).ToArray()),
                    UnknownInt2 = BitConverter.ToInt32(data.Skip(i * 4 + 4).Take(4).ToArray()),
                });
            }
        }

        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        public string GetAsm(int indentation, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
            for (int i = 0; i < NumObjects; i++)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].UnknownInt1}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].UnknownInt2}");
            }
            sb.AppendLine();

            return sb.ToString();
        }
    }

    public struct Unknown09SectionEntry
    {
        public int UnknownInt1 { get; set; }
        public int UnknownInt2 { get; set; }

        public override string ToString()
        {
            return $"({UnknownInt1}, {UnknownInt2})";
        }
    }

    public class Unknown10Section : IEventSection<Unknown10SectionEntry>
    {
        public string Name { get; set; }
        public List<byte> Data { get; set; }
        public int NumObjects { get; set; }
        public int ObjectLength { get; set; }
        public List<Unknown10SectionEntry> Objects { get; set; } = new();
        public Type SectionType { get; set; }
        public Type ObjectType { get; set; }

        public void Initialize(IEnumerable<byte> data, int numObjects, string name, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectType = typeof(Unknown10SectionEntry);
            ObjectLength = 8;
            if (NumObjects != Data.Count / ObjectLength)
            {
                throw new ArgumentException($"{Name} section in event file has mismatch in number of arguments.");
            }
            SectionType = typeof(Unknown10Section);

            for (int i = 0; i < NumObjects; i++)
            {
                Objects.Add(new()
                {
                    UnknownInt1 = BitConverter.ToInt32(data.Skip(i * 4).Take(4).ToArray()),
                    UnknownInt2 = BitConverter.ToInt32(data.Skip(i * 4 + 4).Take(4).ToArray()),
                });
            }
        }

        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        public string GetAsm(int indentation, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
            for (int i = 0; i < NumObjects; i++)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].UnknownInt1}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].UnknownInt2}");
            }
            sb.AppendLine();

            return sb.ToString();
        }
    }

    public struct Unknown10SectionEntry
    {
        public int UnknownInt1 { get; set; }
        public int UnknownInt2 { get; set; }

        public override string ToString()
        {
            return $"({UnknownInt1}, {UnknownInt2})";
        }
    }

    public class LabelsSection : IEventSection<FlagsSectionEntry>
    {
        public string Name { get; set; }
        public List<byte> Data { get; set; }
        public int NumObjects { get; set; }
        public int ObjectLength { get; set; }
        public List<FlagsSectionEntry> Objects { get; set; } = new();
        public Type SectionType { get; set; }
        public Type ObjectType { get; set; }

        public void Initialize(IEnumerable<byte> data, int numObjects, string name, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            SectionType = typeof(LabelsSection);
            ObjectType = typeof(FlagsSectionEntry);

            for (int i = 0; i < NumObjects; i++)
            {
                int nameIndex = BitConverter.ToInt32(data.Skip(i * 8 + 4).Take(4).ToArray()) - offset;
                Objects.Add(new()
                {
                    Id = BitConverter.ToInt16(data.Skip(i * 8 + 2).Take(2).ToArray()),
                    Name = nameIndex > 0 ? Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(nameIndex).TakeWhile(b => b != 0x00).ToArray()) : string.Empty,
                });
            }
        }

        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        public string GetAsm(int indentation, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
            for (int i = 0; i < NumObjects; i++)
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
            for (int i = 0; i < NumObjects; i++)
            {
                if (Objects[i].Id > 0)
                {
                    sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}LABEL{i:D2}: .string \"{Objects[i].Name}\"");
                    Objects[i].Name.AsmPadShiftJISString(sb);
                }
            }
            sb.AppendLine();

            return sb.ToString();
        }
    }

    public struct FlagsSectionEntry
    {
        public short Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return $"{Id}: {Name}";
        }
    }

    public class DramatisPersonaeSection : IEventSection<string>
    {
        public string Name { get; set; }
        public List<byte> Data { get; set; }
        public int NumObjects { get; set; }
        public int ObjectLength { get; set; }
        public List<string> Objects { get; set; } = new();
        public Type SectionType { get; set; }
        public Type ObjectType { get; set; }
        public int Offset { get; set; }
        public int Index { get; set; }

        public void Initialize(IEnumerable<byte> data, int numObjects, string name, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            SectionType = typeof(DramatisPersonaeSection);
            ObjectType = typeof(string);
            Offset = offset;

            Objects.Add(Encoding.GetEncoding("Shift-JIS").GetString(data.TakeWhile(b => b != 0x00).ToArray()));
        }

        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        public string GetAsm(int indentation, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.string \"{Objects[0].EscapeShiftJIS()}\"");
            int neededPadding = 4 - Encoding.GetEncoding("Shift-JIS").GetByteCount(Objects[0]) % 4 - 1;
            Objects[0].AsmPadShiftJISString(sb);
            sb.AppendLine();

            return sb.ToString();
        }
    }

    public class DialogueSection : IEventSection<DialogueLine>
    {
        public string Name { get; set; }
        public List<byte> Data { get; set; }
        public int NumObjects { get; set; }
        public int ObjectLength { get; set; }
        public List<DialogueLine> Objects { get; set; } = new();
        public Type SectionType { get; set; }
        public Type ObjectType { get; set; }

        public void Initialize(IEnumerable<byte> data, int numObjects, string name, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            SectionType = typeof(DialogueSection);
            ObjectType = typeof(DialogueLine);

            int realLines = 0;
            for (int i = 0; i < NumObjects; i++)
            {
                int dramatisPersonaeOffset = BitConverter.ToInt32(data.Skip(offset + i * 12 + 4).Take(4).ToArray());
                int dialoguePointer = BitConverter.ToInt32(data.Skip(offset + i * 12 + 8).Take(4).ToArray());
                Objects.Add(new((Speaker)BitConverter.ToInt32(data.Skip(offset + i * 12).Take(4).ToArray()),
                    Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(dramatisPersonaeOffset).TakeWhile(b => b != 0x00).ToArray()),
                    dramatisPersonaeOffset,
                    dialoguePointer,
                    data.ToArray())
                { CorrectedIndex = realLines });
                if (dialoguePointer > 0)
                {
                    realLines++;
                }
            }
        }

        public void InitializeDramatisPersonaeIndices(List<DramatisPersonaeSection> dramatisPersonae)
        {
            for (int i = 0; i < Objects.Count; i++)
            {
                if ((int)Objects[i].Speaker > 0)
                {
                    Objects[i].SpeakerIndex = dramatisPersonae.First(d => d.Offset == Objects[i].SpeakerPointer).Index;
                }
            }
        }

        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        public string GetAsm(int indentation, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
            for (int i = 0; i < NumObjects; i++)
            {
                int speakerInt = (int)Objects[i].Speaker;
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.int {speakerInt}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(speakerInt > 0 ? $"POINTER{currentPointer++}: .word DRAMATISPERSONAE{Objects[i].SpeakerIndex}" : ".word 0")}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(speakerInt > 0 && Objects[i].Pointer > 0 ? $"POINTER{currentPointer++}: .word DIALOGUELINE{i:D3}" : ".word 0")}");
            }
            for (int i = 0; i < NumObjects; i++)
            {
                if ((int)Objects[i].Speaker > 0 && Objects[i].Pointer > 0)
                {
                    sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}DIALOGUELINE{i:D3}: .string \"{Objects[i].Text.EscapeShiftJIS()}\"");
                    Objects[i].Text.AsmPadShiftJISString(sb);
                }
            }
            sb.AppendLine();

            return sb.ToString();
        }
    }

    public class ConditionalSection : IEventSection<string>
    {
        public string Name { get; set; }
        public List<byte> Data { get; set; }
        public int NumObjects { get; set; }
        public int ObjectLength { get; set; }
        public List<string> Objects { get; set; } = new();
        public Type SectionType { get; set; }
        public Type ObjectType { get; set; }

        public void Initialize(IEnumerable<byte> data, int numObjects, string name, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            SectionType = typeof(ConditionalSection);
            ObjectType = typeof(string);

            for (int i = 0; i < NumObjects; i++)
            {
                int pointer = BitConverter.ToInt32(Data.Skip(offset + i * 4).Take(4).ToArray());
                if (pointer > 0)
                {
                    Objects.Add(Encoding.ASCII.GetString(Data.Skip(pointer).TakeWhile(b => b != 0).ToArray()));
                }
                else
                {
                    Objects.Add(null);
                }
            }
        }

        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        public string GetAsm(int indentation, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
            for (int i = 0; i < NumObjects; i++)
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
            for (int i = 0; i < NumObjects; i++)
            {
                if (!string.IsNullOrEmpty(Objects[i]))
                {
                    sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}CONDITIONAL_{i:D2}: .string \"{Objects[i]}\"");
                    Objects[i].AsmPadShiftJISString(sb);
                }
            }
            sb.AppendLine();

            return sb.ToString();
        }
    }

    public class ScriptSectionDefinitionsSection : IEventSection<ScriptSectionDefinition>
    {
        public string Name { get; set; }
        public List<byte> Data { get; set; }
        public int NumObjects { get; set; }
        public int ObjectLength { get; set; }
        public List<string> Labels { get; set; }
        public List<ScriptSectionDefinition> Objects { get; set; } = new();
        public Type SectionType { get; set; }
        public Type ObjectType { get; set; }

        public void Initialize(IEnumerable<byte> data, int numObjects, string name, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            SectionType = typeof(ScriptSectionDefinitionsSection);
            ObjectType = typeof(ScriptSectionDefinition);
            ObjectLength = 8;

            for (int i = 0; i < NumObjects; i++)
            {
                Objects.Add(new()
                {
                    Name = string.IsNullOrEmpty(Labels[i]) ? $"SCRIPT{i:D2}" : Labels[i],
                    NumCommands = BitConverter.ToInt32(Data.Skip(i * ObjectLength).Take(4).ToArray()),
                    Pointer = BitConverter.ToInt32(Data.Skip(i * ObjectLength + 4).Take(4).ToArray())
                });
            }
        }

        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        public string GetAsm(int indentation, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{Helpers.Indent(indentation)}{Name}:");
            for (int i = 0; i < Objects.Count; i++)
            {
                sb.AppendLine($"{Helpers.Indent(indentation + 3)}.word {Objects[i].NumCommands}");
                if (Objects[i].Pointer > 0)
                {
                    sb.AppendLine($"{Helpers.Indent(indentation + 3)}POINTER{currentPointer++}: .word {Objects[i].Name}");
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

    public struct ScriptSectionDefinition
    {
        public string Name { get; set; }
        public int NumCommands { get; set; }
        public int Pointer { get; set; }
    }

    public class ScriptSection : IEventSection<ScriptCommandInvocation>
    {
        public string Name { get; set; }
        public List<byte> Data { get; set; }
        public int NumObjects { get; set; }
        public int ObjectLength { get; set; }
        public List<ScriptCommandInvocation> Objects { get; set; } = new();
        public Type SectionType { get; set; }
        public Type ObjectType { get; set; }
        public List<ScriptCommand> CommandsAvailable { get; set; }

        public void Initialize(IEnumerable<byte> data, int numObjects, string name, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            SectionType = typeof(ScriptSection);
            ObjectType = typeof(ScriptCommandInvocation);
            ObjectLength = 0x24;

            for (int i = 0; i < NumObjects; i++)
            {
                Objects.Add(new(Data.Skip(i * 0x24).Take(0x24), CommandsAvailable));
            }
        }

        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }
        public string GetAsm(int indentation, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");

            for (int i = 0; i < NumObjects; i++)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{Objects[i].Command.Mnemonic} {string.Join(", ", Objects[i].Parameters.Take(Objects[i].Command.Parameters.Length))}");
            }
            sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.skip 0x24");
            sb.AppendLine();

            return sb.ToString();
        }
    }

    public class EventNameSection : IEventSection<string>
    {
        public string Name { get; set; }
        public List<byte> Data { get; set; }
        public int NumObjects { get; set; }
        public int ObjectLength { get; set; }
        public List<string> Objects { get; set; } = new();
        public Type SectionType { get; set; }
        public Type ObjectType { get; set; }

        public void Initialize(IEnumerable<byte> data, int numObjects, string name, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            SectionType = typeof(EventNameSection);
            ObjectType = typeof(string);

            Objects.Add(Encoding.ASCII.GetString(data.ToArray()));
        }

        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        public string GetAsm(int indentation, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}: .string \"{Objects[0]}\"");
            Objects[0].AsmPadShiftJISString(sb);
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
