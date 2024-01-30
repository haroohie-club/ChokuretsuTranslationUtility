using System.Collections.Generic;

namespace HaruhiChokuretsuLib.Archive
{
    public interface ITranslatableFile
    {
        /// <summary>
        /// Obtains a list of translatable strings for translation software
        /// </summary>
        /// <returns>Gets the list of translatable strings for translation software</returns>
        public List<TranslatableString> GetTranslatableStrings();

        /// <summary>
        /// Replaces the translatable strings in the file
        /// </summary>
        /// <param name="newTranslations">A list of translated strings to reinsert into the file</param>
        public void ReplaceTranslatableStrings(List<TranslatableString> newTranslations);
    }

    /// <summary>
    /// A representation of a translatable string in a file
    /// </summary>
    public class TranslatableString()
    {
        /// <summary>
        /// The key to identify the string with translation software
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// Any comments to display about the translated string in the software
        /// </summary>
        public string Comment { get; set; }
        /// <summary>
        /// The string to translate
        /// </summary>
        public string Line { get; set; }
    }
}
