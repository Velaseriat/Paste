using System;
using System.Collections.Generic;

public class IPAtoAzureVisemeConverter
{
    // List of multi-character IPA phonemes
    private static readonly List<string> MultiCharPhonemes = new List<string>
    {
        "aɪ", "aʊ", "ɔɪ", "eɪ", "oʊ", "tʃ", "dʒ", "ʃ", "ʒ", "θ", "ð", "ŋ"
    };

    // Map IPA phonemes to Azure viseme IDs (22 IDs based on Microsoft's documentation)
    private static readonly Dictionary<string, int> ipaToAzureViseme = new Dictionary<string, int>
    {
        {"p", 0}, {"b", 1}, {"m", 2}, {"f", 3}, {"v", 4}, {"θ", 5}, {"ð", 6},
        {"t", 7}, {"d", 8}, {"s", 9}, {"z", 10}, {"ʃ", 11}, {"ʒ", 12},
        {"k", 13}, {"g", 14}, {"ŋ", 15}, {"h", 16}, {"ɹ", 17}, {"l", 18},
        {"j", 19}, {"w", 20}, {"i", 21}, {"ɪ", 21}, {"e", 22}, {"æ", 23},
        {"ɑ", 24}, {"ɔ", 25}, {"o", 26}, {"ʊ", 27}, {"u", 28}, {"ʌ", 29},
        {"ə", 30}, {"aɪ", 31}, {"aʊ", 32}, {"ɔɪ", 33}, {"eɪ", 34}, {"oʊ", 35}
    };

    public class VisemeFrame
    {
        public int FrameIndex;  // Time index of the frame
        public int VisemeId;    // Azure Viseme ID
        public float BlendWeight; // Strength (0-1)
    }

    /// <summary>
    /// Tokenizes an IPA string into phonemes.
    /// </summary>
    public static List<string> TokenizeIPA(string ipaString)
    {
        List<string> tokens = new List<string>();
        int i = 0;

        while (i < ipaString.Length)
        {
            string currentChar = ipaString[i].ToString();
            string nextChar = (i + 1 < ipaString.Length) ? ipaString[i + 1].ToString() : "";

            if (!string.IsNullOrEmpty(nextChar) && MultiCharPhonemes.Contains(currentChar + nextChar))
            {
                tokens.Add(currentChar + nextChar);
                i += 2;
            }
            else
            {
                tokens.Add(currentChar);
                i++;
            }
        }

        return tokens;
    }

    /// <summary>
    /// Converts a tokenized IPA phoneme list into Azure viseme frames.
    /// </summary>
public static List<VisemeFrame> ConvertToVisemes(string ipaString, float audioDuration, int totalFrames)
{
    List<VisemeFrame> visemeFrames = new List<VisemeFrame>();
    List<string> phonemes = TokenizeIPA(ipaString);

    // Calculate dynamic frame spacing
    int frameStep = totalFrames / phonemes.Count;  // Distribute phonemes evenly

    int frameIndex = 0;

    foreach (var phoneme in phonemes)
    {
        if (ipaToAzureViseme.TryGetValue(phoneme, out int visemeId))
        {
            visemeFrames.Add(new VisemeFrame
            {
                FrameIndex = frameIndex,
                VisemeId = visemeId,
                BlendWeight = 1.0f
            });

            frameIndex += frameStep;  // Adjust dynamically based on total frames
        }
        else
        {
            Console.WriteLine($"[WARNING] Unrecognized IPA phoneme: '{phoneme}'");
        }
    }

    return visemeFrames;
}

    public static void Main()
    {
        string ipaInput = "pætɪk"; // Example IPA input (for "patic")
        List<VisemeFrame> result = ConvertToVisemes(ipaInput);

        Console.WriteLine("Generated Viseme Frames:");
        foreach (var frame in result)
        {
            Console.WriteLine($"Frame: {frame.FrameIndex}, Viseme: {frame.VisemeId}, Weight: {frame.BlendWeight}");
        }
    }
}
