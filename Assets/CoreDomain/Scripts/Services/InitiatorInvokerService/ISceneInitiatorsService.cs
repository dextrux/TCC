using System.Threading;
using CoreDomain.Scripts.CoreInitiator.Base;
using CoreDomain.Scripts.Services.SceneService;
using UnityEngine;

namespace CoreDomain.Scripts.Services.InitiatorInvokerService
{
    public interface ISceneInitiatorsService
    {
        void RegisterInitiator(ISceneInitiator sceneInitiator);
        void UnregisterInitiator(ISceneInitiator sceneInitiator);
        Awaitable InvokeInitiatorLoadEntryPoint(ScenesType sceneType, IInitiatorEnterData enterData, CancellationTokenSource cancellationTokenSource);
        Awaitable InvokeInitiatorStartEntryPoint(ScenesType sceneType, IInitiatorEnterData enterData, CancellationTokenSource cancellationTokenSource);
        Awaitable InvokeInitiatorExitPoint(ScenesType sceneType, CancellationTokenSource cancellationTokenSource);
    }
}