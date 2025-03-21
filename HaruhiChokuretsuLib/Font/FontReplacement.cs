using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace HaruhiChokuretsuLib.Font;

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
    /// <summary>
    /// If true, indicates that this character causes previous characters that take offset adjustments to decrement its offset by 1
    /// </summary>
    public bool CauseOffsetAdjust { get; set; }
    /// <summary>
    /// If true, indicates that this character will decrement its offset by 1 if the next character causes offset adjustments
    /// </summary>
    public bool TakeOffsetAdjust { get; set; }
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

    /// <inheritdoc/>
    public ICollection<char> Keys => (ICollection<char>)_fontReplacements.Select(f => f.ReplacedCharacter);

    /// <inheritdoc/>
    public ICollection<FontReplacement> Values => _fontReplacements;

    /// <inheritdoc/>
    public int Count => _fontReplacements.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets a replacement character given an original character
    /// </summary>
    /// <param name="originalCharacter">The original character to get</param>
    /// <returns></returns>
    public FontReplacement GetReplacementCharacter(char originalCharacter) => _fontReplacements.FirstOrDefault(f => f.OriginalCharacter == originalCharacter);

    /// <summary>
    /// Adds a font replacement to the dictionary
    /// </summary>
    /// <param name="value"></param>
    public void Add(FontReplacement value)
    {
        _fontReplacements.Add(value);
    }

    /// <summary>
    /// Adds a range of font replacements to the dictionary
    /// </summary>
    /// <param name="values"></param>
    public void AddRange(IEnumerable<FontReplacement> values)
    {
        _fontReplacements.AddRange(values);
    }

    /// <inheritdoc/>
    public void Add(char key, FontReplacement value)
    {
        if (key != value.ReplacedCharacter)
        {
            throw new ArgumentException($"Replacement character '{value.ReplacedCharacter}' did not match key '{key}'");
        }
        _fontReplacements.Add(value);
    }

    /// <inheritdoc/>
    public void Add(KeyValuePair<char, FontReplacement> item)
    {
        Add(item.Key, item.Value);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _fontReplacements.Clear();
    }

    /// <inheritdoc/>
    public bool Contains(KeyValuePair<char, FontReplacement> item)
    {
        return _fontReplacements.Contains(item.Value);
    }

    /// <inheritdoc/>
    public bool ContainsKey(char key)
    {
        return _fontReplacements.Any(f => f.ReplacedCharacter == key);
    }

    /// <inheritdoc/>
    public void CopyTo(KeyValuePair<char, FontReplacement>[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<char, FontReplacement>> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public bool Remove(KeyValuePair<char, FontReplacement> item)
    {
        return _fontReplacements.Remove(item.Value);
    }

    /// <inheritdoc/>
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