﻿using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data
{
    /// <summary>
    /// Represents MESS.S in dat.bin
    /// </summary>
    public class MessageFile : DataFile, ITranslatableFile
    {
        /// <summary>
        /// The list of messages as defined in the file
        /// </summary>
        public List<string> Messages { get; set; } = [];

        /// <inheritdoc/>
        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            Log = log;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            int numSections = IO.ReadInt(decompressedData, 0);
            if (numSections != 1)
            {
                Log.LogError($"MESS.S should only have 1 section, but {numSections} were detected.");
                return;
            }

            int messSecLoc = IO.ReadInt(decompressedData, 0x0C);
            int numMess = IO.ReadInt(decompressedData, 0x10);

            for (int i = 0; i < numMess; i++)
            {
                int messLoc = IO.ReadInt(decompressedData, messSecLoc + i * 0x04);
                if (messLoc > 0)
                {
                    Messages.Add(Encoding.GetEncoding("Shift-JIS").GetString(decompressedData.Skip(messLoc).TakeWhile(b => b != 0x00).ToArray()));
                }
            }
        }

        /// <inheritdoc/>
        public override string GetSource(Dictionary<string, IncludeEntry[]> includes)
        {
            StringBuilder sb = new();

            int numPointers = 0;
            sb.AppendLine(".word 1");
            sb.AppendLine(".word END_POINTERS");
            sb.AppendLine(".word FILE_START");
            sb.AppendLine(".word MESS_SECTION");
            sb.AppendLine($".word {Messages.Count + 1}");
            sb.AppendLine();


            sb.AppendLine("FILE_START:");
            sb.AppendLine("MESS_SECTION:");
            sb.AppendLine(".word 0");

            for (int i = 0; i < Messages.Count; i++)
            {
                sb.AppendLine($"POINTER{numPointers++}: .word MESS{i}");
            }

            for (int i = 0; i < Messages.Count; i++)
            {
                sb.AppendLine($"MESS{i}: .string \"{Messages[i].EscapeShiftJIS()}\"");
                sb.AsmPadString(Messages[i], Encoding.GetEncoding("Shift-JIS"));
            }

            sb.AppendLine("END_POINTERS:");
            sb.AppendLine($".word {numPointers}");
            for (int i = 0; i < numPointers; i++)
            {
                sb.AppendLine($".word POINTER{i}");
            }

            return sb.ToString();
        }

        /// <inheritdoc/>
        public List<TranslatableString> GetTranslatableStrings()
        {
            return Messages.Select((m, i) => new TranslatableString { Key = $"MESS{i:D3}", Comment = "UI text", Line = m }).ToList();
        }

        /// <inheritdoc/>
        public void ReplaceTranslatableStrings(List<TranslatableString> newTranslations)
        {
            foreach (TranslatableString str in newTranslations)
            {
                Messages[int.Parse(str.Key[4..])] = str.Line;
            }
        }
    }
}
