using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HaruhiChokuretsuLib.Font;

// Adapted from https://github.com/yahya99223/C-Sharp-Arabic-Reshaper/
/// <summary>
/// A static class of extension methods for dealing with Arabic text
/// </summary>
public static class ArabicExtensions
{
    /// <summary>
    /// Get shaped Arabic text as UTF-16 string
    /// </summary>
    /// <param name="shapedText">Shaped Arabic text</param>
    /// <returns>Returns UTF-16 encoded string of Arabic text</returns>
    public static string GetAsUnicode(this string shapedText)
    {
        shapedText = Regex.Unescape(shapedText.Trim());
        var words = shapedText.Split(' ');
        StringBuilder builder = new StringBuilder();
        foreach (var word in words)
        {
            for (int i = 0; i < word.Length; i++)
            {
                string shapedUnicode = @"\u" + ((int)word[i]).ToString("X4");
                builder.Append(shapedUnicode);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Get UTF-16 string with "unshaped" (contextual) Arabic characters
    /// </summary>
    /// <param name="original">The string of shaped Arabic characters to convert</param>
    /// <returns>A UTF-16 string with unshaped (contextual) Arabic characters</returns>
    public static string GetUnShapedUnicode(this string original)
    {
        //remove escape characters
        original = Regex.Unescape(original.Trim());

        var words = original.Split(' ');
        StringBuilder builder = new StringBuilder();
        foreach (var word in words)
        {
            string previous = null;
            int index = 0;
            foreach (var character in word)
            {
                string shapedUnicode = @"\u" + ((int)character).ToString("X4");

                //if Unicode doesn't exist in Unicode table then character isn't a letter hence shaped Unicode is fine
                if (!ArabicUnicodeTable.ArabicGlyphs.ContainsKey(shapedUnicode))
                {
                    builder.Append(shapedUnicode);
                    previous = null;
                    continue;
                }
                else
                {
                    //first character in word or previous character isn't a letter
                    if (index == 0 || previous == null)
                    {
                        builder.Append(ArabicUnicodeTable.ArabicGlyphs[shapedUnicode][1]);
                    }
                    else
                    {
                        bool previousCharHasOnlyTwoCases = ArabicUnicodeTable.ArabicGlyphs[previous][4] == "2";
                        //if last character in word
                        if (index == word.Length - 1)
                        {
                            if (!string.IsNullOrEmpty(previous) && previousCharHasOnlyTwoCases)
                            {
                                builder.Append(ArabicUnicodeTable.ArabicGlyphs[shapedUnicode][0]);
                            }
                            else
                                builder.Append(ArabicUnicodeTable.ArabicGlyphs[shapedUnicode][3]);
                        }
                        else
                        {
                            if (previousCharHasOnlyTwoCases)
                                builder.Append(ArabicUnicodeTable.ArabicGlyphs[shapedUnicode][1]);
                            else
                                builder.Append(ArabicUnicodeTable.ArabicGlyphs[shapedUnicode][2]);
                        }
                    }
                }

                previous = shapedUnicode;
                index++;
            }

            //if not last word then add a space Unicode
            if (words.ToList().IndexOf(word) != words.Length - 1)
                builder.Append(@"\u" + ((int)' ').ToString("X4"));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Decodes encoded non-ASCII characters
    /// </summary>
    /// <param name="value">A string</param>
    /// <returns>UTF-16 encoded string</returns>
    public static string DecodeEncodedNonAsciiCharacters(this string value)
    {
        return Regex.Replace(
            value,
            @"\\u(?<Value>[a-zA-Z0-9]{4})",
            m => ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString());
    }
}