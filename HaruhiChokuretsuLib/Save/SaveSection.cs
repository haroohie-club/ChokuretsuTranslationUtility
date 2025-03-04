using System;

namespace HaruhiChokuretsuLib.Save;

/// <summary>
/// Abstract class for common methods applying to all blocks of save data
/// </summary>
public abstract class SaveSection
{
    /// <summary>
    /// An array of bitmask flags
    /// </summary>
    public byte[] Flags { get; set; }

    /// <summary>
    /// Gets the binary representation of the save data section
    /// </summary>
    /// <returns>Byte array of the binary data</returns>
    public byte[] GetBytes()
    {
        return [.. GetChecksum(), .. GetDataBytes()];
    }

    /// <summary>
    /// Get the binary representation of the data portion of the section not including the checksum
    /// </summary>
    /// <returns>Byte array of the checksumless binary data</returns>
    protected virtual byte[] GetDataBytes()
    {
        return [];
    }

    /// <summary>
    /// Clears the save slot, restoring it to being like a new save
    /// </summary>
    public virtual void Clear()
    {
        Flags = new byte[Flags.Length];
    }

    /// <summary>
    /// Calculates the checksum based on the current binary data
    /// </summary>
    /// <returns>Byte array of the binary checksum data</returns>
    public byte[] GetChecksum()
    {
        byte[] data = GetDataBytes();
        uint checksum1 = 0xA93D15EF;
        uint checksum2 = 0x5A49FFC3;
        for (int i = 0; i < data.Length; i++)
        {
            checksum1 += data[i];
            checksum2 ^= checksum1;
        }

        return [.. BitConverter.GetBytes(checksum1), .. BitConverter.GetBytes(checksum2)];
    }

    /// <summary>
    /// Determines whether a particular flag is set
    /// </summary>
    /// <param name="flag">The flag to check</param>
    /// <returns>True if the flag is set, false otherwise</returns>
    public bool IsFlagSet(int flag)
    {
        return (Flags[flag / 8] & (1 << (flag % 8))) > 0;
    }

    /// <summary>
    /// Sets a particular flag
    /// </summary>
    /// <param name="flag">The flag to set</param>
    public void SetFlag(int flag)
    {
        Flags[flag / 8] |= (byte)(1 << (flag % 8));
    }

    /// <summary>
    /// Clears a particular flag
    /// </summary>
    /// <param name="flag">The flag to clear</param>
    public void ClearFlag(int flag)
    {
        Flags[flag / 8] &= (byte)~(1 << (flag % 8));
    }
}