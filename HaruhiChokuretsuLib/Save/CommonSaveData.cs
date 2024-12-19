using HaruhiChokuretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiChokuretsuLib.Save
{
    /// <summary>
    /// Represents the common save data (not specific to a particular slot)
    /// </summary>
    public class CommonSaveData : SaveSection
    {
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown08 { get; set; }
        /// <summary>
        /// Number of times this file has been saved
        /// </summary>
        public int NumSaves { get; set; }
        /// <summary>
        /// Save options as stored in the common save data
        /// </summary>
        public SaveOptions Options { get; set; }
        /// <summary>
        /// Power status data for Mikuru as stored in the common save data
        /// </summary>
        public CharacterPowerStatus MikuruPowerStatus { get; set; }
        /// <summary>
        /// Power status data for Nagato as stored in the common save data
        /// </summary>
        public CharacterPowerStatus NagatoPowerStatus { get; set; }
        /// <summary>
        /// Power status data for Koizumi as stored in the common save data
        /// </summary>
        public CharacterPowerStatus KoizumiPowerStatus { get; set; }
        /// <summary>
        /// In puzzle phase usage, contains pointers to the active characters, but in the save file is empty
        /// </summary>
        public byte[] Footer { get; set; }

        /// <summary>
        /// Creates the object based on the binary data section
        /// </summary>
        /// <param name="data"></param>
        public CommonSaveData(IEnumerable<byte> data)
        {
            Unknown08 = IO.ReadInt(data, 0x08);
            NumSaves = IO.ReadInt(data, 0x0C);
            Flags = data.Skip(0x10).Take(0x280).ToArray();
            Options = new(data.Skip(0x290).Take(0x18));
            MikuruPowerStatus = new([.. data.Skip(0x2A8).Take(0x14)]);
            NagatoPowerStatus = new([.. data.Skip(0x2BC).Take(0x14)]);
            KoizumiPowerStatus = new([.. data.Skip(0x2D0).Take(0x14)]);
            Footer = [.. data.Skip(0x2E4).Take(0x0C)];
        }

        /// <summary>
        /// Get the binary representation of the data portion of the section not including the checksum
        /// </summary>
        /// <returns>Byte array of the checksumless binary data</returns>
        protected override byte[] GetDataBytes()
        {
            List<byte> data = [];

            data.AddRange(BitConverter.GetBytes(Unknown08));
            data.AddRange(BitConverter.GetBytes(NumSaves));
            data.AddRange(Flags);
            data.AddRange(Options.GetBytes());
            data.AddRange(MikuruPowerStatus.GetBytes());
            data.AddRange(NagatoPowerStatus.GetBytes());
            data.AddRange(KoizumiPowerStatus.GetBytes());
            data.AddRange(Footer);

            return [.. data];
        }
    }

    /// <summary>
    /// Represents the saved options in the options menu as stored in the save file
    /// </summary>
    /// <remarks>
    /// Creates the object based on the binary data section
    /// </remarks>
    /// <param name="data">The section of data representing the save options</param>
    public class SaveOptions(IEnumerable<byte> data)
    {
        /// <summary>
        /// Set of flags for the investigation and puzzle phase options
        /// </summary>
        public PuzzleInvestigationOptions StoryOptions { get; set; } = (PuzzleInvestigationOptions)IO.ReadShort(data, 0x00);
        /// <summary>
        /// The set of flags indicating which character voices are enabled/disabled
        /// </summary>
        public VoiceOptions VoiceToggles { get; set; } = (VoiceOptions)IO.ReadShort(data, 0x02);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown04 { get; set; } = IO.ReadInt(data, 0x04);
        /// <summary>
        /// Volume of the background music
        /// </summary>
        public int BgmVolume { get; set; } = IO.ReadInt(data, 0x08);
        /// <summary>
        /// Volume of the sound effects
        /// </summary>
        public int SfxVolume { get; set; } = IO.ReadInt(data, 0x0C);
        /// <summary>
        /// Volume of unpsoken dialogue
        /// </summary>
        public int WordsVolume { get; set; } = IO.ReadInt(data, 0x10);
        /// <summary>
        /// Volume of spoken dialogue
        /// </summary>
        public int VoiceVolume { get; set; } = IO.ReadInt(data, 0x14);

        /// <summary>
        /// Gets the binary representation of the options section
        /// </summary>
        /// <returns>Byte array of binary data</returns>
        public List<byte> GetBytes()
        {
            List<byte> bytes = [];

            bytes.AddRange(BitConverter.GetBytes((short)StoryOptions));
            bytes.AddRange(BitConverter.GetBytes((short)VoiceToggles));
            bytes.AddRange(BitConverter.GetBytes(Unknown04));
            bytes.AddRange(BitConverter.GetBytes(BgmVolume));
            bytes.AddRange(BitConverter.GetBytes(SfxVolume));
            bytes.AddRange(BitConverter.GetBytes(WordsVolume));
            bytes.AddRange(BitConverter.GetBytes(VoiceVolume));

            return bytes;
        }

        /// <summary>
        /// Set of flags describing the investigation and puzzle phase options
        /// </summary>
        [Flags]
        public enum PuzzleInvestigationOptions : short
        {
            /// <summary>
            /// When set, batch dialogue display is turned on
            /// </summary>
            BATCH_DIALOGUE_DISPLAY_ON = 1 << 0,
            /// <summary>
            /// When set, puzzle interrupt scenes are set to "unseen only" (mutually exclusive with PUZZLE_INTERRUPT_SCENES_ON)
            /// </summary>
            PUZZLE_INTERRUPT_SCENES_UNSEEN_ONLY = 1 << 1,
            /// <summary>
            /// When set, puzzle interrupt scenes are set to "on" (mutually exclusive with PUZZLE_INTERRUPT_SCENES_UNSEEN_ONLY)
            /// </summary>
            PUZZLE_INTERRUPTE_SCENES_ON = 1 << 2,
            /// <summary>
            /// When set, topic stock mode is turned on
            /// </summary>
            TOPIC_STOCK_MODE_ON = 1 << 3,
            /// <summary>
            /// When set, dialogue skipping is set to "Skip Already Read"; when unset, it is set to "Fast Forward"
            /// </summary>
            DIALOGUE_SKIPPING_SKIP_ALREADY_READ = 1 << 4,
        }

        /// <summary>
        /// Set of flags describing which voices are switched on/off
        /// </summary>
        [Flags]
        public enum VoiceOptions : short
        {
            /// <summary>
            /// Kyon's voice
            /// </summary>
            KYON = 1 << 4,
            /// <summary>
            /// Haruhi's voice
            /// </summary>
            HARUHI = 1 << 5,
            /// <summary>
            /// Mikuru's voice
            /// </summary>
            MIKURU = 1 << 6,
            /// <summary>
            /// Nagato's voice
            /// </summary>
            NAGATO = 1 << 7,
            /// <summary>
            /// Koizumi's voice
            /// </summary>
            KOIZUMI = 1 << 8,
            /// <summary>
            /// Kyon's sister's voice
            /// </summary>
            SISTER = 1 << 9,
            /// <summary>
            /// Tsuruya's voice
            /// </summary>
            TSURUYA = 1 << 10,
            /// <summary>
            /// Taniguchi's voice
            /// </summary>
            TANIGUCHI = 1 << 11,
            /// <summary>
            /// Kunikida's voice
            /// </summary>
            KUNIKIDA = 1 << 12,
            /// <summary>
            /// Mystery girl's (Mikoto Misumaru's) voice
            /// </summary>
            MYSTERY_GIRL = 1 << 13,
        }
    }
}

/// <summary>
/// Defines a character's ability properties as seen in the puzzle phase
/// </summary>
public class CharacterPowerStatus(byte[] data)
{
    /// <summary>
    /// The level of the character's power -- min 1, max 5
    /// </summary>
    public byte Level { get; set; } = data[0x00];
    /// <summary>
    /// Unknown
    /// </summary>
    public byte Unknown01 { get; set; } = data[0x01];
    /// <summary>
    /// Number of remaining uses of the character's power
    /// </summary>
    public byte RemainingUses { get; set; } = data[0x02];
    /// <summary>
    /// The number of times the power has been used since the character last leveled up
    /// </summary>
    public byte UsesSinceLevelUp { get; set; } = data[0x03];
    /// <summary>
    /// The total number of times the power has to be used to level up
    /// When this equals <see cref="UsesSinceLevelUp"/>, the character levels up
    /// </summary>
    public byte UsesToLevelUp { get; set; } = data[0x04];
    /// <summary>
    /// Unknown
    /// </summary>
    public byte Unknown05 { get; set; } = data[0x05];
    /// <summary>
    /// Unknown
    /// </summary>
    public short Unknown06 { get; set; } = IO.ReadShort(data, 0x06);
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown08 { get; set; } = IO.ReadInt(data, 0x08);
    /// <summary>
    /// Not used in save data, but in the puzzle phase this represents the cooldown timer for the power
    /// </summary>
    public int CooldownTimer { get; set; } = IO.ReadInt(data, 0x0C);
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown10 { get; set; } = IO.ReadInt(data, 0x10);

    /// <summary>
    /// Gets the binary representation of this structure
    /// </summary>
    /// <returns>A byte array containing the binary representation of this structure</returns>
    public byte[] GetBytes()
    {
        return
        [
            Level, Unknown01, RemainingUses, UsesSinceLevelUp, UsesToLevelUp, Unknown05,
            .. BitConverter.GetBytes(Unknown06),
            .. BitConverter.GetBytes(Unknown08), .. BitConverter.GetBytes(CooldownTimer),
            ..BitConverter.GetBytes(Unknown10),
        ];
    }
}
