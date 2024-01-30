using HaruhiChokuretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Event
{
    /// <summary>
    /// A (fake) generic section used for generic operations
    /// </summary>
    public class GenericSection : IEventSection<object>, IConvertible
    {
        /// <inheritdoc/>
        public string Name { get; set; }
        /// <inheritdoc/>
        public List<byte> Data { get; set; }
        /// <inheritdoc/>
        public int NumObjects { get; set; }
        /// <inheritdoc/>
        public int ObjectLength { get; set; }
        /// <inheritdoc/>
        public List<object> Objects { get; set; }
        /// <inheritdoc/>
        public Type SectionType { get; set; }
        /// <inheritdoc/>
        public Type ObjectType { get; set; }

        /// <inheritdoc/>
        public void Initialize(IEnumerable<byte> data, int numObjects, string name, ILogger log, int offset)
        {
        }

        /// <inheritdoc/>
        public IEventSection<object> GetGeneric()
        {
            return this;
        }

        /// <inheritdoc/>
        public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
        {
            return string.Empty;
        }

        /// <inheritdoc/>
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

        /// <summary>
        /// Converts a generic section to a specific section type
        /// </summary>
        /// <param name="conversionType">The type to convert to</param>
        /// <param name="provider">Unused</param>
        /// <returns>The converted type</returns>
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
            else if (conversionType == typeof(InteractableObjectsSection))
            {
                return new InteractableObjectsSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Select(o => (InteractableObjectEntry)Convert.ChangeType(o, ObjectType)).ToList(), SectionType = SectionType, ObjectType = ObjectType };
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
                return new LabelsSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Select(o => (LabelsSectionEntry)Convert.ChangeType(o, ObjectType)).ToList(), SectionType = SectionType, ObjectType = ObjectType };
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

    /// <summary>
    /// The settings section of the event file
    /// </summary>
    public class SettingsSection : IEventSection<EventFileSettings>
    {
        /// <inheritdoc/>
        public string Name { get; set; }
        /// <inheritdoc/>
        public List<byte> Data { get; set; }
        /// <inheritdoc/>
        public int NumObjects { get; set; }
        /// <inheritdoc/>
        public int ObjectLength { get; set; }
        /// <inheritdoc/>
        public List<EventFileSettings> Objects { get; set; } = [];
        /// <inheritdoc/>
        public Type SectionType { get; set; }
        /// <inheritdoc/>
        public Type ObjectType { get; set; }

        /// <inheritdoc/>
        public void Initialize(IEnumerable<byte> data, int numObjects, string name, ILogger log, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectLength = 0x128;
            SectionType = typeof(SettingsSection);
            ObjectType = typeof(EventFileSettings);

            Objects = [new(data)];
        }

        /// <inheritdoc/>
        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        /// <inheritdoc/>
        public string GetAsm(int indentation, ref int currentPointer, EventFile evt)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
            for (int i = 0; i < NumObjects; i++)
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
        public List<byte> Data { get; set; }
        /// <inheritdoc/>
        public int NumObjects { get; set; }
        /// <inheritdoc/>
        public int ObjectLength { get; set; }
        /// <inheritdoc/>
        public List<PointerStruct> Objects { get; set; } = [];
        /// <inheritdoc/>
        public Type SectionType { get; set; }
        /// <inheritdoc/>
        public Type ObjectType { get; set; }

        /// <inheritdoc/>
        public void Initialize(IEnumerable<byte> data, int numObjects, string name, ILogger log, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectLength = 12;
            if (NumObjects != Data.Count / ObjectLength)
            {
                log.LogError($"{Name} section in event file has mismatch in number of arguments.");
                return;
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

        internal static (int sectionIndex, PointerSection section) ParseSection(List<EventFileSection> eventFileSections, int pointer, string name, IEnumerable<byte> data, ILogger log)
        {
            int sectionIndex = eventFileSections.FindIndex(s => s.Pointer == pointer);
            PointerSection section = new();
            section.Initialize(data.Skip(eventFileSections[sectionIndex].Pointer)
                .Take(eventFileSections[sectionIndex + 1].Pointer - eventFileSections[sectionIndex].Pointer),
                eventFileSections[sectionIndex].ItemCount,
                $"{name}_POINTER", log, eventFileSections[sectionIndex].Pointer);
            return (sectionIndex, section);
        }

        internal static PointerSection GetForSection<T>(IEventSection<T> section)
        {
            return new() 
            { 
                Name = $"{section.Name}_POINTER",
                NumObjects = 1,
                ObjectLength = 12,
                SectionType = typeof(PointerSection),
                ObjectType = typeof(PointerStruct),
                Objects =
                [
                    new() { Pointer = 1, Padding2 = section.Objects.Count - 1 }, // doesn't matter since we're only creating this for the ASM output,
                ]
            };
        }

        /// <inheritdoc/>
        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        /// <inheritdoc/>
        public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
            for (int i = 0; i < NumObjects; i++)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].Padding1}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{(Objects[i].Pointer == 0 ? ".word 0" : $"POINTER{currentPointer++}: .word {Name[0..Name.IndexOf('_')]}")}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].Padding2}");
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
        /// Padding
        /// </summary>
        public int Padding2 { get; set; }

        /// <inheritdoc/>
        public override readonly string ToString()
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
        public List<byte> Data { get; set; }
        /// <inheritdoc/>
        public int NumObjects { get; set; }
        /// <inheritdoc/>
        public int ObjectLength { get; set; }
        /// <inheritdoc/>
        public List<int> Objects { get; set; } = [];
        /// <inheritdoc/>
        public Type SectionType { get; set; }
        /// <inheritdoc/>
        public Type ObjectType { get; set; }

        /// <inheritdoc/>
        public void Initialize(IEnumerable<byte> data, int numObjects, string name, ILogger log, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectLength = 4;
            if (NumObjects != Data.Count / ObjectLength)
            {
                log.LogError($"{Name} section in event file has mismatch in number of arguments.");
                return;
            }
            SectionType = typeof(IntegerSection);
            ObjectType = typeof(int);

            for (int i = 0; i < NumObjects; i++)
            {
                Objects.Add(BitConverter.ToInt32(data.Skip(i * 4).Take(4).ToArray()));
            }
        }

        /// <inheritdoc/>
        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
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
        public List<byte> Data { get; set; }
        /// <inheritdoc/>
        public int NumObjects { get; set; }
        /// <inheritdoc/>
        public int ObjectLength { get; set; }
        /// <inheritdoc/>
        public List<InteractableObjectEntry> Objects { get; set; } = [];
        /// <inheritdoc/>
        public Type SectionType { get; set; }
        /// <inheritdoc/>
        public Type ObjectType { get; set; }

        /// <inheritdoc/>
        public void Initialize(IEnumerable<byte> data, int numObjects, string name, ILogger log, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectLength = 6;
            if (NumObjects != Data.Count / ObjectLength)
            {
                log.LogError($"{Name} section in event file has mismatch in number of arguments.");
                return;
            }
            SectionType = typeof(InteractableObjectsSection);
            ObjectType = typeof(InteractableObjectEntry);

            for (int i = 0; i < NumObjects; i++)
            {
                Objects.Add(new()
                {
                    ObjectId = BitConverter.ToInt16(data.Skip(i * 6).Take(2).ToArray()),
                    ScriptBlock = BitConverter.ToInt16(data.Skip(i * 6 + 2).Take(2).ToArray()),
                    Padding = BitConverter.ToInt16(data.Skip(i * 6 + 4).Take(2).ToArray()),
                });
            }
        }

        /// <inheritdoc/>
        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        /// <inheritdoc/>
        public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
            for (int i = 0; i < Objects.Count; i++)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.short {Objects[i].ObjectId}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.short {Objects[i].ScriptBlock}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.short {Objects[i].Padding}");
            }
            if (Objects.Count * ObjectLength % 4 > 0)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.skip {4 - NumObjects * ObjectLength % 4}");
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
        public List<byte> Data { get; set; }
        /// <inheritdoc/>
        public int NumObjects { get; set; }
        /// <inheritdoc/>
        public int ObjectLength { get; set; }
        /// <inheritdoc/>
        public List<Unknown03SectionEntry> Objects { get; set; } = [];
        /// <inheritdoc/>
        public Type SectionType { get; set; }
        /// <inheritdoc/>
        public Type ObjectType { get; set; }

        /// <inheritdoc/>
        public void Initialize(IEnumerable<byte> data, int numObjects, string name, ILogger log, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectLength = 12;
            if (NumObjects != Data.Count / ObjectLength)
            {
                log.LogError($"{Name} section in event file has mismatch in number of arguments.");
                return;
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

        /// <inheritdoc/>
        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        /// <inheritdoc/>
        public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
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
        public List<byte> Data { get; set; }
        /// <inheritdoc/>
        public int NumObjects { get; set; }
        /// <inheritdoc/>
        public int ObjectLength { get; set; }
        /// <inheritdoc/>
        public List<StartingChibiEntry> Objects { get; set; } = [];
        /// <inheritdoc/>
        public Type SectionType { get; set; }
        /// <inheritdoc/>
        public Type ObjectType { get; set; }

        /// <inheritdoc/>
        public void Initialize(IEnumerable<byte> data, int numObjects, string name, ILogger log, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectLength = 12;
            if (NumObjects != Data.Count / ObjectLength)
            {
                log.LogError($"{Name} section in event file has mismatch in number of arguments.");
                return;
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

        /// <inheritdoc/>
        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        /// <inheritdoc/>
        public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
            for (int i = 0; i < Objects.Count; i++)
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
        public List<byte> Data { get; set; }
        /// <inheritdoc/>
        public int NumObjects { get; set; }
        /// <inheritdoc/>
        public int ObjectLength { get; set; }
        /// <inheritdoc/>
        public List<MapCharactersSectionEntry> Objects { get; set; } = [];
        /// <inheritdoc/>
        public Type SectionType { get; set; }
        /// <inheritdoc/>
        public Type ObjectType { get; set; }

        /// <inheritdoc/>
        public void Initialize(IEnumerable<byte> data, int numObjects, string name, ILogger log, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectLength = 14;
            if (NumObjects != Data.Count / ObjectLength)
            {
                log.LogError($"{Name} section in event file has mismatch in number of arguments.");
                return;
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

        /// <inheritdoc/>
        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        /// <inheritdoc/>
        public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");
            for (int i = 0; i < Objects.Count; i++)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.word {Objects[i].CharacterIndex}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {Objects[i].FacingDirection}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {Objects[i].X}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {Objects[i].Y}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {Objects[i].TalkScriptBlock}");
                sb.AppendLine($"{string.Join(' ', new string[indentation + 7])}.short {Objects[i].Padding}");
            }
            if (Objects.Count * ObjectLength % 4 > 0)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}.skip {4 - Objects.Count * ObjectLength % 4}");
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
        public List<byte> Data { get; set; }
        /// <inheritdoc/>
        public int NumObjects { get; set; }
        /// <inheritdoc/>
        public int ObjectLength { get; set; }
        /// <inheritdoc/>
        public List<Unknown07SectionEntry> Objects { get; set; } = [];
        /// <inheritdoc/>
        public Type SectionType { get; set; }
        /// <inheritdoc/>
        public Type ObjectType { get; set; }

        /// <inheritdoc/>
        public void Initialize(IEnumerable<byte> data, int numObjects, string name, ILogger log, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectLength = 4;
            if (NumObjects != Data.Count / ObjectLength)
            {
                log.LogError($"{Name} section in event file has mismatch in number of arguments.");
                return;
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

        /// <inheritdoc/>
        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        /// <inheritdoc/>
        public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
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
        public List<byte> Data { get; set; }
        /// <inheritdoc/>
        public int NumObjects { get; set; }
        /// <inheritdoc/>
        public int ObjectLength { get; set; }
        /// <inheritdoc/>
        public List<ChoicesSectionEntry> Objects { get; set; } = [];
        /// <inheritdoc/>
        public Type SectionType { get; set; }
        /// <inheritdoc/>
        public Type ObjectType { get; set; }

        /// <inheritdoc/>
        public void Initialize(IEnumerable<byte> data, int numObjects, string name, ILogger log, int offset = -1)
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

        /// <inheritdoc/>
        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
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
        public List<byte> Data { get; set; }
        /// <inheritdoc/>
        public int NumObjects { get; set; }
        /// <inheritdoc/>
        public int ObjectLength { get; set; }
        /// <inheritdoc/>
        public List<Unknown08SectionEntry> Objects { get; set; } = [];
        /// <inheritdoc/>
        public Type SectionType { get; set; }
        /// <inheritdoc/>
        public Type ObjectType { get; set; }

        /// <inheritdoc/>
        public void Initialize(IEnumerable<byte> data, int numObjects, string name, ILogger log, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectLength = 16;
            if (NumObjects != Data.Count / ObjectLength)
            {
                log.LogError($"{Name} section in event file has mismatch in number of arguments.");
                return;
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

        /// <inheritdoc/>
        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        /// <inheritdoc/>
        public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
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
        public List<byte> Data { get; set; }
        /// <inheritdoc/>
        public int NumObjects { get; set; }
        /// <inheritdoc/>
        public int ObjectLength { get; set; }
        /// <inheritdoc/>
        public List<Unknown09SectionEntry> Objects { get; set; } = [];
        /// <inheritdoc/>
        public Type SectionType { get; set; }
        /// <inheritdoc/>
        public Type ObjectType { get; set; }

        /// <inheritdoc/>
        public void Initialize(IEnumerable<byte> data, int numObjects, string name, ILogger log, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectLength = 8;
            if (NumObjects != Data.Count / ObjectLength)
            {
                log.LogError($"{Name} section in event file has mismatch in number of arguments.");
                return;
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

        /// <inheritdoc/>
        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        /// <inheritdoc/>
        public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
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
        public List<byte> Data { get; set; }
        /// <inheritdoc/>
        public int NumObjects { get; set; }
        /// <inheritdoc/>
        public int ObjectLength { get; set; }
        /// <inheritdoc/>
        public List<Unknown10SectionEntry> Objects { get; set; } = [];
        /// <inheritdoc/>
        public Type SectionType { get; set; }
        /// <inheritdoc/>
        public Type ObjectType { get; set; }

        /// <inheritdoc/>
        public void Initialize(IEnumerable<byte> data, int numObjects, string name, ILogger log, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            ObjectType = typeof(Unknown10SectionEntry);
            ObjectLength = 8;
            if (NumObjects != Data.Count / ObjectLength)
            {
                log.LogError($"{Name} section in event file has mismatch in number of arguments.");
                return;
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

        /// <inheritdoc/>
        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        /// <inheritdoc/>
        public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
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
        public List<byte> Data { get; set; }
        /// <inheritdoc/>
        public int NumObjects { get; set; }
        /// <inheritdoc/>
        public int ObjectLength { get; set; }
        /// <inheritdoc/>
        public List<LabelsSectionEntry> Objects { get; set; } = [];
        /// <inheritdoc/>
        public Type SectionType { get; set; }
        /// <inheritdoc/>
        public Type ObjectType { get; set; }

        /// <inheritdoc/>
        public void Initialize(IEnumerable<byte> data, int numObjects, string name, ILogger log, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            SectionType = typeof(LabelsSection);
            ObjectType = typeof(LabelsSectionEntry);

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

        /// <inheritdoc/>
        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
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
    /// </summary>
    public class DramatisPersonaeSection : IEventSection<string>
    {
        /// <inheritdoc/>
        public string Name { get; set; }
        /// <inheritdoc/>
        public List<byte> Data { get; set; }
        /// <inheritdoc/>
        public int NumObjects { get; set; }
        /// <inheritdoc/>
        public int ObjectLength { get; set; }
        /// <inheritdoc/>
        public List<string> Objects { get; set; } = [];
        /// <inheritdoc/>
        public Type SectionType { get; set; }
        /// <inheritdoc/>
        public Type ObjectType { get; set; }
        /// <inheritdoc/>
        public int Offset { get; set; }
        /// <inheritdoc/>
        public int Index { get; set; }

        /// <inheritdoc/>
        public void Initialize(IEnumerable<byte> data, int numObjects, string name, ILogger log, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            SectionType = typeof(DramatisPersonaeSection);
            ObjectType = typeof(string);
            Offset = offset;

            Objects.Add(Encoding.GetEncoding("Shift-JIS").GetString(data.TakeWhile(b => b != 0x00).ToArray()));
        }

        /// <inheritdoc/>
        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
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
        public List<byte> Data { get; set; }
        /// <inheritdoc/>
        public int NumObjects { get; set; }
        /// <inheritdoc/>
        public int ObjectLength { get; set; }
        /// <inheritdoc/>
        public List<DialogueLine> Objects { get; set; } = [];
        /// <inheritdoc/>
        public Type SectionType { get; set; }
        /// <inheritdoc/>
        public Type ObjectType { get; set; }

        /// <inheritdoc/>
        public void Initialize(IEnumerable<byte> data, int numObjects, string name, ILogger log, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            SectionType = typeof(DialogueSection);
            ObjectType = typeof(DialogueLine);

            for (int i = 0; i < NumObjects; i++)
            {
                int dramatisPersonaeOffset = BitConverter.ToInt32(data.Skip(offset + i * 12 + 4).Take(4).ToArray());
                int dialoguePointer = BitConverter.ToInt32(data.Skip(offset + i * 12 + 8).Take(4).ToArray());
                Objects.Add(new((Speaker)BitConverter.ToInt32(data.Skip(offset + i * 12).Take(4).ToArray()),
                    Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(dramatisPersonaeOffset).TakeWhile(b => b != 0x00).ToArray()),
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
            for (int i = 0; i < NumObjects; i++)
            {
                if ((int)Objects[i].Speaker > 0)
                {
                    Objects[i].SpeakerIndex = dramatisPersonae.First(d => d.Offset == Objects[i].SpeakerPointer).Index;
                }
            }
        }

        /// <inheritdoc/>
        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
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
        public List<byte> Data { get; set; }
        /// <inheritdoc/>
        public int NumObjects { get; set; }
        /// <inheritdoc/>
        public int ObjectLength { get; set; }
        /// <inheritdoc/>
        public List<string> Objects { get; set; } = [];
        /// <inheritdoc/>
        public Type SectionType { get; set; }
        /// <inheritdoc/>
        public Type ObjectType { get; set; }

        /// <inheritdoc/>
        public void Initialize(IEnumerable<byte> data, int numObjects, string name, ILogger log, int offset)
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

        /// <inheritdoc/>
        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
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
        /// <inheritdoc/>
        public List<byte> Data { get; set; }
        /// <inheritdoc/>
        public int NumObjects { get; set; }
        /// <inheritdoc/>
        public int ObjectLength { get; set; }
        /// <inheritdoc/>
        public List<string> Labels { get; set; }
        /// <inheritdoc/>
        public List<ScriptSectionDefinition> Objects { get; set; } = [];
        /// <inheritdoc/>
        public Type SectionType { get; set; }
        /// <inheritdoc/>
        public Type ObjectType { get; set; }

        /// <inheritdoc/>
        public void Initialize(IEnumerable<byte> data, int numObjects, string name, ILogger log, int offset)
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

        /// <inheritdoc/>
        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        /// <inheritdoc/>
        public string GetAsm(int indentation, ref int currentPointer, EventFile evt)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{Helpers.Indent(indentation)}{Name}:");
            for (int i = 0; i < evt.ScriptSections.Count; i++)
            {
                sb.AppendLine($"{Helpers.Indent(indentation + 3)}.word {evt.ScriptSections[i].Objects.Count}");
                if (evt.ScriptSections[i].Objects.Count > 0)
                {
                    sb.AppendLine($"{Helpers.Indent(indentation + 3)}POINTER{currentPointer++}: .word {evt.ScriptSections[i].Name}");
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
        public List<byte> Data { get; set; }
        /// <inheritdoc/>
        public int NumObjects { get; set; }
        /// <inheritdoc/>
        public int ObjectLength { get; set; }
        /// <inheritdoc/>
        public List<ScriptCommandInvocation> Objects { get; set; } = [];
        /// <inheritdoc/>
        public Type SectionType { get; set; }
        /// <inheritdoc/>
        public Type ObjectType { get; set; }
        /// <inheritdoc/>
        public List<ScriptCommand> CommandsAvailable { get; set; }

        /// <inheritdoc/>
        public void Initialize(IEnumerable<byte> data, int numObjects, string name, ILogger log, int offset)
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

        /// <inheritdoc/>
        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
        }

        /// <inheritdoc/>
        public string GetAsm(int indentation, ref int currentPointer, EventFile evt = null)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{string.Join(' ', new string[indentation])}{Name}:");

            for (int i = 0; i < Objects.Count; i++)
            {
                sb.AppendLine($"{string.Join(' ', new string[indentation + 4])}{Objects[i].Command.Mnemonic} {string.Join(", ", Objects[i].Parameters.Take(Objects[i].Command.Parameters.Length))}");
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
        public List<byte> Data { get; set; }
        /// <inheritdoc/>
        public int NumObjects { get; set; }
        /// <inheritdoc/>
        public int ObjectLength { get; set; }
        /// <inheritdoc/>
        public List<string> Objects { get; set; } = [];
        /// <inheritdoc/>
        public Type SectionType { get; set; }
        /// <inheritdoc/>
        public Type ObjectType { get; set; }

        /// <inheritdoc/>
        public void Initialize(IEnumerable<byte> data, int numObjects, string name, ILogger log, int offset)
        {
            Name = name;
            Data = data.ToList();
            NumObjects = numObjects;
            SectionType = typeof(EventNameSection);
            ObjectType = typeof(string);

            Objects.Add(Encoding.ASCII.GetString(data.ToArray()));
        }

        /// <inheritdoc/>
        public IEventSection<object> GetGeneric()
        {
            return new GenericSection() { Name = Name, Data = Data, NumObjects = NumObjects, ObjectLength = ObjectLength, Objects = Objects.Cast<object>().ToList(), SectionType = SectionType, ObjectType = ObjectType };
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
}
