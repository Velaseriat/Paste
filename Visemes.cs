using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class IPAParser
{
    // List of multi-character IPA phonemes (e.g., "aɪ", "oʊ", etc.)
    private static readonly List<string> MultiCharPhonemes = new List<string>
    {
        "aɪ", "aʊ", "ɔɪ", "eɪ", "oʊ", "tʃ", "dʒ", "ʃ", "ʒ", "θ", "ð", "ŋ"
    };

    /// <summary>
    /// Splits a single IPA string into phoneme tokens.
    /// </summary>
    public static List<string> TokenizeIPA(string ipaString)
    {
        List<string> tokens = new List<string>();
        int i = 0;

        while (i < ipaString.Length)
        {
            string currentChar = ipaString[i].ToString();
            string nextChar = (i + 1 < ipaString.Length) ? ipaString[i + 1].ToString() : "";

            // Check for multi-character phoneme
            if (!string.IsNullOrEmpty(nextChar) && MultiCharPhonemes.Contains(currentChar + nextChar))
            {
                tokens.Add(currentChar + nextChar);
                i += 2; // Move past both characters
            }
            else
            {
                tokens.Add(currentChar);
                i++; // Move to the next character
            }
        }

        return tokens;
    }


public class IPAtoAzureVisemeConverter
{
    // Map IPA phonemes to Azure viseme indices (example mapping)
    private static readonly Dictionary<string, int> ipaToAzureViseme = new Dictionary<string, int>
    {
        {"p", 0},  // Example: "p" maps to viseme 0
        {"b", 1},  // "b" maps to viseme 1
        {"m", 2},  // "m" maps to viseme 2
        {"f", 3},  // "f" maps to viseme 3
        {"v", 4},  // "v" maps to viseme 4
        {"θ", 5},  // "th" unvoiced (θ)
        {"ð", 6},  // "th" voiced (ð)
        {"t", 7},  // "t"
        {"d", 8},  // "d"
        {"s", 9},  // "s"
        {"z", 10}, // "z"
        {"ʃ", 11}, // "sh"
        {"ʒ", 12}, // "zh"
        {"k", 13}, // "k"
        {"g", 14}, // "g"
        {"ŋ", 15}, // "ng"
        {"h", 16}, // "h"
        {"ɹ", 17}, // "r"
        {"l", 18}, // "l"
        {"j", 19}, // "y"
        {"w", 20}, // "w"
        {"i", 21}, // "ee"
        {"ɪ", 22}, // "ih"
        {"e", 23}, // "eh"
        {"æ", 24}, // "aa"
        {"ɑ", 25}, // "ah"
        {"ɔ", 26}, // "aw"
        {"o", 27}, // "oh"
        {"ʊ", 28}, // "uh"
        {"u", 29}, // "oo"
        {"ʌ", 30}, // "uh" (stressed)
        {"ə", 31}, // "schwa"
        {"aɪ", 32}, // "eye"
        {"aʊ", 33}, // "ow"
        {"ɔɪ", 34}, // "oy"
        {"eɪ", 35}, // "ay"
        {"oʊ", 36}  // "oh"
    };

    public class VisemeFrame
    {
        public int FrameIndex;  // The time index of the frame
        public int VisemeIndex; // The Azure viseme ID
        public float BlendWeight; // Strength of the viseme (0-1)
    }

    /// <summary>
    /// Converts IPA phonemes into Azure blendshape visemes.
    /// </summary>
    /// <param name="ipaPhonemes">List of phonemes with timing.</param>
    /// <returns>List of viseme animation frames.</returns>
    public static List<VisemeFrame> ConvertPhonemesToVisemes(List<Tuple<string, int>> ipaPhonemes)
    {
        List<VisemeFrame> visemeFrames = new List<VisemeFrame>();

        foreach (var phonemeData in ipaPhonemes)
        {
            string ipa = phonemeData.Item1;
            int frameIndex = phonemeData.Item2;

            if (ipaToAzureViseme.TryGetValue(ipa, out int visemeIndex))
            {
                visemeFrames.Add(new VisemeFrame
                {
                    FrameIndex = frameIndex,
                    VisemeIndex = visemeIndex,
                    BlendWeight = 1.0f  // Assume full intensity
                });
            }
            else
            {
                Console.WriteLine($"Warning: No mapping for IPA phoneme '{ipa}'");
            }
        }

        return visemeFrames;
    }
}

  
}
