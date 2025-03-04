// These files are taken from Nitro Studio 2
// https://github.com/Gota7/NitroStudio2
// They have been modified to work with .NET 6.0
// Nitro Studio 2 is created by Gota 7
// It is not explicitly licensed, but given that its
// components are licensed under GPLv3, we can assume
// it is also GPLv3 compatible
using GotaSoundIO;
using GotaSoundIO.IO;
using GotaSoundIO.Sound;
using System.Collections.Generic;

namespace HaruhiChokuretsuLib.Audio.SDAT
{
    /// <summary>
    /// Wave archive.
    /// </summary>
    public class WaveArchive : IOFile
    {
        /// <summary>
        /// Waves.
        /// </summary>
        public List<Wave> Waves = [];

        /// <summary>
        /// Read the wave archive.
        /// </summary>
        /// <param name="r">The reader.</param>
        public override void Read(FileReader r)
        {
            //Open data.
            r.OpenFile<NHeader>(out _);
            r.OpenBlock(0, out _, out _, false);
            r.ReadUInt32();
            uint size = r.ReadUInt32();
            r.ReadUInt32s(8);
            var offs = r.Read<Table<uint>>();
            Waves = [];
            for (int i = 0; i < offs.Count; i++)
            {
                uint len;
                if (i == offs.Count - 1)
                {
                    len = size - (offs[i] - 0x10);
                }
                else
                {
                    len = offs[i + 1] - offs[i];
                }
                r.Jump(offs[i], true);
                Waves.Add(Wave.ReadShortened(r, len));
            }
        }

        /// <summary>
        /// Write the wave archive.
        /// </summary>
        /// <param name="w">The writer.</param>
        public override void Write(FileWriter w)
        {
            //Write the wave archive.
            w.InitFile<NHeader>("SWAR", ByteOrder.LittleEndian, null, 1);
            w.InitBlock("DATA");
            w.Write(new uint[8]);
            w.Write((uint)Waves.Count);
            long bak = w.Position;
            w.Write(new uint[Waves.Count]);
            for (int i = 0; i < Waves.Count; i++)
            {
                long bak2 = w.Position;
                w.Position = bak + i * 4;
                w.Write((uint)(bak2 - w.FileOffset));
                w.Position = bak2;
                Waves[i].WriteShortened(w);
            }
            w.Pad(4);
            w.CloseBlock();
            w.CloseFile();
        }

        /// <summary>
        /// Get the waves.
        /// </summary>
        /// <returns>The wave files.</returns>
        public RiffWave[] GetWaves()
        {
            RiffWave[] w = new RiffWave[Waves.Count];
            for (int i = 0; i < Waves.Count; i++)
            {
                w[i] = new();
                w[i].FromOtherStreamFile(Waves[i]);
            }
            return w;
        }
    }
}
