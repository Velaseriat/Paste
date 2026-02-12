using UnityEngine;

namespace Felsan.Scripts.Shared.Handlers.Interfaces
{
    public interface IBaseHandler
    {
        protected HandlerState HandlerState { get; set; }
        
        void PreInitialize();
        void Initialize(HandlerManager handlerManager);
    }
}