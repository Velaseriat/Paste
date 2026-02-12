using System;
using UnityEngine;

namespace Felsan.Scripts.Shared.Events
{
    /// <summary>
    /// Base class for ScriptableObject event channels with generic type support
    /// </summary>
    public abstract class EventChannel : ScriptableObject
    {
        [SerializeField] private string channelName;
        [SerializeField] private bool enableLogging = true;
        
        public string ChannelName => channelName;
        public bool EnableLogging => enableLogging;
        
        protected virtual void OnEnable()
        {
            if (string.IsNullOrEmpty(channelName))
            {
                channelName = name;
            }
        }
        
        protected void LogEvent(string eventType, string details = "")
        {
            if (!enableLogging) return;
            var message = $"[{channelName}] {eventType}";
            if (!string.IsNullOrEmpty(details))
            {
                message += $" - {details}";
            }
            Debug.Log(message);
        }
    }
    
    /// <summary>
    /// Generic event channel for type-safe event communication
    /// </summary>
    /// <typeparam name="T">The type of data to be passed with the event</typeparam>
    public abstract class EventChannel<T> : EventChannel
    {
        /// <summary>
        /// The event that gets raised when data is available
        /// </summary>
        public event Action<T> OnEventRaised;
        
        /// <summary>
        /// Raise the event with the specified data
        /// </summary>
        /// <param name="data">The data to pass with the event</param>
        public virtual void RaiseEvent(T data)
        {
            // LogEvent("EventRaised", GetEventDetails(data));
            OnEventRaised?.Invoke(data);
        }
        
        /// <summary>
        /// Subscribe to the event
        /// </summary>
        /// <param name="callback">The callback to invoke when the event is raised</param>
        public void Subscribe(Action<T> callback)
        {
            OnEventRaised -= callback;
            OnEventRaised += callback;
        }
        
        /// <summary>
        /// Unsubscribe from the event
        /// </summary>
        /// <param name="callback">The callback to remove</param>
        public void Unsubscribe(Action<T> callback)
        {
            OnEventRaised -= callback;
        }
        
        /// <summary>
        /// Get details about the event data for logging
        /// </summary>
        /// <param name="data">The event data</param>
        /// <returns>String representation of the event details</returns>
        protected virtual string GetEventDetails(T data)
        {
            return data?.ToString() ?? "null";
        }
    }
    
    /// <summary>
    /// Event channel for events without data (void events)
    /// </summary>
    public abstract class VoidEventChannel : EventChannel
    {
        /// <summary>
        /// The event that gets raised
        /// </summary>
        public event Action OnEventRaised;
        
        /// <summary>
        /// Raise the event
        /// </summary>
        public virtual void RaiseEvent()
        {
            // LogEvent("EventRaised");
            OnEventRaised?.Invoke();
        }
        
        /// <summary>
        /// Subscribe to the event
        /// </summary>
        /// <param name="callback">The callback to invoke when the event is raised</param>
        public void Subscribe(Action callback)
        {
            OnEventRaised += callback;
        }
        
        /// <summary>
        /// Unsubscribe from the event
        /// </summary>
        /// <param name="callback">The callback to remove</param>
        public void Unsubscribe(Action callback)
        {
            OnEventRaised -= callback;
        }
    }
} 