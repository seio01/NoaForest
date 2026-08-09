using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class BadWordFilter
{
    private const string RESOURCE_PATH = "Data/BadWords";

    private readonly HashSet<string> _exactWords = new();
    private readonly List<string> _containsWords = new();
    private Task _initializeTask;

    public bool IsInitialized { get; private set; }

    public Task InitializeAsync()
    {
        _initializeTask ??= InitializeInternalAsync();
        return _initializeTask;
    }

    public bool ContainsBadWord(string value)
    {
        if (!IsInitialized || string.IsNullOrWhiteSpace(value))
            return false;

        string comparisonKey = CreateComparisonKey(value);
        if (string.IsNullOrEmpty(comparisonKey))
            return false;

        if (_exactWords.Contains(comparisonKey))
            return true;

        foreach (string badWord in _containsWords)
        {
            if (comparisonKey.Contains(badWord))
                return true;
        }

        return false;
    }

    private async Task InitializeInternalAsync()
    {
        TextAsset textAsset = await Managers.Resource.LoadTaskAsync<TextAsset>(RESOURCE_PATH);
        BuildLookup(textAsset.text);
        IsInitialized = true;

        Debug.Log($"[BadWordFilter] Loaded {_exactWords.Count + _containsWords.Count:N0} rules.");
    }

    private void BuildLookup(string content)
    {
        _exactWords.Clear();
        _containsWords.Clear();

        HashSet<string> uniqueWords = new();
        string[] lines = content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            string comparisonKey = CreateComparisonKey(line);
            if (string.IsNullOrEmpty(comparisonKey) || !uniqueWords.Add(comparisonKey))
                continue;

            if (IsShortAsciiWord(comparisonKey))
                _exactWords.Add(comparisonKey);
            else
                _containsWords.Add(comparisonKey);
        }

        _containsWords.Sort((left, right) => right.Length.CompareTo(left.Length));
    }

    private static string CreateComparisonKey(string value)
    {
        string normalized = value.Normalize(NormalizationForm.FormKC).Trim().ToLowerInvariant();
        StringBuilder builder = new(normalized.Length);

        foreach (char character in normalized)
        {
            if (char.IsLetterOrDigit(character) || IsHangulCharacter(character))
                builder.Append(character);
        }

        return builder.ToString();
    }

    private static bool IsShortAsciiWord(string value)
    {
        if (value.Length > 3)
            return false;

        foreach (char character in value)
        {
            bool isAsciiLetter = character is >= 'a' and <= 'z';
            bool isAsciiDigit = character is >= '0' and <= '9';
            if (!isAsciiLetter && !isAsciiDigit)
                return false;
        }

        return true;
    }

    private static bool IsHangulCharacter(char character)
    {
        return character is >= '\u1100' and <= '\u11FF'
            or >= '\u3130' and <= '\u318F'
            or >= '\uA960' and <= '\uA97F'
            or >= '\uAC00' and <= '\uD7A3'
            or >= '\uD7B0' and <= '\uD7FF';
    }
}
