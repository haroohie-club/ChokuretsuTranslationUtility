using System.Collections.Generic;

namespace HaruhiChokuretsuLib.Font;

// Adapted from https://github.com/yahya99223/C-Sharp-Arabic-Reshaper/
internal static class ArabicUnicodeTable
{
    public static Dictionary<string, string[]> ArabicGlyphs { get; } = new()
    {
        { "\\u0622", ["\\uFE81", "\\uFE81", "\\uFE82", "\\uFE82", "2"] },
        { "\\u0623", ["\\uFE83", "\\uFE83", "\\uFE84", "\\uFE84", "2"] },
        { "\\u0624", ["\\uFE85", "\\uFE85", "\\uFE86", "\\uFE86", "2"] },
        { "\\u0625", ["\\uFE87", "\\uFE87", "\\uFE88", "\\uFE88", "2"] },
        { "\\u0626", ["\\uFE89", "\\uFE8B", "\\uFE8C", "\\uFE8A", "4"] },
        { "\\u0627", ["\\u0627", "\\u0627", "\\uFE8E", "\\uFE8E", "2"] },
        { "\\u0628", ["\\uFE8F", "\\uFE91", "\\uFE92", "\\uFE90", "4"] },
        { "\\u0629", ["\\uFE93", "\\uFE93", "\\uFE94", "\\uFE94", "2"] },
        { "\\u062A", ["\\uFE95", "\\uFE97", "\\uFE98", "\\uFE96", "4"] },
        { "\\u062B", ["\\uFE99", "\\uFE9B", "\\uFE9C", "\\uFE9A", "4"] },
        { "\\u062C", ["\\uFE9D", "\\uFE9F", "\\uFEA0", "\\uFE9E", "4"] },
        { "\\u062D", ["\\uFEA1", "\\uFEA3", "\\uFEA4", "\\uFEA2", "4"] },
        { "\\u062E", ["\\uFEA5", "\\uFEA7", "\\uFEA8", "\\uFEA6", "4"] },
        { "\\u062F", ["\\uFEA9", "\\uFEA9", "\\uFEAA", "\\uFEAA", "2"] },
        { "\\u0630", ["\\uFEAB", "\\uFEAB", "\\uFEAC", "\\uFEAC", "2"] },
        { "\\u0631", ["\\uFEAD", "\\uFEAD", "\\uFEAE", "\\uFEAE", "2"] },
        { "\\u0632", ["\\uFEAF", "\\uFEAF", "\\uFEB0", "\\uFEB0", "2"] },
        { "\\u0633", ["\\uFEB1", "\\uFEB3", "\\uFEB4", "\\uFEB2", "4"] },
        { "\\u0634", ["\\uFEB5", "\\uFEB7", "\\uFEB8", "\\uFEB6", "4"] },
        { "\\u0635", ["\\uFEB9", "\\uFEBB", "\\uFEBC", "\\uFEBA", "4"] },
        { "\\u0636", ["\\uFEBD", "\\uFEBF", "\\uFEC0", "\\uFEBE", "4"] },
        { "\\u0637", ["\\uFEC1", "\\uFEC3", "\\uFEC4", "\\uFEC2", "4"] },
        { "\\u0638", ["\\uFEC5", "\\uFEC7", "\\uFEC8", "\\uFEC6", "4"] },
        { "\\u0639", ["\\uFEC9", "\\uFECB", "\\uFECC", "\\uFECA", "4"] },
        { "\\u063A", ["\\uFECD", "\\uFECF", "\\uFED0", "\\uFECE", "4"] },
        { "\\u0641", ["\\uFED1", "\\uFED3", "\\uFED4", "\\uFED2", "4"] },
        { "\\u0642", ["\\uFED5", "\\uFED7", "\\uFED8", "\\uFED6", "4"] },
        { "\\u0643", ["\\uFED9", "\\uFEDB", "\\uFEDC", "\\uFEDA", "4"] },
        { "\\u0644", ["\\uFEDD", "\\uFEDF", "\\uFEE0", "\\uFEDE", "4"] },
        { "\\u0645", ["\\uFEE1", "\\uFEE3", "\\uFEE4", "\\uFEE2", "4"] },
        { "\\u0646", ["\\uFEE5", "\\uFEE7", "\\uFEE8", "\\uFEE6", "4"] },
        { "\\u0647", ["\\uFEE9", "\\uFEEB", "\\uFEEC", "\\uFEEA", "4"] },
        { "\\u0648", ["\\uFEED", "\\uFEED", "\\uFEEE", "\\uFEEE", "2"] },
        { "\\u0649", ["\\uFEEF", "\\uFEEF", "\\uFEF0", "\\uFEF0", "2"] },
        { "\\u0671", ["\\u0671", "\\u0671", "\\uFB51", "\\uFB51", "2"] },
        { "\\u064A", ["\\uFEF1", "\\uFEF3", "\\uFEF4", "\\uFEF2", "4"] },
        { "\\u066E", ["\\uFBE4", "\\uFBE8", "\\uFBE9", "\\uFBE5", "4"] },
        { "\\u06AA", ["\\uFB8E", "\\uFB90", "\\uFB91", "\\uFB8F", "4"] },
        { "\\u06C1", ["\\uFBA6", "\\uFBA8", "\\uFBA9", "\\uFBA7", "4"] },
        { "\\u06E4", ["\\u06E4", "\\u06E4", "\\u06E4", "\\uFEEE", "2"] },
    };

    public static Dictionary<string, char> ArabicDiacriticReplacements { get; } = new()
    {
        { "\\u064B", 'E' },
        { "\\u064C", 'D' },
        { "\\u064D", 'C' },
        { "\\u064E", 'B' },
        { "\\u064F", 'A' },
        { "\\u0650", '9' },
        { "\\u0651", '8' },
    };
}