using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;

namespace HaruhiChokuretsuLib.Archive.Event;

public partial class EventFile
{
    /// <summary>
    /// In TUTORIAL.S, represents the list of tutorials
    /// </summary>
    public List<Tutorial> Tutorials { get; set; } = [];

    /// <summary>
    /// Initializes TUTORIAL.S
    /// </summary>
    public void InitializeTutorialFile()
    {
        byte[] data = Data.ToArray();
        int numSections = IO.ReadInt(data, 0x00);
        if (numSections != 1)
        {
            Log.LogError($"Tutorial file should have 1 section, {numSections} detected");
            return;
        }

        int tutorialsStart = IO.ReadInt(data, 0x0C);
        int numTutorials = IO.ReadInt(data, 0x10);

        for (int i = 0; i < numTutorials; i++)
        {
            Tutorials.Add(new(data[(tutorialsStart + i * 0x04)..(tutorialsStart + i * 0x04 + 4)]));
        }
    }
}

/// <summary>
/// Represents a tutorial entry in TUTORIAL.S
/// </summary>
public class Tutorial
{
    /// <summary>
    /// The ID/flag of the tutorial
    /// </summary>
    public short Id { get; set; }
    /// <summary>
    /// The script to be loaded for that tutorial
    /// </summary>
    public short AssociatedScript { get; set; }
    
    /// <summary>
    /// Parameterless constructor for serialization
    /// </summary>
    public Tutorial()
    {
    }

    /// <summary>
    /// Constructs a tutorial from data
    /// </summary>
    /// <param name="data">The binary data representing the tutorial</param>
    public Tutorial(byte[] data)
    {
        Id = IO.ReadShort(data, 0x00);
        AssociatedScript = IO.ReadShort(data, 0x02);
    }
}