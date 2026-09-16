using System.Threading;

namespace CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Services.LevelCancellationToken
{
    public interface ILevelCancellationTokenService
    {
        CancellationTokenSource CancellationTokenSource { get; }
        void InitCancellationToken();
        void CancelCancellationToken();
    }
}