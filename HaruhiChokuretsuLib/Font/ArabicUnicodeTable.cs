using System.Collections.Generic;

namespace HaruhiChokuretsuLib.Font;

// Adapted from https://github.com/yahya99223/C-Sharp-Arabic-Reshaper/
internal static class ArabicUnicodeTable
{
    public static Dictionary<string, string[]> ArabicGlyphs = new()
    {
        { "\\u0622", [ "\\uFE81", "\\uFE81", "\\uFE82", "\\uFE82", "2" ] },
        { "\\u0623", [ "\\uFE83", "\\uFE83", "\\uFE84", "\\uFE84", "2" ] },
        { "\\u0624", [ "\\uFE85", "\\uFE85", "\\uFE86", "\\uFE86", "2" ] },
        { "\\u0625", [ "\\uFE87", "\\uFE87", "\\uFE88", "\\uFE88", "2" ] },
        { "\\u0626", [ "\\uFE89", "\\uFE8B", "\\uFE8C", "\\uFE8A", "4" ] },
        { "\\u0627", [ "\\u0627", "\\u0627", "\\uFE8E", "\\uFE8E", "2" ] },
        { "\\u0628", [ "\\uFE8F", "\\uFE91", "\\uFE92", "\\uFE90", "4" ] },
        { "\\u0629", [ "\\uFE93", "\\uFE93", "\\uFE94", "\\uFE94", "2" ] },
        { "\\u0630", [ "\\uFEAB", "\\uFEAB", "\\uFEAC", "\\uFEAC", "2" ] },
        { "\\u0631", [ "\\uFEAD", "\\uFEAD", "\\uFEAE", "\\uFEAE", "2" ] },
        { "\\u0632", [ "\\uFEAF", "\\uFEAF", "\\uFEB0", "\\uFEB0", "2" ] },
        { "\\u0633", [ "\\uFEB1", "\\uFEB3", "\\uFEB4", "\\uFEB2", "4" ] },
        { "\\u0634", [ "\\uFEB5", "\\uFEB7", "\\uFEB8", "\\uFEB6", "4" ] },
        { "\\u0635", [ "\\uFEB9", "\\uFEBB", "\\uFEBC", "\\uFEBA", "4" ] },
        { "\\u0636", [ "\\uFEBD", "\\uFEBF", "\\uFEC0", "\\uFEBE", "4" ] },
        { "\\u0637", [ "\\uFEC1", "\\uFEC3", "\\uFEC4", "\\uFEC2", "4" ] },
        { "\\u0638", [ "\\uFEC5", "\\uFEC7", "\\uFEC8", "\\uFEC6", "4" ] },
        { "\\u0639", [ "\\uFEC9", "\\uFECB", "\\uFECC", "\\uFECA", "4" ] },
        { "\\u0641", [ "\\uFED1", "\\uFED3", "\\uFED4", "\\uFED2", "4" ] },
        { "\\u0642", [ "\\uFED5", "\\uFED7", "\\uFED8", "\\uFED6", "4" ] },
        { "\\u0643", [ "\\uFED9", "\\uFEDB", "\\uFEDC", "\\uFEDA", "4" ] },
        { "\\u0644", [ "\\uFEDD", "\\uFEDF", "\\uFEE0", "\\uFEDE", "4" ] },
        { "\\u0645", [ "\\uFEE1", "\\uFEE3", "\\uFEE4", "\\uFEE2", "4" ] },
        { "\\u0646", [ "\\uFEE5", "\\uFEE7", "\\uFEE8", "\\uFEE6", "4" ] },
        { "\\u0647", [ "\\uFEE9", "\\uFEEB", "\\uFEEC", "\\uFEEA", "4" ] },
        { "\\u0648", [ "\\uFEED", "\\uFEED", "\\uFEEE", "\\uFEEE", "2" ] },
        { "\\u0649", [ "\\uFEEF", "\\uFEEF", "\\uFEF0", "\\uFEF0", "2" ] },
        { "\\u0671", [ "\\u0671", "\\u0671", "\\uFB51", "\\uFB51", "2" ] },
    };
}