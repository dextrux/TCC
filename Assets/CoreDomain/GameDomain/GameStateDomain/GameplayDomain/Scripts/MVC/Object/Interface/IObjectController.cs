using System;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Interface
{
    public interface IObjectController
    {
        /// <summary>
        /// Disparado quando o objeto arremessado atinge algo (posicao, velocidade do impacto).
        /// </summary>
        event Action<Vector3, float> Impacted;
        
        /// <summary>
        /// Disparado quando o objeto eh do tipo de interacao
        /// </summary>
        public event Action<string> ActionPerformed;

        void Setup();
        void Throw(Vector3 direction);
        void OnInteract(ulong clientId);
        void Dispose();
    }
}