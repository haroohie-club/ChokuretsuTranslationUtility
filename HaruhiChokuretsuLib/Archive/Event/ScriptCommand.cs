using System.Collections.Generic;
using System.Linq;
using System.Text;
using HaruhiChokuretsuLib.Util;

namespace HaruhiChokuretsuLib.Archive.Event;

/// <summary>
/// Represents a command in a script event file
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
    public ScriptCommandInvocation(byte[] data, List<ScriptCommand> commandsAvailable)
    {
        Command = commandsAvailable.FirstOrDefault(c => c.CommandId == IO.ReadInt(data, 0));
        Parameters.Add(IO.ReadShort(data, 0x04));
        Parameters.Add(IO.ReadShort(data, 0x06));
        Parameters.Add(IO.ReadShort(data, 0x08));
        Parameters.Add(IO.ReadShort(data, 0x0A));
        Parameters.Add(IO.ReadShort(data, 0x0C));
        Parameters.Add(IO.ReadShort(data, 0x0E));
        Parameters.Add(IO.ReadShort(data, 0x10));
        Parameters.Add(IO.ReadShort(data, 0x12));
        Parameters.Add(IO.ReadShort(data, 0x14));
        Parameters.Add(IO.ReadShort(data, 0x16));
        Parameters.Add(IO.ReadShort(data, 0x18));
        Parameters.Add(IO.ReadShort(data, 0x1A));
        Parameters.Add(IO.ReadShort(data, 0x1C));
        Parameters.Add(IO.ReadShort(data, 0x1E));
        Parameters.Add(IO.ReadShort(data, 0x20));
        Parameters.Add(IO.ReadShort(data, 0x22));
    }
}