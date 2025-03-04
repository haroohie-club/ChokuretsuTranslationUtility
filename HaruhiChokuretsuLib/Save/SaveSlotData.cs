using HaruhiChokuretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using HaruhiChokuretsuLib.Archive.Event;

namespace HaruhiChokuretsuLib.Save;

/// <summary>
/// Represents a save slot (either dynamic or static)
/// </summary>
public class SaveSlotData : SaveSection
{
    /// <summary>
    /// The time the game was saved (cannot be before year 2000 or after year 2255)
    /// </summary>
    public DateTimeOffset SaveTime { get; set; }
    /// <summary>
    /// The 1-indexed position of the scenario command the save is at
    /// </summary>
    public short ScenarioPosition { get; set; }
    /// <summary>
    /// The episode that will be displayed on the save file
    /// </summary>
    public short EpisodeNumber { get; set; }
    /// <summary>
    /// Current value of the Haruhi Meter (0-9 for 10% through 100%)
    /// </summary>
    public short HaruhiMeter { get; set; }
    /// <summary>
    /// This value is set by the script command GLOBAL2D, but its function is unknown
    /// </summary>
    public short Global2D { get; set; }
    /// <summary>
    /// The index of the objective Kyon was most recently assigned to (0 = A, 1 = B, 2 = C, 3 = D)
    /// </summary>
    public int KyonObjectiveIndex { get; set; }
    /// <summary>
    /// The characters present on the most recent objective A
    /// </summary>
    public CharacterMask ObjectiveA { get; set; }
    /// <summary>
    /// The characters present on the most recent objective B
    /// </summary>
    public CharacterMask ObjectiveB { get; set; }
    /// <summary>
    /// The characters present on the most recent objective C
    /// </summary>
    public CharacterMask ObjectiveC { get; set; }
    /// <summary>
    /// The characters present on the most recent objective D
    /// </summary>
    public CharacterMask ObjectiveD { get; set; }
    /// <summary>
    /// Current friendship level with Haruhi (HFL)
    /// </summary>
    public byte HaruhiFriendshipLevel { get; set; }
    /// <summary>
    /// Current friendship level with Mikuru (MFL)
    /// </summary>
    public byte MikuruFriendshipLevel { get; set; }
    /// <summary>
    /// Current friendship level with Nagato (NFL)
    /// </summary>
    public byte NagatoFriendshipLevel { get; set; }
    /// <summary>
    /// Current friendship level with Koizumi (KFL)
    /// </summary>
    public byte KoizumiFriendshipLevel { get; set; }
    /// <summary>
    /// Unused in game
    /// </summary>
    public byte UnknownFriendshipLevel { get; set; }
    /// <summary>
    /// Current friendship level with Tsuruya (TFL)
    /// </summary>
    public byte TsuruyaFriendshipLevel { get; set; }
    /// <summary>
    /// Unknown footer
    /// </summary>
    public byte[] Footer { get; set; }

    /// <summary>
    /// Abstract constructor of a save slot section
    /// </summary>
    /// <param name="data">Binary data of the save slot section</param>
    public SaveSlotData(byte[] data)
    {
        Flags = data.Skip(0x08).Take(0x280).ToArray();
        if (data.ElementAt(0x288) != 0)
        {
            SaveTime = new(data[0x288] + 2000, data[0x289], data[0x28A], data[0x28B], data[0x28C], data[0x28D], TimeSpan.Zero);
        }
        else
        {
            SaveTime = DateTimeOffset.MinValue;
        }
        // Padding: 2 bytes
        ScenarioPosition = IO.ReadShort(data, 0x290);
        EpisodeNumber = IO.ReadShort(data, 0x292);
        HaruhiMeter = IO.ReadShort(data, 0x294);
        Global2D = IO.ReadShort(data, 0x296);
        KyonObjectiveIndex = IO.ReadInt(data, 0x298);
        ObjectiveA = (CharacterMask)data.ElementAt(0x29C);
        ObjectiveB = (CharacterMask)data.ElementAt(0x29D);
        ObjectiveC = (CharacterMask)data.ElementAt(0x29E);
        ObjectiveD = (CharacterMask)data.ElementAt(0x29F);
        // Padding: 2 bytes
        HaruhiFriendshipLevel = data.ElementAt(0x2A2);
        MikuruFriendshipLevel = data.ElementAt(0x2A3);
        NagatoFriendshipLevel = data.ElementAt(0x2A4);
        KoizumiFriendshipLevel = data.ElementAt(0x2A5);
        UnknownFriendshipLevel = data.ElementAt(0x2A6);
        TsuruyaFriendshipLevel = data.ElementAt(0x2A7);
        Footer = data.Skip(0x2A8).Take(0x144).ToArray();
    }

    /// <inheritdoc/>
    public override void Clear()
    {
        base.Clear();
        SaveTime = DateTimeOffset.MinValue;
        ScenarioPosition = 0;
        EpisodeNumber = 0;
        HaruhiMeter = 0;
        Global2D = 0;
        KyonObjectiveIndex = 0;
        ObjectiveA = 0;
        ObjectiveB = 0;
        ObjectiveC = 0;
        ObjectiveD = 0;
        HaruhiFriendshipLevel = 0;
        MikuruFriendshipLevel = 0;
        NagatoFriendshipLevel = 0;
        KoizumiFriendshipLevel = 0;
        UnknownFriendshipLevel = 0;
        TsuruyaFriendshipLevel = 0;
        Footer = new byte[Footer.Length];
    }

    /// <summary>
    /// Get the binary representation of the data portion of the section not including the checksum
    /// </summary>
    /// <returns>Byte array of the checksumless binary data</returns>
    protected override byte[] GetDataBytes()
    {
        List<byte> data = [];

        data.AddRange(Flags);
        if (SaveTime == DateTimeOffset.MinValue)
        {
            data.AddRange(new byte[6]);
        }
        else if (SaveTime.Year < 2000 || SaveTime.Year > 2255)
        {
            throw new ArgumentException($"Invalid year for save time provided ({SaveTime.Year}); must be between 2000 and 2255");
        }
        else
        {
            data.Add((byte)(SaveTime.Year - 2000));
            data.Add((byte)SaveTime.Month);
            data.Add((byte)SaveTime.Day);
            data.Add((byte)SaveTime.Hour);
            data.Add((byte)SaveTime.Minute);
            data.Add((byte)SaveTime.Second);
        }
        data.AddRange(new byte[2]);
        data.AddRange(BitConverter.GetBytes(ScenarioPosition));
        data.AddRange(BitConverter.GetBytes(EpisodeNumber));
        data.AddRange(BitConverter.GetBytes(HaruhiMeter));
        data.AddRange(BitConverter.GetBytes(Global2D));
        data.AddRange(BitConverter.GetBytes(KyonObjectiveIndex));
        data.Add((byte)ObjectiveA);
        data.Add((byte)ObjectiveB);
        data.Add((byte)ObjectiveC);
        data.Add((byte)ObjectiveD);
        data.AddRange(new byte[2]);
        data.Add(HaruhiFriendshipLevel);
        data.Add(MikuruFriendshipLevel);
        data.Add(NagatoFriendshipLevel);
        data.Add(KoizumiFriendshipLevel);
        data.Add(UnknownFriendshipLevel);
        data.Add(TsuruyaFriendshipLevel);
        data.AddRange(Footer);
        data.AddRange(new byte[4]);

        return [.. data];
    }
}