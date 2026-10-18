using System;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Interface
{
    public interface IObjectController
    {
        /// <summary>
        /// Quando objeto que foi arremesado acertar algo
        /// </summary>
        event Action<Vector3, float> OnImpact;

        /// <summary>
        /// Quando objeto for do tipo interacao
        /// </summary>
        event Action<string> OnActionPerfom;

        void Setup();
        void Throw(Vector3 origin, Vector3 direction);
        void OnInteract(int connectionId);
        void Dispose();
    }
}