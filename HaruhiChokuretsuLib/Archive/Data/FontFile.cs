using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data
{
    /// <summary>
    /// Representation of FONT.S in dat.bin 
    /// </summary>
    public class FontFile : DataFile
    {
        /// <summary>
        /// The map of characters that can be used in-game (corresponds to their position in the font graphic)
        /// </summary>
        public List<char> CharMap { get; set; } = [];

        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (IO.ReadInt(decompressedData, 0) != 1)
            {
                log.LogError($"FONT.S should only have one section, {IO.ReadInt(decompressedData, 0)} detected.");
                return;
            }

            CharMap.AddRange(Encoding.GetEncoding("Shift-JIS").GetChars(decompressedData.Skip(IO.ReadInt(decompressedData, 0x0C)).TakeWhile(c => c != 0x00).ToArray()));
        }
    }
}
