using System.Threading;
using CoreDomain.Scripts.Services.StateMachineService;

namespace CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Services.LevelCancellationToken
{
    public class LevelCancellationTokenService : ILevelCancellationTokenService
    {
        public CancellationTokenSource CancellationTokenSource => CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);
        
        private CancellationTokenSource _cancellationTokenSource;
        private readonly IStateMachineService _stateMachineService;

        public LevelCancellationTokenService(IStateMachineService stateMachineService)
        {
            _stateMachineService = stateMachineService;
        }

        public void InitCancellationToken()
        {
            _cancellationTokenSource = _stateMachineService.CurrentState().CancellationTokenSource;
        }
        
        public void CancelCancellationToken()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}
