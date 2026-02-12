using UnityEngine;

namespace Felsan.Scripts.Shared
{
    [CreateAssetMenu(fileName = "SceneMetadata", menuName = "Felsan/Scene Metadata")]

    public class SceneMetadata : ScriptableObject
    {
        public string sceneName;
        public string description;
        public string[] addressableLabels;
        // Add other metadata fields here
    }
}