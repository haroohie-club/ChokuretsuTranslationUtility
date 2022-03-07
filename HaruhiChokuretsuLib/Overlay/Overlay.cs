using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace HaruhiChokuretsuLib.Overlay
{
    public class Overlay
    {
        public string Name { get; set; }
        public int Id { get => int.Parse(Name.Substring(Name.Length - 4), System.Globalization.NumberStyles.HexNumber); }
        public List<byte> Data { get; set; }

        public Overlay(string file)
        {
            Name = Path.GetFileNameWithoutExtension(file);
            Data = File.ReadAllBytes(file).ToList();
        }

        public void Save(string file)
        {
            File.WriteAllBytes(file, Data.ToArray());
        }

        public void Patch(int loc, byte[] patchData)
        {
            Data.RemoveRange(loc, patchData.Length);
            Data.InsertRange(loc, patchData);
        }

        public void Append(byte[] appendData, string ndsProjectFile)
        {
            Data.AddRange(appendData);
            XDocument ndsProjectFileDocument = XDocument.Load(ndsProjectFile);
            Console.WriteLine($"Expanding RAM size in overlay table for overlay {Id}...");
            var overlayTableEntry = ndsProjectFileDocument.Root.Element("RomInfo").Element("ARM9Ovt").Elements()
                .First(o => o.Attribute("Id").Value == $"{Id}");
            overlayTableEntry.Element("RamSize").Value = $"{Data.Count}";
            ndsProjectFileDocument.Save(ndsProjectFile);
        }
    }
}
