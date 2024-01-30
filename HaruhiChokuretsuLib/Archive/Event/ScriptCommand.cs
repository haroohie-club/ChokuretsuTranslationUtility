using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Event
{
    /// <summary>
    /// Represents a command in an script event file
    /// This represents the actual abstract command; for an invocation use ScriptCommandInvocation
    /// </summary>
    public class ScriptCommand
    {
        /// <summary>
        /// The ID of this command
        /// </summary>
        public int CommandId { get; set; }
        /// <summary>
        /// The mnemonic for this command
        /// </summary>
        public string Mnemonic { get; set; }
        /// <summary>
        /// The parameters for this command
        /// </summary>
        public string[] Parameters { get; set; }

        internal ScriptCommand(int commandId, string mnemonic, string[] parameters)
        {
            CommandId = commandId;
            Mnemonic = mnemonic;
            Parameters = parameters;
            if (Mnemonic.StartsWith("UNKNOWN") && Parameters.Length == 0)
            {
                Parameters = ["a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p"];
            }
        }

        /// <summary>
        /// Gets the ARM assembly macro for this command
        /// </summary>
        /// <returns>A string containing the ASM macro for this command</returns>
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

    /// <summary>
    /// Represents an invocation of a ScriptCommand
    /// </summary>
    public class ScriptCommandInvocation
    {
        /// <summary>
        /// The script command that this is invoking
        /// </summary>
        public ScriptCommand Command { get; set; }
        /// <summary>
        /// The parameters set for this invocation
        /// </summary>
        public List<short> Parameters { get; set; } = [];

        /// <summary>
        /// Creates a blank script command invocation
        /// </summary>
        /// <param name="command">The command to invoke</param>
        public ScriptCommandInvocation(ScriptCommand command)
        {
            Command = command;
            Parameters.AddRange(new short[16]);
        }

        /// <summary>
        /// Creates a script command invocation from event file data
        /// </summary>
        /// <param name="data">The binary data for the script command invocation</param>
        /// <param name="commandsAvailable">The list of available commands</param>
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
