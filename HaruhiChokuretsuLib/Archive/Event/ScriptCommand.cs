using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Event
{
    public class ScriptCommand
    {
        public int CommandId { get; set; }
        public string Mnemonic { get; set; }
        public string[] Parameters { get; set; }

        public ScriptCommand(int commandId, string mnemonic, string[] parameters)
        {
            CommandId = commandId;
            Mnemonic = mnemonic;
            Parameters = parameters;
            if (Mnemonic.StartsWith("UNKNOWN") && Parameters.Length == 0)
            {
                Parameters = new string[] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p" };
            }
        }

        public string GetMacro()
        {
            StringBuilder macroBuilder = new();
            macroBuilder.Append($".macro {Mnemonic} ");
            macroBuilder.AppendLine(string.Join(", ", Parameters));
            macroBuilder.AppendLine($"   .word {CommandId}");

            for (int i = 0; i < 16; i++)
            {
                if (i < Parameters.Length)
                {
                    macroBuilder.AppendLine($"      .short \\{Parameters[i]}");
                }
                else
                {
                    macroBuilder.AppendLine("      .short 0");
                }
            }

            macroBuilder.AppendLine(".endm");

            return macroBuilder.ToString();
        }
    }

    public class ScriptCommandInvocation
    {
        public ScriptCommand Command { get; set; }
        public List<short> Parameters { get; set; } = new();

        public ScriptCommandInvocation(ScriptCommand command)
        {
            Command = command;
            Parameters.AddRange(new short[16]);
        }

        public ScriptCommandInvocation(IEnumerable<byte> data, List<ScriptCommand> commandsAvailable)
        {
            Command = commandsAvailable.FirstOrDefault(c => c.CommandId == BitConverter.ToInt32(data.Take(4).ToArray()));
            Parameters.Add(BitConverter.ToInt16(data.Skip(0x04).Take(2).ToArray()));
            Parameters.Add(BitConverter.ToInt16(data.Skip(0x06).Take(2).ToArray()));
            Parameters.Add(BitConverter.ToInt16(data.Skip(0x08).Take(2).ToArray()));
            Parameters.Add(BitConverter.ToInt16(data.Skip(0x0A).Take(2).ToArray()));
            Parameters.Add(BitConverter.ToInt16(data.Skip(0x0C).Take(2).ToArray()));
            Parameters.Add(BitConverter.ToInt16(data.Skip(0x0E).Take(2).ToArray()));
            Parameters.Add(BitConverter.ToInt16(data.Skip(0x10).Take(2).ToArray()));
            Parameters.Add(BitConverter.ToInt16(data.Skip(0x12).Take(2).ToArray()));
            Parameters.Add(BitConverter.ToInt16(data.Skip(0x14).Take(2).ToArray()));
            Parameters.Add(BitConverter.ToInt16(data.Skip(0x16).Take(2).ToArray()));
            Parameters.Add(BitConverter.ToInt16(data.Skip(0x18).Take(2).ToArray()));
            Parameters.Add(BitConverter.ToInt16(data.Skip(0x1A).Take(2).ToArray()));
            Parameters.Add(BitConverter.ToInt16(data.Skip(0x1C).Take(2).ToArray()));
            Parameters.Add(BitConverter.ToInt16(data.Skip(0x1E).Take(2).ToArray()));
            Parameters.Add(BitConverter.ToInt16(data.Skip(0x20).Take(2).ToArray()));
            Parameters.Add(BitConverter.ToInt16(data.Skip(0x22).Take(2).ToArray()));
        }
    }
}
