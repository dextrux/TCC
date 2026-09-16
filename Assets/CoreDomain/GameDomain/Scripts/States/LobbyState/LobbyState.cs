using System.Threading;
using CoreDomain.Scripts.Services.SceneService;
using CoreDomain.Scripts.Services.StateMachineService;
using UnityEngine;
using Zenject;

namespace CoreDomain.GameDomain.Scripts.States.LobbyState
{
     public class LobbyState : BaseGameState<LobbyInitiatorEnterData>
     {
         private readonly ISceneLoaderService _sceneLoaderService;

         public override GameStateType GameStateType => GameStateType.Lobby;
    
         public LobbyState(ISceneLoaderService sceneLoaderService, LobbyInitiatorEnterData gamePlayStateEnterData) : base(gamePlayStateEnterData)
         {
             _sceneLoaderService = sceneLoaderService;
         }

         public override async Awaitable LoadState(CancellationTokenSource cancellationTokenSource)
         {
             await base.LoadState(cancellationTokenSource);
             await _sceneLoaderService.TryLoadScene(ScenesType.LobbyScene, new LobbyInitiatorEnterData(), cancellationTokenSource);
         }

         public override async Awaitable ExitState(CancellationTokenSource cancellationTokenSource)
         {
             await base.ExitState(cancellationTokenSource);
             await _sceneLoaderService.TryUnloadScene(ScenesType.LobbyScene, cancellationTokenSource);
         }

         public class Factory : PlaceholderFactory<LobbyInitiatorEnterData, LobbyState>
         {
         }
    }
}