using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Felsan.Scripts.Shared.Utilities
{
    [CreateAssetMenu(fileName = "HandlerManifest", menuName = "Felsan/Handler Manifest")]
    public class HandlerManifest : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string primaryKey;
            public string assemblyQualifiedType;
            public AssetReferenceGameObject prefab;
        }

        [SerializeField] public List<Entry> entries = new();

        public bool TryGetEntry(string primaryKey, out Entry entry)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i].primaryKey == primaryKey)
                {
                    entry = entries[i];
                    return true;
                }
            }

            entry = default;
            return false;
        }
    }
}
