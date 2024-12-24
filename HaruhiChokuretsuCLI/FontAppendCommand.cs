using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Util;
using Mono.Options;
using SkiaSharp;

namespace HaruhiChokuretsuCLI;

public class FontAppendCommand : Command
{
    private string _datPath, _imagePath, _fontPath, _charsetPath, _pathToAppendFile;
    private int _offset;
    private SKTextAlign _textAlign = SKTextAlign.Left;
    
    public FontAppendCommand() : base("font-append", "Appends characters to the modified font")
    {
        Options = new()
        {
            { "d|dat=", "The path to dat.bin", d => _datPath = d },
            { "i|image=", "The path to the font PNG", i => _imagePath = i },
            { "f|font=", "The path to the font file", f => _fontPath = f },
            { "c|charset=", "The path to the charset JSON", c => _charsetPath = c },
            { "o|offset=", "The charmap offset to begin appending at", o => _offset = int.Parse(o) },
            { "n|alignment=", "Text alignment (left, right, center)", n => _textAlign = Enum.Parse<SKTextAlign>(n.ToUpper()) },
            { "a|append=", "The path to a UTF-16 encoded file with characters to append", a => _pathToAppendFile = a },
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        ConsoleLogger log = new();
        ArchiveFile<DataFile> datBin = ArchiveFile<DataFile>.FromFile(_datPath, log);
        FontFile chokuFontFile = datBin.GetFileByName("FONTS").CastTo<FontFile>();
        
        SKBitmap fontImage = SKBitmap.Decode(_imagePath);
        SKCanvas canvas = new SKCanvas(fontImage);
        string characters = File.ReadAllText(_pathToAppendFile, Encoding.UTF8);
        SKFont font = new(SKTypeface.FromFile(_fontPath), 10f)
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
            canvas.DrawRect(0, yOffset, 16, 16, new() { Color = SKColors.Black });
            canvas.DrawText($"{@char}", 0, yOffset + 8, _textAlign, font, paint);
            char original = chokuFontFile.CharMap[_offset++];
            
            replacementDict.Add(@char, new()
            {
                OriginalCharacter = original,
                ReplacedCharacter = @char,
                CodePoint = BitConverter.ToUInt16(Encoding.GetEncoding("Shift-JIS").GetBytes($"{original}")),
                Offset = (int)Math.Ceiling(font.GetGlyphWidths($"{@char}", new(font))[0]),
            });
        }
        
        canvas.Flush();
        using FileStream fs = File.OpenWrite(_imagePath);
        fontImage.Encode(fs, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
        
        File.WriteAllText(_charsetPath, JsonSerializer.Serialize(replacementDict.Values.ToArray(), new JsonSerializerOptions() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)}));
        
        return 0;
    }
}