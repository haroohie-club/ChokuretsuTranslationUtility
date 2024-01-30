using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using System;
using System.Collections.Generic;

namespace HaruhiChokuretsuLib.Save
{
    /// <summary>
    /// Represents a dynamic save slot's data (slot 3)
    /// </summary>
    /// <remarks>
    /// Creates a static save slot representation given binary data
    /// </remarks>
    /// <param name="data">The binary data of the save slot</param>
    public class QuickSaveSlotData(IEnumerable<byte> data) : SaveSlotData(data)
    {
        /// <summary>
        /// The first character sprite to display
        /// </summary>
        public int FirstCharacterSprite { get; set; } = IO.ReadInt(data, 0x3EC);
        /// <summary>
        /// The second character sprite to display
        /// </summary>
        public int SecondCharacterSprite { get; set; } = IO.ReadInt(data, 0x3F0);
        /// <summary>
        /// The third character sprite to display
        /// </summary>
        public int ThirdCharacterSprite { get; set; } = IO.ReadInt(data, 0x3F4);
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown3F8 { get; set; } = IO.ReadShort(data, 0x3F8);
        /// <summary>
        /// The horizontal offset of the first character sprite
        /// </summary>
        public short Sprite1XOffset { get; set; } = IO.ReadShort(data, 0x3FA);
        /// <summary>
        /// The horizontal offset of the second character sprite
        /// </summary>
        public short Sprite2XOffset { get; set; } = IO.ReadShort(data, 0x3FC);
        /// <summary>
        /// The horizontal offset of the third character sprite
        /// </summary>
        public short Sprite3XOffset { get; set; } = IO.ReadShort(data, 0x3FE);
        /// <summary>
        /// Unknown
        /// </summary>
        public CharacterMask TopScreenChibis { get; set; } = (CharacterMask)IO.ReadInt(data, 0x400);
        /// <summary>
        /// The episode header to display as with EPHEADER
        /// </summary>
        public short EpisodeHeader { get; set; } = IO.ReadShort(data, 0x404);
        /// <summary>
        /// The PLACE.S index of the place graphic to display on the top screen
        /// </summary>
        public short Place { get; set; } = IO.ReadShort(data, 0x406);
        /// <summary>
        /// The palette effect to apply to the BG as with PALEFFECT
        /// </summary>
        public short BgPalEffect { get; set; } = IO.ReadShort(data, 0x408);
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown40A { get; set; } = IO.ReadShort(data, 0x40A);
        /// <summary>
        /// The BGTBL.S index of the background to display on the bottom screen
        /// </summary>
        public short BgIndex { get; set; } = IO.ReadShort(data, 0x40C);
        /// <summary>
        /// The BGTBL.S index of the KBG to display on the top screen
        /// </summary>
        public short KbgIndex { get; set; } = IO.ReadShort(data, 0x40E);
        /// <summary>
        /// The BGTBL.S index of the CG displayed with DISP_CG
        /// </summary>
        public short CgIndex { get; set; } = IO.ReadShort(data, 0x410);
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown412 { get; set; } = IO.ReadShort(data, 0x412);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown414 { get; set; } = IO.ReadInt(data, 0x414);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown418 { get; set; } = IO.ReadInt(data, 0x418);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown41C { get; set; } = IO.ReadInt(data, 0x41C);
        /// <summary>
        /// The script where the save was made
        /// </summary>
        public int CurrentScript { get; set; } = IO.ReadInt(data, 0x420);
        /// <summary>
        /// The script block in the script where the save was made
        /// </summary>
        public int CurrentScriptBlock { get; set; } = IO.ReadInt(data, 0x424);
        /// <summary>
        /// The index of the command in the script block where the save was made
        /// </summary>
        public int CurrentScriptCommand { get; set; } = IO.ReadInt(data, 0x428);

        /// <inheritdoc/>
        public override void Clear()
        {
            base.Clear();
            FirstCharacterSprite = 0;
            SecondCharacterSprite = 0;
            ThirdCharacterSprite = 0;
            Unknown3F8 = 0;
            Sprite1XOffset = 0;
            Sprite2XOffset = 0;
            Sprite3XOffset = 0;
            TopScreenChibis = 0;
            EpisodeHeader = 0;
            Place = 0;
            BgPalEffect = 0;
            Unknown40A = 0;
            BgIndex = 0;
            KbgIndex = 0;
            CgIndex = 0;
            Unknown412 = 0;
            Unknown414 = 0;
            Unknown418 = 0;
            Unknown41C = 0;
            CurrentScript = 0;
            CurrentScriptBlock = 0;
            CurrentScriptCommand = 0;
        }

        /// <summary>
        /// Get the binary representation of the data portion of the section not including the checksum
        /// </summary>
        /// <returns>Byte array of the checksumless binary data</returns>
        protected override byte[] GetDataBytes()
        {
            List<byte> data = [.. base.GetDataBytes()];

            data.RemoveRange(data.Count - 5, 4);
            data.AddRange(BitConverter.GetBytes(FirstCharacterSprite));
            data.AddRange(BitConverter.GetBytes(SecondCharacterSprite));
            data.AddRange(BitConverter.GetBytes(ThirdCharacterSprite));
            data.AddRange(BitConverter.GetBytes(Unknown3F8));
            data.AddRange(BitConverter.GetBytes(Sprite1XOffset));
            data.AddRange(BitConverter.GetBytes(Sprite2XOffset));
            data.AddRange(BitConverter.GetBytes(Sprite3XOffset));
            data.AddRange(BitConverter.GetBytes((int)TopScreenChibis));
            data.AddRange(BitConverter.GetBytes(EpisodeHeader));
            data.AddRange(BitConverter.GetBytes(Place));
            data.AddRange(BitConverter.GetBytes(BgPalEffect));
            data.AddRange(BitConverter.GetBytes(Unknown40A));
            data.AddRange(BitConverter.GetBytes(BgIndex));
            data.AddRange(BitConverter.GetBytes(KbgIndex));
            data.AddRange(BitConverter.GetBytes(CgIndex));
            data.AddRange(BitConverter.GetBytes(Unknown412));
            data.AddRange(BitConverter.GetBytes(Unknown414));
            data.AddRange(BitConverter.GetBytes(Unknown418));
            data.AddRange(BitConverter.GetBytes(Unknown41C));
            data.AddRange(BitConverter.GetBytes(CurrentScript));
            data.AddRange(BitConverter.GetBytes(CurrentScriptBlock));
            data.AddRange(BitConverter.GetBytes(CurrentScriptCommand));
            data.AddRange(new byte[4]);

            return [.. data];
        }
    }
}
