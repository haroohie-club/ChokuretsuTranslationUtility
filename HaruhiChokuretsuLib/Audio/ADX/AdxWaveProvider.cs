using NAudio.Wave;
using System;
using System.Linq;

namespace HaruhiChokuretsuLib.Audio.ADX;

/// <summary>
/// An implementation of IWaveProvider for ADX audio
/// </summary>
/// <remarks>
/// Creates an ADXWaveProvider
/// </remarks>
/// <param name="decoder">The ADX decoder to use</param>
/// <param name="loopEnabled">(Optional) Sets looping enabled/disabled</param>
/// <param name="loopStartSample">(Optional) Sample to start looping from</param>
/// <param name="loopEndSample">(Optional) Last sample to play before starting loop over</param>
public class AdxWaveProvider(IAdxDecoder decoder, bool loopEnabled = false, uint loopStartSample = 0, uint loopEndSample = 0) : IWaveProvider
{
    /// <summary>
    /// If enabled, will loop the audio
    /// </summary>
    public bool LoopEnabled { get; } = loopEnabled;
    /// <summary>
    /// Sample to start looping from
    /// </summary>
    public uint LoopStartSample { get; } = loopStartSample;
    /// <summary>
    /// Last sample to play before starting loop over
    /// </summary>
    public uint LoopEndSample { get; } = loopEndSample;

    /// <inheritdoc/>
    public WaveFormat WaveFormat { get; } = new((int)decoder.SampleRate, (int)decoder.Channels);

    /// <inheritdoc/>
    public int Read(Span<byte> buffer)
    {
        int i = 0;
        while (i < buffer.Length)
        {
            Sample nextSample = decoder.NextSample();
            if (nextSample is null)
            {
                return i;
            }
            Span<byte> bytes = nextSample.SelectMany(BitConverter.GetBytes).ToArray();
            bytes.CopyTo(buffer[i..]);
            i += bytes.Length;
        }
        return i;
    }
}