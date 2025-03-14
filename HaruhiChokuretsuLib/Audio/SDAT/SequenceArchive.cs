﻿// These files are taken from Nitro Studio 2
// https://github.com/Gota7/NitroStudio2
// They have been modified to work with .NET 6.0
// Nitro Studio 2 is created by Gota 7
// It is not explicitly licensed, but given that its
// components are licensed under GPLv3, we can assume
// it is also GPLv3 compatible
using GotaSequenceLib;
using GotaSoundIO.IO;
using HaruhiChokuretsuLib.Audio.SDAT.SoundArchiveComponents;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiChokuretsuLib.Audio.SDAT;

/// <summary>
/// A sequence archive. It's still a sequence. TODO: OVERRIDE LOADING AND SAVING FROM TEXT!!!
/// </summary>
public class SequenceArchive : SequenceFile
{
    /// <summary>
    /// Sequences. The index of each one of these is the same as the public label.
    /// </summary>
    public List<SequenceArchiveSequence> Sequences = [];

    /// <summary>
    /// Read the sequence archive.
    /// </summary>
    /// <param name="r">The reader.</param>
    public override void Read(FileReader r)
    {
        //Open the data block.
        r.OpenFile<NHeader>(out _);
        r.OpenBlock(0, out _, out uint dataSize);
        long bakPos = r.Position - 8;
        uint seqDataOffset = r.ReadUInt32();
        uint numSeqs = r.ReadUInt32();

        //Read entries.
        Labels = new();
        Sequences = [];
        for (uint i = 0; i < numSeqs; i++)
        {
            uint off = r.ReadUInt32();
            if (off != 0xFFFFFFFF)
            {
                Labels.Add("Sequence_" + i, off);
                Sequences.Add(r.Read<SequenceArchiveSequence>());
                Sequences.Last().Index = (int)i;
            }
            else
            {
                r.ReadUInt64();
            }
        }

        //Data.
        r.Jump(seqDataOffset, true);
        var data = r.ReadBytes((int)(dataSize - (r.Position - bakPos))).ToList();

        //Remove padding.
        for (int i = data.Count - 1; i >= 0; i--)
        {
            if (data[i] == 0)
            {
                data.RemoveAt(i);
            }
            else
            {
                break;
            }
        }

        //Set raw data.
        RawData = [.. data];
    }

    /// <summary>
    /// Write the sequence archive.
    /// </summary>
    /// <param name="w">The writer.</param>
    public override void Write(FileWriter w)
    {
        //Write the data.
        w.InitFile<NHeader>("SSAR", ByteOrder.LittleEndian, null, 1);
        w.InitBlock("DATA");

        //Write each sequence.
        Sequences = [.. Sequences.OrderBy(x => x.Index)];
        if (Sequences.Count > 0)
        {
            w.Write((uint)(0x20 + 12 * (Sequences.Last().Index + 1)));
            w.Write((uint)(Sequences.Last().Index + 1));
            int num = 0;
            for (int i = 0; i <= Sequences.Last().Index; i++)
            {
                var e = Sequences.Where(x => x.Index == i).FirstOrDefault();
                if (e == null)
                {
                    w.Write((uint)0xFFFFFFFF);
                    w.Write((ulong)0);
                }
                else
                {
                    w.Write(Labels.Values.ElementAt(num));
                    w.Write(e);
                    num++;
                }
            }
        }
        else
        {
            w.Write((uint)0x20);
            w.Write((uint)0);
        }

        //Write data and end.
        w.Write(RawData);
        w.Pad(4);
        w.CloseBlock();
        w.CloseFile();
    }

    /// <summary>
    /// Get the platform.
    /// </summary>
    /// <returns>The platform.</returns>
    public override SequencePlatform Platform() => new Nitro();

    /// <summary>
    /// Convert the file to text.
    /// </summary>
    /// <returns>The file as text.</returns>
    public new string[] ToText()
    {
        //Command list.
        List<string> l =
        [
            ";;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;",
            ";",
            $";     {Name}.mus",
            ";     Generated by ChokuretsuTranslationLib",
            ";     Using Code Borrowed From Nitro Studio 2",
            ";;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;",
            "",

            //Add sequence table.
            "@SEQ_TABLE",
        ];
        for (int i = 0; i < Sequences.Count; i++)
        {
            string s = "";
            var e = Sequences[i];
            s += (e.Name ?? "Sequence_" + Sequences[i].Index) + " = " + Sequences[i].Index + ":\t";
            s += Labels.Keys.ElementAt(i) + ",\t";
            s += (e.Bank == null ? e.ReadingBankId.ToString() : e.Bank.Name) + ",\t";
            s += e.Volume + ",\t";
            s += e.ChannelPriority + ",\t";
            s += e.PlayerPriority + ",\t";
            s += (e.Player == null ? e.ReadingPlayerId.ToString() : e.Player.Name);
            l.Add(s);
        }

        //Add sequence data.
        l.Add("");
        l.Add("@SEQ_DATA");

        //For each command. Last one isn't counted.
        for (int i = 0; i < Commands.Count - 1; i++)
        {

            //Add labels.
            bool labelAdded = false;
            var labels = PublicLabels.Where(x => x.Value == i).Select(x => x.Key);
            foreach (var label in labels)
            {
                if (i != 0 && !labelAdded && Commands[i - 1].CommandType == SequenceCommands.Fin)
                {
                    l.Add(" ");
                }
                l.Add(label + ":");
                labelAdded = true;
            }
            if (OtherLabels.Contains(i))
            {
                if (i != 0 && !labelAdded && Commands[i - 1].CommandType == SequenceCommands.Fin)
                {
                    l.Add(" ");
                }
                l.Add("Command_" + i + ":");
                labelAdded = true;
            }

            //Add command.
            l.Add("\t" + Commands[i].ToString());

        }

        //Return the list.
        return [.. l];

    }

    /// <summary>
    /// From text.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    public new void FromText(List<string> text)
    {
        FromText(text, null);
    }

    /// <summary>
    /// From text.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="a">Sound archive.</param>
    public void FromText(List<string> text, SoundArchive a)
    {
        //Success by default.
        WritingCommandSuccess = true;

        //Get platform.
        var p = Platform();

        //Reset labels.
        PublicLabels = new();
        OtherLabels = [];
        Dictionary<string, int> privateLabels = new Dictionary<string, int>();
        List<int> labelLines = [];

        //Format text.
        List<string> t = [.. text];
        int comNum = 0;
        for (int i = t.Count - 1; i >= 0; i--)
        {
            t[i] = t[i].Replace("\t", "").Replace("\r", "");
            try { t[i] = t[i].Split(';')[0]; } catch { }
            if (t[i].Replace(" ", "").Length == 0) { t.RemoveAt(i); continue; }
            for (int j = 0; j < t[i].Length; j++)
            {
                if (t[i][j].Equals(' '))
                {
                    t[i] = t[i].Substring(j + 1);
                    j--;
                }
                else
                {
                    break;
                }
            }
        }

        //Sequence id to label name.
        Dictionary<int, string> seqId2Label = new Dictionary<int, string>();

        //Get sequences.
        Sequences = [];
        int currSeqId = 0;
        for (int i = t.IndexOf("@SEQ_TABLE") + 1; i < t.IndexOf("@SEQ_DATA"); i++)
        {

            //New sequence.
            SequenceArchiveSequence s = new SequenceArchiveSequence();

            //Get sequence data.
            string[] seqData = t[i].Replace("\n", "").Replace(" ", "").Replace("\t", "").Replace("\r", "").Split(',');

            //Get sequence.
            string label = seqData[0].Split(':')[1];
            string seqNameData = seqData[0].Split(':')[0];
            if (seqNameData.Contains("="))
            {
                currSeqId = int.Parse(seqNameData.Split('=')[1]);
                s.Name = seqNameData.Split('=')[0];
            }
            else
            {
                s.Name = seqNameData;
            }
            s.Index = currSeqId;
            seqId2Label.Add(currSeqId++, label);
            s.LabelName = label;

            //Bank.
            string bnk = seqData[1];
            if (ushort.TryParse(bnk, out _))
            {
                s.ReadingBankId = ushort.Parse(bnk);
                if (a != null)
                {
                    s.Bank = a.Banks.Where(x => x.Index == s.ReadingBankId).FirstOrDefault();
                }
            }
            else if (a != null)
            {
                var bnkReal = a.Banks.Where(x => x.Name.Equals(bnk)).FirstOrDefault();
                if (bnkReal == null)
                {
                    throw new("Bank " + bnk + " does not exist!");
                }
                s.ReadingBankId = (ushort)bnkReal.Index;
                s.Bank = bnkReal;
            }
            else
            {
                throw new("Can't use a name when there is no sound archive open!");
            }

            //Data.
            s.Volume = byte.Parse(seqData[2]);
            s.ChannelPriority = byte.Parse(seqData[3]);
            s.PlayerPriority = byte.Parse(seqData[4]);

            //Player
            string ply = seqData[5];
            if (ushort.TryParse(ply, out _))
            {
                s.ReadingPlayerId = byte.Parse(ply);
                if (a != null)
                {
                    s.Player = a.Players.Where(x => x.Index == s.ReadingPlayerId).FirstOrDefault();
                }
            }
            else if (a != null)
            {
                var plyReal = a.Players.Where(x => x.Name.Equals(ply)).FirstOrDefault();
                if (plyReal == null)
                {
                    throw new("Player " + ply + " does not exist!");
                }
                s.ReadingPlayerId = (byte)plyReal.Index;
                s.Player = plyReal;
            }
            else
            {
                throw new("Can't use a name when there is no sound archive open!");
            }

            //Add sequence.
            Sequences.Add(s);

        }

        //Fetch labels.
        int strt = t.IndexOf("@SEQ_DATA") + 1;
        for (int i = strt; i < t.Count; i++)
        {

            //If it's a label.
            if (t[i].EndsWith(":"))
            {
                labelLines.Add(i);
                string lbl = t[i].Replace(":", "");
                if (!seqId2Label.ContainsValue(lbl))
                {
                    privateLabels.Add(lbl, comNum);
                    OtherLabels.Add(comNum);
                }
                else
                {
                    PublicLabels.Add(lbl, comNum);
                }
            }
            else
            {
                comNum++;
            }

        }

        //Get commands.
        Commands = [];
        for (int i = t.IndexOf("@SEQ_DATA") + 1; i < t.Count; i++)
        {
            if (labelLines.Contains(i))
            {
                continue;
            }
            SequenceCommand seq = new SequenceCommand();
            try { seq.FromString(t[i], PublicLabels, privateLabels); } catch { WritingCommandSuccess = false; throw new("Command " + i + ": \"" + t[i] + "\" is invalid."); }
            Commands.Add(seq);
        }

        //Fin.
        Commands.Add(new() { CommandType = SequenceCommands.Fin });

        //Backup labels.
        var bakLabels = PublicLabels;
        PublicLabels = new();
        foreach (var seq in Sequences)
        {
            PublicLabels.Add(seq.Name, bakLabels[seq.LabelName]);
        }
    }
}

/// <summary>
/// A sequence inside of the sequence archive.
/// </summary>
public class SequenceArchiveSequence : IReadable, IWriteable
{
    /// <summary>
    /// Sequence archive name.
    /// </summary>
    public string Name;

    /// <summary>
    /// Sequence index.
    /// </summary>
    public int Index;

    /// <summary>
    /// Bank.
    /// </summary>
    public BankInfo Bank;

    /// <summary>
    /// Player.
    /// </summary>
    public PlayerInfo Player;

    /// <summary>
    /// Volume.
    /// </summary>
    public byte Volume = 100;

    /// <summary>
    /// Channel priority.
    /// </summary>
    public byte ChannelPriority = 0x40;

    /// <summary>
    /// Player priority.
    /// </summary>
    public byte PlayerPriority = 0x40;

    /// <summary>
    /// Reading bank Id.
    /// </summary>
    public ushort ReadingBankId;

    /// <summary>
    /// Reading player Id.
    /// </summary>
    public byte ReadingPlayerId;

    /// <summary>
    /// Label name. FOR CONVERSION ONLY, DON'T USE!
    /// </summary>
    public string LabelName;

    /// <summary>
    /// Read the sequence info.
    /// </summary>
    /// <param name="r">The reader.</param>
    public void Read(FileReader r)
    {
        ReadingBankId = r.ReadUInt16();
        Volume = r.ReadByte();
        ChannelPriority = r.ReadByte();
        PlayerPriority = r.ReadByte();
        ReadingPlayerId = r.ReadByte();
        r.ReadUInt16();
    }

    /// <summary>
    /// Write the sequence info.
    /// </summary>
    /// <param name="w">The writer.</param>
    public void Write(FileWriter w)
    {
        w.Write(Bank != null ? (ushort)Bank.Index : (ushort)ReadingBankId);
        w.Write(Volume);
        w.Write(ChannelPriority);
        w.Write(PlayerPriority);
        w.Write(Player != null ? (byte)Player.Index : (byte)ReadingPlayerId);
        w.Write((ushort)0);
    }
}