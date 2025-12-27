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
    /// <param name="original">The string of shaped Arabic characters to convert</param>
    extension(string original)
    {
        /// <summary>
        /// Get UTF-16 string with "unshaped" (contextual) Arabic characters
        /// </summary>
        /// <returns>A UTF-16 string with unshaped (contextual) Arabic characters</returns>
        public string GetUnShapedUnicode()
        {
            //remove escape characters
            original = Regex.Unescape(original);

            string[] words = original.Split(' ');
            StringBuilder builder = new();
            foreach (string word in words)
            {
                string previous = null;
                int index = 0;
                foreach (char character in word)
                {
                    string shapedUnicode = $@"\u{(int)character:X4}";

                    if (ArabicUnicodeTable.ArabicDiacriticReplacements.TryGetValue(shapedUnicode, out char replacement))
                    {
                        builder[^4] = replacement;
                        index++;
                        continue;
                    }
                
                    //if Unicode doesn't exist in Unicode table then character isn't a letter hence shaped Unicode is fine
                    if (!ArabicUnicodeTable.ArabicGlyphs.ContainsKey(shapedUnicode))
                    {
                        builder.Append(shapedUnicode);
                        previous = null;
                        index++;
                        continue;
                    }

                    //first character in word or previous character isn't a letter
                    if (index == 0 || previous == null)
                    {
                        builder.Append(ArabicUnicodeTable.ArabicGlyphs[shapedUnicode][1]);
                    }
                    else
                    {
                        bool previousCharHasOnlyTwoCases = ArabicUnicodeTable.ArabicGlyphs[previous][4] == "2";
                        //if last character in word
                        if (index == word.Length - 1 || index == word.Length - 2 
                                                     && (ArabicUnicodeTable.ArabicDiacriticReplacements.ContainsKey($@"\u{(int)word[^1]:X4}"))
                                                     || char.IsPunctuation(word[^1]))
                        {
                            if (!string.IsNullOrEmpty(previous) && previousCharHasOnlyTwoCases)
                            {
                                builder.Append(ArabicUnicodeTable.ArabicGlyphs[shapedUnicode][0]);
                            }
                            else
                            {
                                builder.Append(ArabicUnicodeTable.ArabicGlyphs[shapedUnicode][3]);
                            }
                        }
                        else
                        {
                            builder.Append(previousCharHasOnlyTwoCases
                                ? ArabicUnicodeTable.ArabicGlyphs[shapedUnicode][1]
                                : ArabicUnicodeTable.ArabicGlyphs[shapedUnicode][2]);
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
        /// <returns>UTF-16 encoded string</returns>
        public string DecodeEncodedNonAsciiCharacters()
        {
            return Regex.Replace(
                original,
                @"\\u(?<Value>[a-zA-Z0-9]{4})",
                m => ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString());
        }
    }
}