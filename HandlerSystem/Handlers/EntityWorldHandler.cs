using System;
using Felsan.Scripts.Shared.Events;
using Felsan.Scripts.Shared.Utilities;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.NetCode;
using Unity.Scenes;
using UnityEngine;

namespace Felsan.Scripts.Shared.Handlers
{
    public class EntityWorldHandler : ABaseHandler
    {
        public GameConnectionMode GameWorldMode { get; set; }
        
        private OnSetWorldTypeEventChannel _onSetWorldTypeEventChannel;
        private OnLocalWorldCreatedEventChannel _onLocalWorldCreatedEventChannel;
        private OnClientWorldCreatedEventChannel _onClientWorldCreatedEventChannel;
        private OnServerWorldCreatedEventChannel _onServerWorldCreatedEventChannel;
        
        [Header("ECS Subscene")]
        [SerializeField] private EntitySceneReference entitiesScene;

        private World _localWorld;
        private World _clientWorld;
        private World _serverWorld;

        private const string LocalWorldName = "LocalWorld";
        private const string ClientWorldName = "ClientWorld";
        private const string ServerWorldName = "ServerWorld";


        public override void Initialize(HandlerManager handlerManager)
        {
            // If already initialized due to dependency on another Handler, exit
            if (handlerState == HandlerState.Initialized) return;

            // Enable to initializing
            handlerState = HandlerState.Initializing;
            
            // Initialize EventChannelHandler dependency
            handlerManager.InitHandler<EventChannelHandler>();

            _onSetWorldTypeEventChannel = handlerManager.GetHandler<EventChannelHandler>().GetEventChannel<OnSetWorldTypeEventChannel>();
            _onSetWorldTypeEventChannel.Subscribe(SetWorld);
            _onLocalWorldCreatedEventChannel = handlerManager.GetHandler<EventChannelHandler>().GetEventChannel<OnLocalWorldCreatedEventChannel>();
            _onClientWorldCreatedEventChannel = handlerManager.GetHandler<EventChannelHandler>().GetEventChannel<OnClientWorldCreatedEventChannel>();
            _onServerWorldCreatedEventChannel = handlerManager.GetHandler<EventChannelHandler>().GetEventChannel<OnServerWorldCreatedEventChannel>();
            
            GameWorldMode = GameConnectionMode.Local;

            // Call base class to finish the initializing process
            base.Initialize(handlerManager);
        }
        
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void OnEnable()
        {
            _onSetWorldTypeEventChannel?.Subscribe(SetWorld);
        }
        
        public void OnDisable()
        {
            _onSetWorldTypeEventChannel?.Unsubscribe(SetWorld);
        }

        public World GetWorld()
        {
            switch (GameWorldMode)
            {
                case GameConnectionMode.Local:
                    return _localWorld;
                case GameConnectionMode.Client:
                case GameConnectionMode.Hosted:
                    return _clientWorld;
                case GameConnectionMode.Server:
                    return _serverWorld;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SetWorld(GameConnectionMode mode)
        {
            Log($"Switching world type to {mode}.");
            switch (mode)
            {
                case GameConnectionMode.Local:
                    SetLocalMode();
                    break;
                case GameConnectionMode.Client:
                    SetClientMode();
                    break;
                case GameConnectionMode.Hosted:
                    SetHostedMode();
                    break;
                case GameConnectionMode.Server:
                    SetServerMode();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        public void SetLocalMode()
        {
            DestroyAllWorlds();
            CreateLocalWorld();
            GameWorldMode = GameConnectionMode.Local;
            // Load ECS subscene into default world
            LoadEntitiesSceneIntoWorld(_localWorld);
        }

        public void SetClientMode()
        {
            DestroyAllWorlds();
            CreateClientWorld();
            GameWorldMode = GameConnectionMode.Client;
            // Load ECS subscene into client world
            LoadEntitiesSceneIntoWorld(_clientWorld);
        }
        
        public void SetHostedMode()
        {
            DestroyAllWorlds();
            CreateClientWorld();
            CreateServerWorld();
            GameWorldMode = GameConnectionMode.Hosted;
            // Load ECS subscene into both worlds
            LoadEntitiesSceneIntoWorld(_clientWorld);
            LoadEntitiesSceneIntoWorld(_serverWorld);
        }

        public void SetServerMode()
        {
            DestroyAllWorlds();
            CreateServerWorld();
            GameWorldMode = GameConnectionMode.Server;
            // Load ECS subscene into server world
            LoadEntitiesSceneIntoWorld(_serverWorld);
        }

        private void DestroyAllWorlds()
        {
            World.DefaultGameObjectInjectionWorld = null; // Always unset default
            World.DisposeAllWorlds();
            _localWorld = null;
            _clientWorld = null;
            _serverWorld = null;
        }
        
        public void CreateLocalWorld()
        {
            _localWorld = ClientServerBootstrap.CreateLocalWorld(LocalWorldName);
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(_localWorld);
            Log($"[EntityWorldHandler] Created {_localWorld.Name} world.");
        }

        public void CreateClientWorld()
        {
            _clientWorld = ClientServerBootstrap.CreateClientWorld(ClientWorldName);
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(_clientWorld);
            Log($"[EntityWorldHandler] Created {_clientWorld.Name} world.");
        }

        public void CreateServerWorld()
        {
            _serverWorld = ClientServerBootstrap.CreateServerWorld(ServerWorldName);
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(_serverWorld);
            Log($"[EntityWorldHandler] Created {_serverWorld.Name} world.");
        }

        private void LoadEntitiesSceneIntoWorld(World world)
        {
            var sceneEntity = SceneSystem.LoadSceneAsync(world.Unmanaged, entitiesScene);
            SetWorldReady();
            Log($"[EntityWorldHandler] Loading entity scene in {world.Name} world.");
        }

        public void SetWorldReady()
        {
            switch (GameWorldMode)
            {
                case GameConnectionMode.Local:
                    _onLocalWorldCreatedEventChannel.RaiseEvent(_localWorld);
                    break;
                case GameConnectionMode.Client:
                    _onClientWorldCreatedEventChannel.RaiseEvent(_clientWorld);
                    break;
                case GameConnectionMode.Hosted:
                    _onClientWorldCreatedEventChannel.RaiseEvent(_clientWorld);
                    _onServerWorldCreatedEventChannel.RaiseEvent(_serverWorld);
                    break;
                case GameConnectionMode.Server:
                    _onServerWorldCreatedEventChannel.RaiseEvent(_serverWorld);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }       
        }
    }
}