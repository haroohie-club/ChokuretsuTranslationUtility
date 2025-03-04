namespace HaruhiChokuretsuLib.Audio.ADX;

/// <summary>
/// Interface for ADX/AHX decoders
/// </summary>
public interface IAdxDecoder
{
    /// <summary>
    /// Number of channels supported
    /// </summary>
    public uint Channels { get; }
    /// <summary>
    /// The sample rate of the audio file
    /// </summary>
    public uint SampleRate { get; }
    /// <summary>
    /// Audio loop info
    /// </summary>
    public LoopInfo LoopInfo { get; }
    /// <summary>
    /// Gets the next sample from the audio file
    /// </summary>
    /// <returns>The next Sample</returns>
    public Sample NextSample();
}