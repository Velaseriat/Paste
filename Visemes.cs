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
        public int FrameIndex;  // Time index of the frame (at 60 FPS)
        public int VisemeId;    // Azure Viseme ID
        public float BlendWeight; // Strength of viseme (0-1), smoothed
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
    /// Converts IPA phonemes to Azure visemes with smoothing.
    /// </summary>
    public static List<VisemeFrame> ConvertToVisemes(string ipaString, float audioDuration)
    {
        List<VisemeFrame> visemeFrames = new List<VisemeFrame>();
        List<string> phonemes = TokenizeIPA(ipaString);

        int totalFrames = (int)(audioDuration * 60); // Convert audio duration to frame count (60 FPS)
        int frameStep = totalFrames / Math.Max(phonemes.Count, 1); // Distribute phonemes over the frames

        int frameIndex = 0;
        int previousViseme = -1;
        float previousWeight = 0f;

        foreach (var phoneme in phonemes)
        {
            if (ipaToAzureViseme.TryGetValue(phoneme, out int visemeId))
            {
                // Apply smoothing: If the previous viseme is different, add transition frames
                if (previousViseme != -1 && previousViseme != visemeId)
                {
                    int transitionFrames = frameStep / 2; // Half of step for smooth transition
                    for (int t = 1; t <= transitionFrames; t++)
                    {
                        float weight = (float)t / transitionFrames; // Gradually increase weight
                        visemeFrames.Add(new VisemeFrame
                        {
                            FrameIndex = frameIndex - transitionFrames + t,
                            VisemeId = previousViseme,
                            BlendWeight = (1 - weight) * previousWeight
                        });

                        visemeFrames.Add(new VisemeFrame
                        {
                            FrameIndex = frameIndex - transitionFrames + t,
                            VisemeId = visemeId,
                            BlendWeight = weight
                        });
                    }
                }

                // Main viseme frame
                visemeFrames.Add(new VisemeFrame
                {
                    FrameIndex = frameIndex,
                    VisemeId = visemeId,
                    BlendWeight = 1.0f
                });

                // Store previous values for smoothing
                previousViseme = visemeId;
                previousWeight = 1.0f;
                
                frameIndex += frameStep; // Move to next phoneme frame
            }
            else
            {
                Console.WriteLine($"[WARNING] Unrecognized IPA phoneme: '{phoneme}'");
            }
        }

        return visemeFrames;
    }
}
