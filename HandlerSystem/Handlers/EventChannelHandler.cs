using System;
using System.Collections.Generic;
using Felsan.Scripts.Shared.Events;
using UnityEngine;

namespace Felsan.Scripts.Shared.Handlers
{
    public class EventChannelHandler : ABaseHandler
    {
        [SerializeField] private List<EventChannel> eventChannels;
        private readonly Dictionary<Type, EventChannel> _eventChannels = new();
        
        public override void Initialize(HandlerManager handlerManager)
        {
            // If already initialized due to dependency on another Handler, exit
            if (handlerState == HandlerState.Initialized) return;

            // Enable to initializing
            handlerState = HandlerState.Initializing;

            eventChannels = handlerManager.sceneOverrides.eventChannels;
            PopulateEventChannels();

            // Call base class to finish the initializing process
            base.Initialize(handlerManager);
        }
        
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void PopulateEventChannels()
        {
            foreach (var eventChannel in eventChannels)
            {
                _eventChannels.Add(eventChannel.GetType(), eventChannel);
            }
        }

        public T GetEventChannel<T>() where T : EventChannel
        {
            _eventChannels.TryGetValue(typeof(T), out var eventChannel);
            if (eventChannel == null)
                LogWarning("No event channel found for type " + typeof(T));
            else
                return eventChannel as T;
            return null;
        }
    }
}