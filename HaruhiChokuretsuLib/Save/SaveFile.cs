using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Save;

/// <summary>
/// Represents a Chokuretsu save file
/// </summary>
public class SaveFile
{
    private const string MAGIC = "SEGA_HARUHI_CHOKURETSU_DATA";

    /// <summary>
    /// The common data for the save file (not related to any particular slot)
    /// </summary>
    public CommonSaveData CommonData { get; set; }
    /// <summary>
    /// The loss prevention segment of the common data (exists in case the primary common data becomes corrupted)
    /// </summary>
    public CommonSaveData CommonDataBackup { get; set; }
    /// <summary>
    /// The static save slot data for the save file (slots 1 and 2)
    /// </summary>
    public SaveSlotData[] CheckpointSaveSlots { get; set; }
    /// <summary>
    /// The loss prevention segements of the static save slot data (exists in case the primary slot becomes corrupted)
    /// </summary>
    public SaveSlotData[] CheckpointSaveSlotBackups { get; set; }
    /// <summary>
    /// The dyanmic save slot data for the save file (slot 3)
    /// </summary>
    public QuickSaveSlotData QuickSaveSlot { get; set; }
    /// <summary>
    /// The loss prevention segment of the dynamic save slot data (exists in case the primary slot becomes corrupted)
    /// </summary>
    public QuickSaveSlotData QuickSaveSlotBackup { get; set; }

    /// <summary>
    /// Creates a save file given binary data
    /// </summary>
    /// <param name="data">The binary data representing the save file</param>
    public SaveFile(byte[] data)
    {
        if (Encoding.ASCII.GetString(data.Take(0x1B).ToArray()) != MAGIC || data.Count() != 0x2000)
        {
            throw new ArgumentException("Invalid save file");
        }

        CommonData = new(data[0x20..0x310]);
        CommonDataBackup = new(data[0x310..0x600]);
        CheckpointSaveSlots = new SaveSlotData[2];
        CheckpointSaveSlotBackups = new SaveSlotData[2];
        for (int i = 0; i < CheckpointSaveSlots.Length; i++)
        {
            CheckpointSaveSlots[i] = new(data[(0x600 + 0x7E0 * i)..(0x9F0 + 0x7E0 * i)]);
            CheckpointSaveSlotBackups[i] = new(data[(0x9F0 + 0x7E0 * i)..(0xDE0 + 0x7E0 * i)]);
        }
        QuickSaveSlot = new(data[0x15C0..0x19F0]);
        QuickSaveSlotBackup = new(data[0x19F0..0x1E20]);
    }

    /// <summary>
    /// Gets the save file's bytes
    /// </summary>
    /// <returns>Byte array containing binary representation of the save file</returns>
    public byte[] GetBytes()
    {
        List<byte> bytes = [];

        bytes.AddRange(Encoding.ASCII.GetBytes(MAGIC));
        bytes.AddRange(new byte[5]);
        bytes.AddRange(CommonData.GetBytes());
        bytes.AddRange(CommonData.GetBytes());
        foreach (SaveSlotData saveSlot in CheckpointSaveSlots)
        {
            bytes.AddRange(saveSlot.GetBytes());
            bytes.AddRange(saveSlot.GetBytes());
        }
        bytes.AddRange(QuickSaveSlot.GetBytes());
        bytes.AddRange(QuickSaveSlot.GetBytes());
        bytes.AddRange(new byte[0x1E0]);

        return [.. bytes];
    }
}