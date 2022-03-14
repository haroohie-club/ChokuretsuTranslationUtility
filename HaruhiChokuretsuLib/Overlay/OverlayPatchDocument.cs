using System.Xml.Serialization;

namespace HaruhiChokuretsuLib.Overlay
{
    public class OverlayPatchDocument
    {
        [XmlArray("overlays")]
        public OverlayXml[] Overlays { get; set; }
    }

    [XmlType("overlay")]
    public class OverlayXml
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlElement("start")]
        public string start;
        [XmlElement("append")]
        public string appendFunction;
        [XmlIgnore]
        public uint Start { get => uint.Parse(start, System.Globalization.NumberStyles.HexNumber); set => start = $"{value:X8}"; }
        [XmlArray("patches")]
        public OverlayPatchXml[] Patches { get; set; }
        [XmlIgnore]
        public byte[] AppendFunction { get => Helpers.ByteArrayFromString(appendFunction); set => appendFunction = Helpers.StringFromByteArray(value); }
    }

    [XmlType("replace")]
    public class OverlayPatchXml
    {
        [XmlAttribute("location")]
        public string location;
        [XmlAttribute("value")]
        public string patch;

        [XmlIgnore]
        public uint Location { get => uint.Parse(location, System.Globalization.NumberStyles.HexNumber); set => location = $"{value:X8}"; }
        [XmlIgnore]
        public byte[] Value { get => Helpers.ByteArrayFromString(patch); set => patch = Helpers.StringFromByteArray(value); }
    }
}
