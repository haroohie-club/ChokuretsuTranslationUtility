using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace HaruhiChokuretsuLib.Font
{
    public class FontReplacement
    {
        public char OriginalCharacter { get; set; }
        public char ReplacedCharacter { get; set; }
        public int CodePoint { get; set; }
        public int Offset { get; set; }
    }

    public class FontReplacementDictionary : IDictionary<char, FontReplacement>
    {
        private List<FontReplacement> _fontReplacements = new();

        public FontReplacement this[char key]
        {
            get => _fontReplacements.First(f => f.ReplacedCharacter == key);
            set => _fontReplacements[_fontReplacements.FindIndex(f => f.ReplacedCharacter == key)] = value;
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
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
