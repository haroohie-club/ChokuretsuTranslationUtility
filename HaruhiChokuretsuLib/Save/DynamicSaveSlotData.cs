using HaruhiChokuretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiChokuretsuLib.Save
{
    /// <summary>
    /// Represents a dynamic save slot's data (slot 3)
    /// </summary>
    /// <remarks>
    /// Creates a static save slot representation given binary data
    /// </remarks>
    /// <param name="data">The binary data of the save slot</param>
    public class DynamicSaveSlotData(IEnumerable<byte> data) : SaveSlotData(data)
    {
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown3F0 { get; set; } = IO.ReadInt(data, 0x3F0);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown3F4 { get; set; } = IO.ReadInt(data, 0x3F4);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown3F8 { get; set; } = IO.ReadInt(data, 0x3F8);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown3FC { get; set; } = IO.ReadInt(data, 0x3FC);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown400 { get; set; } = IO.ReadInt(data, 0x400);
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown404 { get; set; } = IO.ReadShort(data, 0x404);
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown406 { get; set; } = IO.ReadShort(data, 0x406);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown408 { get; set; } = IO.ReadInt(data, 0x408);
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown40C { get; set; } = IO.ReadShort(data, 0x40C);
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown40E { get; set; } = IO.ReadShort(data, 0x40E);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown410 { get; set; } = IO.ReadInt(data, 0x410);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown414 { get; set; } = IO.ReadInt(data, 0x414);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown418 { get; set; } = IO.ReadInt(data, 0x418);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown41C { get; set; } = IO.ReadInt(data, 0x41C);
        /// <summary>
        /// The script where the save was made
        /// </summary>
        public int CurrentScript { get; set; } = IO.ReadInt(data, 0x420);
        /// <summary>
        /// The script block in the script where the save was made
        /// </summary>
        public int CurrentScriptBlock { get; set; } = IO.ReadInt(data, 0x424);
        /// <summary>
        /// The index of the command in the script block where the save was made
        /// </summary>
        public int CurrentScriptCommand { get; set; } = IO.ReadInt(data, 0x428);

        /// <summary>
        /// Get the binary representation of the data portion of the section not including the checksum
        /// </summary>
        /// <returns>Byte array of the checksumless binary data</returns>
        protected override byte[] GetDataBytes()
        {
            List<byte> data = [.. base.GetDataBytes()];

            data.AddRange(BitConverter.GetBytes(Unknown3F0));
            data.AddRange(BitConverter.GetBytes(Unknown3F4));
            data.AddRange(BitConverter.GetBytes(Unknown3F8));
            data.AddRange(BitConverter.GetBytes(Unknown3FC));
            data.AddRange(BitConverter.GetBytes(Unknown400));
            data.AddRange(BitConverter.GetBytes(Unknown404));
            data.AddRange(BitConverter.GetBytes(Unknown406));
            data.AddRange(BitConverter.GetBytes(Unknown408));
            data.AddRange(BitConverter.GetBytes(Unknown40C));
            data.AddRange(BitConverter.GetBytes(Unknown40E));
            data.AddRange(BitConverter.GetBytes(Unknown410));
            data.AddRange(BitConverter.GetBytes(Unknown414));
            data.AddRange(BitConverter.GetBytes(Unknown418));
            data.AddRange(BitConverter.GetBytes(Unknown41C));
            data.AddRange(BitConverter.GetBytes(CurrentScript));
            data.AddRange(BitConverter.GetBytes(CurrentScriptBlock));
            data.AddRange(BitConverter.GetBytes(CurrentScriptCommand));
            data.AddRange(new byte[4]);

            return [.. data];
        }
    }
}
