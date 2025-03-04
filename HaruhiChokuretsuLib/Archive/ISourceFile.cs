using System.Collections.Generic;

namespace HaruhiChokuretsuLib.Archive;

/// <summary>
/// Interface for files that can be represented as ARM assembly
/// </summary>
public interface ISourceFile
{
    /// <summary>
    /// Gets the ARM assembly source representation of this file
    /// </summary>
    /// <param name="includes">A dictionary mapping archive file names to an array of IncludeEntry objects describing the includes from the specified archive file</param>
    /// <returns>A string containing the ASM source of this file</returns>
    public string GetSource(Dictionary<string, IncludeEntry[]> includes);
}

/// <summary>
/// A representation of an assembly include (specifying a file in an archive, usually)
/// </summary>
/// <param name="include">A string representing the include in the format of '.set FILENAME, INDEX'</param>
public class IncludeEntry(string include)
{
    /// <summary>
    /// The name of the include
    /// </summary>
    public string Name { get; set; } = include[5..include.IndexOf(',')];
    /// <summary>
    /// The value of the include
    /// </summary>
    public int Value { get; set; } = int.Parse(include[(include.IndexOf(',') + 1)..]);
}