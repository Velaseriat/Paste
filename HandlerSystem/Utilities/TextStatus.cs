using System.Collections.Generic;
using UnityEngine;

namespace Felsan.Utility
{
    public enum TextStatus
    {
        Success,
        Info,
        Debug,
        Warning,
        Failure,
        
        Normal,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic,
        Transcendent
    }

    public static class TextStatusHelper
    {
        private static readonly Dictionary<TextStatus, Color> StatusColors = new()
        {
            { TextStatus.Success, Color.green },
            { TextStatus.Info, Color.cyan },
            { TextStatus.Debug, Color.grey },
            { TextStatus.Warning, Color.yellow },
            { TextStatus.Failure, Color.red },
            
            { TextStatus.Normal, new Color(0.8f, 0.8f, 0.8f) }, // Light Gray (Common)
            { TextStatus.Uncommon, new Color(0.2f, 0.8f, 0.2f) }, // Green (Uncommon)
            { TextStatus.Rare, new Color(0.0f, 0.5f, 1.0f) }, // Blue (Rare)
            { TextStatus.Epic, new Color(0.6f, 0.1f, 0.8f) }, // Purple (Epic)
            { TextStatus.Legendary, new Color(1.0f, 0.75f, 0.0f) }, // Orange/Gold (Legendary)
            { TextStatus.Mythic, new Color(1.0f, 0.0f, 0.0f) }, // Bright Red (Mythic)
            { TextStatus.Transcendent, new Color(1.0f, 0.6f, 1.0f) } // Pinkish Purple (Transcendent)
        };

        public static Color GetColor(this TextStatus statusCode)
        {
            return StatusColors.TryGetValue(statusCode, out var color) ? color : Color.white;
        }
    }
}