using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiChokuretsuLib.Save
{
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
        public SaveFile(IEnumerable<byte> data)
        {
            if (Encoding.ASCII.GetString(data.Take(0x1B).ToArray()) != MAGIC || data.Count() != 0x2000)
            {
                throw new ArgumentException("Invalid save file");
            }

            CommonData = new(data.Skip(0x20).Take(0x2F0));
            CommonDataBackup = new(data.Skip(0x310).Take(0x2F0));
            CheckpointSaveSlots = new SaveSlotData[2];
            CheckpointSaveSlotBackups = new SaveSlotData[2];
            for (int i = 0; i < CheckpointSaveSlots.Length; i++)
            {
                CheckpointSaveSlots[i] = new(data.Skip(0x600 + 0x7E0 * i).Take(0x3F0));
                CheckpointSaveSlotBackups[i] = new(data.Skip(0x9F0 + 0x7E0 * i).Take(0x3F0));
            }
            QuickSaveSlot = new(data.Skip(0x15C0).Take(0x430));
            QuickSaveSlotBackup = new(data.Skip(0x19F0).Take(0x430));
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
            for (int i = 0; i < CheckpointSaveSlots.Length; i++)
            {
                bytes.AddRange(CheckpointSaveSlots[i].GetBytes());
                bytes.AddRange(CheckpointSaveSlots[i].GetBytes());
            }
            bytes.AddRange(QuickSaveSlot.GetBytes());
            bytes.AddRange(QuickSaveSlot.GetBytes());
            bytes.AddRange(new byte[0x1E0]);

            return [.. bytes];
        }
    }
}
