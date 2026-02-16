#if UNITY_EDITOR
using System;
using System.Linq;
using Felsan.Scripts.Shared.Handlers;
using Felsan.Scripts.Shared.Utilities;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Editor
{
    public static class HandlerManifestBuilder
    {
        private const string ManifestAssetPath = "Assets/Felsan/ScriptableObjects/HandlerManifest.asset";

        [MenuItem("Tools/Felsan/Build Handler Manifest")]
        public static void Build()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("No AddressableAssetSettings found; cannot build HandlerManifest.");
                return;
            }

            var manifest = AssetDatabase.LoadAssetAtPath<HandlerManifest>(ManifestAssetPath);
            if (manifest == null)
            {
                manifest = ScriptableObject.CreateInstance<HandlerManifest>();
                AssetDatabase.CreateAsset(manifest, ManifestAssetPath);
            }

            manifest.entries.Clear();

            foreach (var group in settings.groups.Where(g => g != null))
            {
                foreach (var entry in group.entries)
                {
                    var path = entry.AssetPath;
                    if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var prefabRoot = PrefabUtility.LoadPrefabContents(path);
                    if (prefabRoot == null) continue;

                    try
                    {
                        var handler = prefabRoot.GetComponent<ABaseHandler>();
                        if (handler == null) continue;

                        manifest.entries.Add(new HandlerManifest.Entry
                        {
                            primaryKey = entry.address,
                            assemblyQualifiedType = handler.GetType().AssemblyQualifiedName,
                            prefab = new AssetReferenceGameObject(entry.guid)
                        });
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(prefabRoot);
                    }
                }
            }

            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"HandlerManifest built with {manifest.entries.Count} entries at {ManifestAssetPath}");
        }
    }
}
#endif
