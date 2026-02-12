using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Felsan.Scripts.Shared.Handlers;
using Felsan.Scripts.Shared.Utilities;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;

namespace Felsan.Scripts.Shared
{
    /// <summary>
    /// Core manager class that handles the lifecycle of all game handlers using Unity's Addressable system.
    /// 
    /// This class implements a sophisticated dependency management system that:
    /// - Automatically loads handlers based on scene metadata
    /// - Manages handler initialization order and dependencies
    /// - Provides asset caching and memory management
    /// - Handles scene transitions and cleanup
    /// 
    /// DESIGN PATTERNS:
    /// - Singleton Pattern: Single instance manages all handlers
    /// - Factory Pattern: Creates and manages handler instances
    /// - Observer Pattern: Events for initialization progress
    /// - Dependency Injection: Handlers receive manager reference
    /// 
    /// USAGE:
    /// 1. Attach to a GameObject in your bootstrap scene
    /// 2. Configure addressable labels for different handler groups
    /// 3. Handlers are automatically loaded when scenes change
    /// 4. Access handlers via GetHandler<T>() method
    /// </summary>
    public class HandlerManager : MonoBehaviour
    {
        #region Singleton Pattern
        
        /// <summary>
        /// Singleton instance of the HandlerManager.
        /// Ensures only one manager exists across the entire game.
        /// </summary>
        public static HandlerManager Instance { get; private set; }
        
        #endregion

        #region Private Fields
        
        /// <summary>
        /// Dictionary mapping handler types to their instances.
        /// Provides O(1) lookup time for handler retrieval.
        /// </summary>
        private Dictionary<Type, ABaseHandler> _handlers = new();
        private Dictionary<Type, ABaseHandler> _prevHandlers = new();

        #endregion

        #region Configuration
        
        [Header("Asset Loading Settings")] [SerializeField] private bool useAssetCache = true;
        
        /// <summary>
        /// Whether to keep handler assets pinned in memory.
        /// Prevents assets from being unloaded during scene transitions.
        /// </summary>
        [SerializeField] private bool pinHandlerAssets = true;
        
        [SerializeField] public SceneOverrides sceneOverrides;
        [Header("Manifest")]
        [SerializeField] private HandlerManifest handlerManifest;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Unity Awake method - called before Start.
        /// Sets up the singleton pattern and begins initial handler loading.
        /// </summary>
        private void Awake()
        {
            // Singleton pattern implementation
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Load the "boot" handler group immediately
            // This group contains essential handlers needed for game startup
            _ = LoadPrefabsByGroupsAsync(sceneOverrides.startingScene.addressableLabels);
            
            // Keep this manager alive across scene changes
            // Handlers need to persist during scene transitions
            DontDestroyOnLoad(gameObject);
        }
        #endregion

        #region Scene Management

        /// <summary>
        /// Called whenever a new scene is loaded.
        /// Automatically loads handlers specific to the new scene.
        /// 
        /// This is the core of the dynamic handler loading system.
        /// </summary>
        /// <param name="scene">The scene that was loaded</param>
        /// <param name="mode">How the scene was loaded (Single/Additive)</param>
        /// <param name="groupTags"></param>
        public async Task OnSceneLoaded(Scene scene, LoadSceneMode mode, SceneMetadata groupTags)
        {
            try
            {
                Debug.Log($"Scene Loaded: {scene.name} with mode: {mode}");
                
                // Get scene-specific metadata from SceneHandler
                // This tells us which handler groups to load for this scene
                Debug.Log($"Scene Metadata for {scene.name}:");
                Debug.Log($"- Scene Name: {groupTags.sceneName}");
                Debug.Log($"- Addressable Labels: {string.Join(", ", groupTags.addressableLabels)}");

                _prevHandlers = _handlers;
                _handlers = new Dictionary<Type, ABaseHandler>();
                
                // Load handlers based on scene metadata
                await LoadPrefabsByGroupsAsync(groupTags.addressableLabels);
                
                _handlers.Values.ForEach(x => x.PostInitialize(this));
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to load prefabs for {scene.name}: {ex.Message}");
            }
        }

        #endregion

        #region Handler Loading Pipeline

        /// <summary>
        /// Main entry point for loading handlers by group labels.
        /// Called after scene loading to set up scene-specific systems.
        /// </summary>
        /// <param name="groupLabels">Array of addressable labels to load handlers from</param>
        private async Task LoadPrefabsByGroupsAsync(string[] groupLabels)
        {
            // Load all handler prefabs first, then initialize them
            await LoadHandlerPrefabsAsync(groupLabels);

            ClearUnusedHandlers();

            Debug.Log("Handlers Initialized.");
            return;

            void ClearUnusedHandlers()
            {
                foreach (var handlerKvp in
                         _prevHandlers.Where(handlerKvp => handlerKvp.Value))
                {
                    Destroy(handlerKvp.Value.gameObject);
                }

                _prevHandlers.Clear();
            }
        }

        /// <summary>
        /// Loads handler prefabs and initializes them.
        ///
        ///
        /// 
        /// 
        /// This method demonstrates advanced async/await patterns:
        /// - Parallel loading of multiple handler groups
        /// - Sequential initialization to respect dependencies
        /// - Progress reporting during the process
        /// </summary>
        /// <param name="labelsToLoad">Addressable labels to load handlers from</param>
        private async Task LoadHandlerPrefabsAsync(string[] labelsToLoad)
        {
            if (labelsToLoad.Length == 0)
            {
                Debug.LogWarning("No addressable labels provided for handler load; skipping.");
                return;
            }
            // Load all handler groups in parallel for maximum performance
            // This is a key optimization - we don't wait for each group sequentially
            var tasks = labelsToLoad.Select(LoadHandlerGroupAsync).ToArray();
            
            // Wait for all prefabs to load before proceeding
            await Task.WhenAll(tasks);
            
            foreach (var handler in _handlers.Values)
            {
                handler.Initialize(this);
            }
        }

        /// <summary>
        /// Loads all handlers for a specific addressable label.
        /// 
        /// This method demonstrates Unity Addressables best practices:
        /// - Proper resource location loading
        /// - Parallel asset loading
        /// - Correct resource cleanup
        /// </summary>
        /// <param name="label">Addressable label to load handlers from</param>
        private async Task LoadHandlerGroupAsync(string label)
        {
            Debug.Log($"Loading handlers for label: {label}");

            var handle = Addressables.LoadResourceLocationsAsync(label, typeof(GameObject));
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"Failed to load resource locations for label {label}");
                return;
            }

            var locations = handle.Result;
            Debug.Log($"Found {locations.Count} handlers for label {label}:");
            foreach (var location in locations)
            {
                Debug.Log($"- {location.PrimaryKey}");
            }

            // Load all prefabs for this label in parallel
            var loadTasks = locations.Select(LoadHandlerPrefabAsync).ToArray();
            await Task.WhenAll(loadTasks);

            // Always release the resource locations handle
            // This prevents memory leaks
            Addressables.Release(handle);
        }

        /// <summary>
        /// Loads a single handler prefab and creates an instance.
        /// 
        /// This method demonstrates advanced asset management:
        /// - Asset caching for performance
        /// - Proper instantiation and tracking
        /// - Error handling and validation
        /// </summary>
        /// <param name="location">Resource location of the handler prefab</param>
        private async Task LoadHandlerPrefabAsync(IResourceLocation location)
        {
            var handlerType = ResolveHandlerType(location.PrimaryKey);
            if (handlerType != null && _prevHandlers.TryGetValue(handlerType, out var prevHandler))
            {
                _handlers.Add(handlerType, prevHandler);
                _prevHandlers[handlerType] = null;
                return;
            }

            GameObject prefab;
            
            // Use asset caching if available for better performance
            if (useAssetCache && AssetCacheHandler.Instance != null)
            {
                // Cached loading reduces memory allocation and improves performance
                prefab = await AssetCacheHandler.Instance.LoadAssetAsync(location.PrimaryKey, pinHandlerAssets);
            }
            else
            {
                // Fallback to direct Addressables loading
                var prefabHandle = Addressables.LoadAssetAsync<GameObject>(location);
                await prefabHandle.Task;

                if (prefabHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"Failed to load prefab at {location.PrimaryKey}");
                    return;
                }

                prefab = prefabHandle.Result;
                Addressables.Release(prefabHandle);
            }

            if (prefab == null)
            {
                Debug.LogError($"Failed to load prefab at {location.PrimaryKey}");
                return;
            }
            
            // Create an instance of the handler prefab
            var instance = Instantiate(prefab);

            // Extract the handler component from the GameObject
            var handler = instance.GetComponent<ABaseHandler>();
            
            if (handler == null)
            {
                Debug.LogError($"Invalid prefab at {location.PrimaryKey}");
                return;
            }
            
            // Pre-initialize the handler (sets up internal state)
            handler.PreInitialize();
            Debug.Log($"Handler {location.PrimaryKey} pre-initialized.");

            // Store in our handler dictionary for later access
            if (handlerType != null) _handlers[handlerType] = handler;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets a handler of the specified type, initializing it if necessary.
        /// 
        /// This is the main public API for accessing handlers.
        /// It handles lazy initialization and dependency management.
        /// 
        /// DESIGN PATTERN: Lazy Initialization with Dependency Management
        /// </summary>
        /// <typeparam name="T">Type of handler to retrieve</typeparam>
        /// <returns>Initialized handler instance</returns>
        /// <exception cref="HandlerException">Thrown if handler not found or cyclic dependency detected</exception>
        public T GetHandler<T>() where T : ABaseHandler
        {
            if (!_handlers.TryGetValue(typeof(T), out var baseHandler))
                throw new HandlerException($"Cannot find handler of type {typeof(T)}");

            switch (baseHandler.handlerState)
            {
                case HandlerState.Initialized:
                    return baseHandler as T; // Safe cast after validation

                case HandlerState.Initializing:
                    // Detect circular dependencies during initialization
                    throw new HandlerException($"Cyclic dependency detected for {typeof(T)}");

                case HandlerState.Uninitialized:
                default:
                    // Initialize the handler if it hasn't been initialized yet
                    _handlers[typeof(T)].Initialize(this);
                    return (T)_handlers[typeof(T)]; // Safe cast after initialization
            }
        }

        /// <summary>
        /// Explicitly initializes a handler without returning it.
        /// 
        /// Use this when you need to ensure a handler is initialized
        /// but don't need to access it immediately.
        /// 
        /// DESIGN PATTERN: Explicit Initialization
        /// </summary>
        /// <typeparam name="T">Type of handler to initialize</typeparam>
        /// <exception cref="HandlerException">Thrown if handler not found or cyclic dependency detected</exception>
        public void InitHandler<T>() where T : ABaseHandler
        {
            if (!_handlers.TryGetValue(typeof(T), out var baseHandler))
                throw new HandlerException($"Cannot find handler of type {typeof(T)}");

            switch (baseHandler.handlerState)
            {
                case HandlerState.Initialized:
                    return; // Already initialized
                case HandlerState.Initializing:
                    throw new HandlerException($"Cyclic dependency detected for {typeof(T)}");
                case HandlerState.Uninitialized:
                default:
                    _handlers[typeof(T)].Initialize(this);
                    break;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets cache statistics if AssetCache is being used.
        /// 
        /// This provides insight into memory usage and caching efficiency.
        /// Useful for debugging and performance monitoring.
        /// </summary>
        /// <returns>Tuple with cache statistics, or null if caching not enabled</returns>
        public (int cachedCount, int pinnedCount, float memoryUsageMB)? GetCacheStats()
        {
            return useAssetCache && AssetCacheHandler.Instance != null 
                ? AssetCacheHandler.Instance.GetCacheStats() 
                : null;
        }

        private Type ResolveHandlerType(string primaryKey)
        {
            if (handlerManifest == null) return null;

            if (handlerManifest.TryGetEntry(primaryKey, out var entry))
            {
                var handlerType = Type.GetType(entry.assemblyQualifiedType);
                if (handlerType == null)
                {
                    Debug.LogError($"HandlerManifest entry for {primaryKey} has invalid type {entry.assemblyQualifiedType}");
                }

                return handlerType;
            }

            Debug.LogWarning($"HandlerManifest missing entry for primary key {primaryKey}");
            return null;
        }

        #endregion
        
        public void AddHandler(Type handlerType, ABaseHandler handler)
        {
            _handlers.TryAdd(handlerType, handler);
        }
        
        public void RemoveHandler(Type handlerType)
        {
            _handlers.Remove(handlerType);
        }
    }

    /// <summary>
    /// Custom exception class for handler-related errors.
    /// 
    /// Provides detailed error information and automatic logging
    /// for debugging handler initialization issues.
    /// </summary>
    public class HandlerException : Exception
    {
        /// <summary>
        /// Creates a new HandlerException with automatic error logging.
        /// </summary>
        /// <param name="message">Error message describing the issue</param>
        public HandlerException(string message) : base(message) => Debug.LogError(message);
    }
}
