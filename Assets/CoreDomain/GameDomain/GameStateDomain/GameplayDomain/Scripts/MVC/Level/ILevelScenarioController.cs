using System.Threading;
using UnityEngine;

public interface ILevelScenarioController
{
    Awaitable CreateLevel(string levelNumber, CancellationTokenSource cancellationTokenSource);
    void DestroyTrack(bool shouldReleaseFromMemory);
    LevelScenarioView CurrentLevelView { get; }
}
