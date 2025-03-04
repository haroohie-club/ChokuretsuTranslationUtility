using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;

namespace HaruhiChokuretsuLib.Archive.Event;

/// <summary>
/// An interface representing event sections
/// </summary>
/// <typeparam name="T">The type of object that section contains</typeparam>
public interface IEventSection<T>
{
    /// <summary>
    /// The name of the section (user-defined)
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// The list of objects in the section
    /// </summary>
    public List<T> Objects { get; set; }

    internal void Initialize(byte[] data, int numObjects, string name, ILogger log, int offset);
    internal string GetAsm(int indentation, ref int currentPointer, EventFile evt);
}