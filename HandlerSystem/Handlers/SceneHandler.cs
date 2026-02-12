
using System;
using AYellowpaper.SerializedCollections;
using Felsan.Scripts.Shared.Events;
using Felsan.Scripts.Shared.Utilities;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Felsan.Scripts.Shared.Handlers
{
    /// <summary>
    /// Manages scene transitions and metadata for the dynamic handler loading system.
    /// 
    /// This handler is responsible for:
    /// - Storing scene-specific metadata (addressable labels, scene names)
    /// - Managing scene transitions with proper cleanup
    /// - Coordinating with HandlerManager for scene-specific handler loading
    /// - Providing scene information to other systems
    /// 
    /// DESIGN PATTERNS:
    /// - Singleton Pattern: Single instance manages all scene data
    /// - Observer Pattern: Notifies when handlers are loaded
    /// - Strategy Pattern: Different scene loading strategies (game vs menu)
    /// - Metadata Pattern: Scene configuration stored as data
    /// 
    /// USAGE:
    /// 1. Configure scene metadata in the inspector
    /// 2. Call GoToScene() or GoToGame() for transitions
    /// 3. SceneHandler automatically coordinates with HandlerManager
    /// 4. Other systems can query current scene metadata
    /// </summary>
    public class SceneHandler : ABaseHandler
    {
        private GoToSceneEventChannel _goToSceneEventChannel;

        #region Scene Information
        
        /// <summary>
        /// The currently active scene name.
        /// Used by other systems to determine which scene is loaded.
        /// </summary>
        public static string CurrentScene { get; private set; }

        #endregion

        #region Scene Configuration
        
        [Header("Scene Configuration")]
        [SerializedDictionary("Scene Title", "Scene Metadata")]
        public SerializedDictionary<string, SceneMetadata> sceneMetadatas;

        public SceneMetadata startingScene;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the SceneHandler and sets up event subscriptions.
        /// 
        /// This method demonstrates the handler initialization pattern:
        /// - Prevents duplicate initialization
        /// - Sets up event subscriptions
        /// - Calls base class initialization
        /// 
        /// DESIGN PATTERN: Template Method Pattern
        /// </summary>
        /// <param name="handlerManager">Reference to the HandlerManager for event subscription</param>
        public override void Initialize(HandlerManager handlerManager)
        {
            // Prevent duplicate initialization (important for dependency management)
            if (handlerState == HandlerState.Initialized) return;
            
            handlerManager.InitHandler<EventChannelHandler>();
            
            _goToSceneEventChannel = handlerManager.GetHandler<EventChannelHandler>().GetEventChannel<GoToSceneEventChannel>();
            _goToSceneEventChannel?.Subscribe(GoToScene);

            sceneMetadatas = handlerManager.sceneOverrides.sceneMetadataDict;
            startingScene = handlerManager.sceneOverrides.startingScene;
            
            CurrentScene = startingScene.sceneName;
            
            // Mark as initializing to prevent circular dependencies
            handlerState = HandlerState.Initializing;

            base.Initialize(handlerManager);
        }

        #endregion

        #region Unity Lifecycle

        
        /// <summary>
        /// Unity Awake method - called before Start.
        /// Begins asynchronous loading of the login UI prefab.
        /// </summary>
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            _goToSceneEventChannel?.Subscribe(GoToScene);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var tags = GetCurrentSceneMetadata();
            _ = HandlerManager.Instance.OnSceneLoaded(scene, mode, tags);
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _goToSceneEventChannel?.Unsubscribe(GoToScene);
        }

        public void GoToScene(ScenePayload scenePayload)
        {
            if (scenePayload.IsGameScene)
                GoToGame(scenePayload.SceneName, scenePayload.OriginalScene);
            else
                GoToScene(scenePayload.SceneName, scenePayload.OriginalScene);
        }

        #endregion
        

        #region Public API

        /// <summary>
        /// Gets metadata for the currently active scene.
        /// 
        /// This method provides access to scene-specific configuration:
        /// - Addressable labels for handler loading
        /// - Scene-specific settings
        /// - Handler group configurations
        /// 
        /// USAGE: Other systems can query this to determine what
        /// handlers or assets should be loaded for the current scene.
        /// </summary>
        /// <returns>SceneMetadata containing configuration for the current scene</returns>
        public SceneMetadata GetCurrentSceneMetadata()
        {
            return sceneMetadatas[CurrentScene];
        }

        #endregion

        #region Scene Loading

        /// <summary>
        /// Internal method for loading scenes with proper setup.
        /// 
        /// This method handles the complete scene loading process:
        /// - Updates current scene tracking
        /// - Stores post-load actions
        /// - Triggers Unity scene loading
        /// - Coordinates with HandlerManager for handler loading
        /// 
        /// DESIGN PATTERN: Template Method Pattern
        /// </summary>
        /// <param name="sceneName">Name of the scene to load</param>
        /// <param name="isOriginalScene">Whether to load the original scene or a basic version</param>
        /// <param name="basicSceneName">Fallback scene name for basic loading</param>
        public void LoadScene(string sceneName, bool isOriginalScene, string basicSceneName)
        {
            // Update current scene tracking
            CurrentScene = sceneName;
            
            // Load the scene using Unity's SceneManager
            // The scene name depends on whether we want the original or basic version
            SceneManager.LoadScene(isOriginalScene ? sceneName : basicSceneName);
        }

        /// <summary>
        /// Loads a scene with basic functionality.
        /// 
        /// This method is used for menu and UI scenes that don't need
        /// the full game functionality. It loads a simplified scene
        /// that focuses on the core UI elements.
        /// 
        /// USAGE: Call this for menu transitions, settings screens,
        /// or any non-gameplay scene.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load</param>
        /// <param name="isOriginalScene">Whether to load the original scene</param>
        private void GoToScene(string sceneName, bool isOriginalScene)
            => LoadScene(sceneName, isOriginalScene, SceneNames.BasicScene);

        /// <summary>
        /// Loads a game scene with full functionality.
        /// 
        /// This method is used for gameplay scenes that need the complete
        /// game systems. It loads the full scene with all game functionality.
        /// 
        /// USAGE: Call this for transitioning to gameplay, levels,
        /// or any scene that needs full game systems.
        /// </summary>
        /// <param name="sceneName">Name of the game scene to load</param>
        /// <param name="isOriginalScene">Whether to load the original scene</param>
        private void GoToGame(string sceneName, bool isOriginalScene)
            => LoadScene(sceneName, isOriginalScene, SceneNames.BasicGameScene);

        #endregion
    }
}