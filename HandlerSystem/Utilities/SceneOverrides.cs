using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using Felsan.Scripts.Shared.Events;
using UnityEngine;

namespace Felsan.Scripts.Shared.Utilities
{
    [CreateAssetMenu(fileName = "SceneOverrides", menuName = "Felsan/Scene Overrides")]
    public class SceneOverrides : ScriptableObject
    {
        [Header("Scene Configuration")]
        [SerializedDictionary("Scene Title", "Scene Metadata")]
        public SerializedDictionary<string, SceneMetadata> sceneMetadataDict;
        public SceneMetadata startingScene;
        
        [Header("Event Channels Configuration")]
        public List<EventChannel> eventChannels;
    }
}