using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data
{
    public class FontFile : DataFile
    {
        public List<char> CharMap { get; set; } = new();

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
