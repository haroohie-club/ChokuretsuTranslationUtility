using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Util;
using Mono.Options;
using SkiaSharp;

namespace HaruhiChokuretsuCLI;

public class FontAppendCommand : Command
{
    private string _datPath, _imagePath, _fontPath, _charsetPath, _cFilePath, _pathToAppendFile;
    private int _offset;
    private float _fontSize = 10f;
    private SKTextAlign _textAlign = SKTextAlign.Left;
    private bool _hasDiacritics;
    
    public FontAppendCommand() : base("font-append", "Appends characters to the modified font")
    {
        Options = new()
        {
            { "d|dat=", "The path to dat.bin", d => _datPath = d },
            { "i|image=", "The path to the font PNG", i => _imagePath = i },
            { "f|font=", "The path to the font file", f => _fontPath = f },
            { "c|charset=", "The path to the charset JSON", c => _charsetPath = c },
            { "s|c-file=", "The path to the fontOffset.c file to output", s => _cFilePath = s },
            { "o|offset=", "The charmap offset to begin appending at", o => _offset = int.Parse(o) },
            { "n|alignment=", "Text alignment (left, right, center)", n => _textAlign = Enum.Parse<SKTextAlign>($"{n[0].ToString().ToUpper()}{n[1..].ToLower()}") },
            { "a|append=", "The path to a UTF-16 encoded file with characters to append", a => _pathToAppendFile = a },
            { "size=", "The font size to use when rendering the font (defaults to 10)", s => _fontSize = float.Parse(s) },
            { "diacritics", "Handle diacritics through a special process I don't want to explain here", _ => _hasDiacritics = true },
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        ConsoleLogger log = new();
        var datBin = ArchiveFile<DataFile>.FromFile(_datPath, log);
        var chokuFontFile = datBin.GetFileByName("FONTS").CastTo<FontFile>();

        string characters = string.Empty;
        Dictionary<char, ushort> diacriticDictionary = [];
        Dictionary<char, string> diacriticsAppliedDictionary = [];
        if (!_hasDiacritics)
        {
            characters = File.ReadAllText(_pathToAppendFile, Encoding.Unicode);
        }
        else
        {
            string[] appendFile = File.ReadAllLines(_pathToAppendFile, Encoding.Unicode);
            characters += appendFile[0];
            characters += appendFile[1];
            foreach (string diacriticLine in appendFile[2..])
            {
                diacriticDictionary.Add(diacriticLine[^1], (ushort)(ushort.Parse(diacriticLine[0].ToString(), NumberStyles.HexNumber) << 12));
            }
            foreach (char diacritic in diacriticDictionary.Keys)
            {
                foreach (char @char in appendFile[1])
                {
                    char modChar = (char)((@char & 0x0FFF) | diacriticDictionary[diacritic]);
                    diacriticsAppliedDictionary.Add(modChar, $"{diacritic}{@char}");
                    characters += modChar;
                }
            }
        }
        
        SKBitmap fontImage = SKBitmap.Decode(_imagePath);
        SKCanvas canvas = new(fontImage);
        SKFont font = new(SKTypeface.FromFile(_fontPath), _fontSize)
        {
            Hinting = SKFontHinting.None,
            Subpixel = false,
            Edging = SKFontEdging.Alias,
        };
        SKPaint paint = new() { Color = SKColors.White };

        FontReplacementDictionary replacementDict = new();
        replacementDict.AddRange(JsonSerializer.Deserialize<FontReplacement[]>(File.ReadAllText(_charsetPath)));
        
        foreach (char @char in characters)
        {
            int yOffset = _offset * 16;
            int yTextOffset = yOffset + 10;
            int xOffset = _textAlign switch
            {
                SKTextAlign.Center => 8,
                SKTextAlign.Right => 13,
                _ => 0,
            };
            string visChar = diacriticsAppliedDictionary.TryGetValue(@char, out string diacriticChar)
                ? diacriticChar
                : $"{@char}";
            canvas.DrawRect(0, yOffset, 14, 16, new() { Color = SKColors.Black });
            canvas.DrawText(visChar, xOffset, yTextOffset, _textAlign, font, paint);
            char original = chokuFontFile.CharMap[_offset++];
            
            replacementDict.Add(@char, new()
            {
                OriginalCharacter = original,
                ReplacedCharacter = @char,
                CodePoint = BitConverter.ToUInt16(Encoding.GetEncoding("Shift-JIS").GetBytes($"{original}").Reverse().ToArray()),
                Offset = (int)Math.Ceiling(font.GetGlyphWidths(visChar, new())[^1]) - 1,
            });
        }
        
        canvas.Flush();
        using FileStream fs = File.OpenWrite(_imagePath);
        fontImage.Encode(fs, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
        
        File.WriteAllText(_charsetPath, JsonSerializer.Serialize(replacementDict.Values.ToArray(), new JsonSerializerOptions() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)}));

        StringBuilder cFileBuilder = new(@"#define FONT_DEFAULT_OFFSET (14)
int font_calculateOffset(unsigned short character)
{
    // Auto-generated code
    switch (character)
    {
");
        foreach (FontReplacement replacement in replacementDict.Values)
        {
            cFileBuilder.AppendLine($"        case 0x{replacement.CodePoint:X4}:");
            cFileBuilder.AppendLine($"            return {replacement.Offset};");
        }
        cFileBuilder.AppendLine(@"        default:
            return FONT_DEFAULT_OFFSET;
    }

    return FONT_DEFAULT_OFFSET;
}");
        File.WriteAllText(_cFilePath, cFileBuilder.ToString());
        
        return 0;
    }
}