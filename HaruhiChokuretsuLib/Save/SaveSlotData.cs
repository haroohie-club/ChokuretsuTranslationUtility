using HaruhiChokuretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiChokuretsuLib.Save
{
    /// <summary>
    /// Represents a save slot (either dynamic or static)
    /// </summary>
    public class SaveSlotData : SaveSection
    {
        /// <summary>
        /// The time the game was saved (cannot be before year 2000 or after year 2255)
        /// </summary>
        public DateTimeOffset SaveTime { get; set; }
        /// <summary>
        /// The 1-indexed position of the scenario command the save is at
        /// </summary>
        public short ScenarioPosition { get; set; }
        /// <summary>
        /// The episode that will be displayed on the save file
        /// </summary>
        public short EpisodeNumber { get; set; }
        /// <summary>
        /// Current value of the Haruhi Meter (0-9 for 10% through 100%)
        /// </summary>
        public short HaruhiMeter { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown296 { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown298 { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown29C { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown29E { get; set; }
        /// <summary>
        /// Current friendship level with Haruhi (HFL)
        /// </summary>
        public byte HaruhiFriendshipLevel { get; set; }
        /// <summary>
        /// Current friendship level with Mikuru (MFL)
        /// </summary>
        public byte MikuruFriendshipLevel { get; set; }
        /// <summary>
        /// Current friendship level with Nagato (NFL)
        /// </summary>
        public byte NagatoFriendshipLevel { get; set; }
        /// <summary>
        /// Current friendship level with Koizumi (KFL)
        /// </summary>
        public byte KoizumiFriendshipLevel { get; set; }
        /// <summary>
        /// Unused in game
        /// </summary>
        public byte UnknownFriendshipLevel { get; set; }
        /// <summary>
        /// Current friendship level with Tsuruya (TFL)
        /// </summary>
        public byte TsuruyaFriendshipLevel { get; set; }
        /// <summary>
        /// Unknown footer
        /// </summary>
        public byte[] Footer { get; set; }

        /// <summary>
        /// Abstract constructor of a save slot section
        /// </summary>
        /// <param name="data">Binary data of the save slot section</param>
        public SaveSlotData(IEnumerable<byte> data)
        {
            Flags = data.Skip(0x08).Take(0x280).ToArray();
            if (data.ElementAt(0x288) != 0)
            {
                SaveTime = new(data.ElementAt(0x288) + 2000, data.ElementAt(0x289), data.ElementAt(0x28A), data.ElementAt(0x28B), data.ElementAt(0x28C), data.ElementAt(0x28D), TimeSpan.Zero);
            }
            else
            {
                SaveTime = DateTimeOffset.MinValue;
            }
            // Padding: 2 bytes
            ScenarioPosition = IO.ReadShort(data, 0x290);
            EpisodeNumber = IO.ReadShort(data, 0x292);
            HaruhiMeter = IO.ReadShort(data, 0x294);
            Unknown296 = IO.ReadShort(data, 0x296);
            Unknown298 = IO.ReadInt(data, 0x298);
            Unknown29C = IO.ReadShort(data, 0x29C);
            Unknown29E = IO.ReadShort(data, 0x29E);
            // Padding: 2 bytes
            HaruhiFriendshipLevel = data.ElementAt(0x2A2);
            MikuruFriendshipLevel = data.ElementAt(0x2A3);
            NagatoFriendshipLevel = data.ElementAt(0x2A4);
            KoizumiFriendshipLevel = data.ElementAt(0x2A5);
            UnknownFriendshipLevel = data.ElementAt(0x2A6);
            TsuruyaFriendshipLevel = data.ElementAt(0x2A7);
            Footer = data.Skip(0x2A8).Take(0x144).ToArray();
        }

        /// <summary>
        /// Get the binary representation of the data portion of the section not including the checksum
        /// </summary>
        /// <returns>Byte array of the checksumless binary data</returns>
        protected override byte[] GetDataBytes()
        {
            List<byte> data = [];

            data.AddRange(Flags);
            if (SaveTime == DateTimeOffset.MinValue)
            {
                data.AddRange(new byte[6]);
            }
            else if (SaveTime.Year < 2000 || SaveTime.Year > 2255)
            {
                throw new ArgumentException($"Invalid year for save time provided ({SaveTime.Year}); must be between 2000 and 2255");
            }
            else
            {
                data.Add((byte)(SaveTime.Year - 2000));
                data.Add((byte)SaveTime.Month);
                data.Add((byte)SaveTime.Day);
                data.Add((byte)SaveTime.Hour);
                data.Add((byte)SaveTime.Minute);
                data.Add((byte)SaveTime.Second);
            }
            data.AddRange(new byte[2]);
            data.AddRange(BitConverter.GetBytes(ScenarioPosition));
            data.AddRange(BitConverter.GetBytes(EpisodeNumber));
            data.AddRange(BitConverter.GetBytes(HaruhiMeter));
            data.AddRange(BitConverter.GetBytes(Unknown296));
            data.AddRange(BitConverter.GetBytes(Unknown298));
            data.AddRange(BitConverter.GetBytes(Unknown29C));
            data.AddRange(BitConverter.GetBytes(Unknown29E));
            data.AddRange(new byte[2]);
            data.Add(HaruhiFriendshipLevel);
            data.Add(MikuruFriendshipLevel);
            data.Add(NagatoFriendshipLevel);
            data.Add(KoizumiFriendshipLevel);
            data.Add(UnknownFriendshipLevel);
            data.Add(TsuruyaFriendshipLevel);
            data.AddRange(Footer);
            data.AddRange(new byte[4]);

            return [.. data];
        }
    }
}
