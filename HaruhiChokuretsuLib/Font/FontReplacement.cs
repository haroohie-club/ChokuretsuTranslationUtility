using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace HaruhiChokuretsuLib.Font
{
    /// <summary>
    /// A representation of a font replacement (used for translation)
    /// </summary>
    public class FontReplacement
    {
        /// <summary>
        /// The original Shift-JIS character to replace
        /// </summary>
        public char OriginalCharacter { get; set; }
        /// <summary>
        /// The character to replace it with
        /// </summary>
        public char ReplacedCharacter { get; set; }
        /// <summary>
        /// The codepoint of the original character
        /// </summary>
        public int CodePoint { get; set; }
        /// <summary>
        /// The width of the replacement character for use by the variable width font hack
        /// </summary>
        public int Offset { get; set; }
    }

    /// <summary>
    /// A special dictionary for font replacement
    /// </summary>
    public class FontReplacementDictionary : IDictionary<char, FontReplacement>
    {
        private readonly List<FontReplacement> _fontReplacements = [];

        /// <summary>
        /// Indexes into a font replacement dictionary
        /// </summary>
        /// <param name="key">The replacement character</param>
        /// <returns>A font replacement object given that replacement character</returns>
        public FontReplacement this[char key]
        {
            get => _fontReplacements.First(f => f.ReplacedCharacter == key);
            set => _fontReplacements[_fontReplacements.FindIndex(f => f.ReplacedCharacter == key)] = value;
        }

        /// <summary>
        /// Looks up a font replacement by original character
        /// </summary>
        /// <param name="key">The original character</param>
        /// <returns>A font replacement object</returns>
        public FontReplacement ReverseLookup(char key)
        {
            return _fontReplacements.FirstOrDefault(f => f.OriginalCharacter == key);
        }

        public ICollection<char> Keys => (ICollection<char>)_fontReplacements.Select(f => f.ReplacedCharacter);

        public ICollection<FontReplacement> Values => _fontReplacements;

        public int Count => _fontReplacements.Count;

        public bool IsReadOnly => false;

        public FontReplacement GetReplacementCharacter(char originalCharacter) => _fontReplacements.FirstOrDefault(f => f.OriginalCharacter == originalCharacter);

        public void Add(FontReplacement value)
        {
            _fontReplacements.Add(value);
        }

        public void AddRange(IEnumerable<FontReplacement> values)
        {
            _fontReplacements.AddRange(values);
        }

        public void Add(char key, FontReplacement value)
        {
            if (key != value.ReplacedCharacter)
            {
                throw new ArgumentException($"Replacement character '{value.ReplacedCharacter}' did not match key '{key}'");
            }
            _fontReplacements.Add(value);
        }

        public void Add(KeyValuePair<char, FontReplacement> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _fontReplacements.Clear();
        }

        public bool Contains(KeyValuePair<char, FontReplacement> item)
        {
            return _fontReplacements.Contains(item.Value);
        }

        public bool ContainsKey(char key)
        {
            return _fontReplacements.Any(f => f.ReplacedCharacter == key);
        }

        public void CopyTo(KeyValuePair<char, FontReplacement>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<char, FontReplacement>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public bool Remove(char key)
        {
            int index = _fontReplacements.FindIndex(f => f.ReplacedCharacter == key);
            if (index == -1)
            {
                return false;
            }
            else
            {
                _fontReplacements.RemoveAt(index);
                return true;
            }
        }

        public bool Remove(KeyValuePair<char, FontReplacement> item)
        {
            return _fontReplacements.Remove(item.Value);
        }

        public bool TryGetValue(char key, [MaybeNullWhen(false)] out FontReplacement value)
        {
            if (ContainsKey(key))
            {
                value = this[key];
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
