using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Felsan.Scripts.Shared.Handlers
{
    public abstract class ABaseHandler : MonoBehaviour
    {
        public HandlerState handlerState;
        protected HandlerManager HandlerManager;
        
        private static readonly Queue<Action> ExecutionQueue = new();
        
        public virtual void PreInitialize()
        {
            handlerState = HandlerState.Uninitialized;
            HandlerManager.Instance.AddHandler(GetType(), this);
        }
        
        public virtual void Initialize(HandlerManager handlerManager)
        {
            handlerState = HandlerState.Initialized;
            HandlerManager = handlerManager;
        }
        
        public virtual void PostInitialize(HandlerManager handlerManager)
        {
        }
        
        protected virtual void Update()
        {
            lock (ExecutionQueue)
            {
                while (ExecutionQueue.Count > 0)
                {
                    ExecutionQueue.Dequeue()?.Invoke();
                }
            }
        }

        protected static void EnqueueAction(Action action)
        {
            lock (ExecutionQueue)
            {
                ExecutionQueue.Enqueue(action);
            }
        }

        private void OnDestroy()
        {
            HandlerManager.Instance.RemoveHandler(GetType());
        }

        #region Logging Methods

        protected static void Log(string message, 
            [CallerMemberName] string memberName = "", 
            [CallerFilePath] string filePath = "")
        {
            var className = GetClassName(filePath);
            var msg = $"[{className}.{memberName}] {message}";
            Debug.Log(msg);
        }
        
        protected static void LogWarning(string message, 
            [CallerMemberName] string memberName = "", 
            [CallerFilePath] string filePath = "")
        {
            var className = GetClassName(filePath);
            var msg = $"[{className}.{memberName}] {message}";
            Debug.LogWarning(msg);
        }

        protected static void LogError(string message, 
            [CallerMemberName] string memberName = "", 
            [CallerFilePath] string filePath = "")
        {
            var className = GetClassName(filePath);
            var msg = $"[{className}.{memberName}] {message}";
            Debug.LogError(msg);
        }

        private static string GetClassName(string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }

        #endregion
    }
    
    
    public enum HandlerState { Uninitialized, Initializing, Initialized }
}