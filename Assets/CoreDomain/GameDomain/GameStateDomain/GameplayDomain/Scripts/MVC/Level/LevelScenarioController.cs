using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Mvc.Level;
using CoreDomain.Scripts.Services.AddressablesLoader;
using CoreDomain.Scripts.Services.Logger.Base;
using System.Threading;
using UnityEngine;

public class LevelScenarioController : ILevelScenarioController {
    private readonly LevelFactory _levelFactory;

    private LevelTrackData _currentLevelTrackData;
    public LevelScenarioView CurrentLevelView => _currentLevelTrackData.LevelTrackView;

    public LevelScenarioController(IAddressablesLoaderService addressablesLoaderService) {
        _levelFactory = new LevelFactory(addressablesLoaderService);
    }

    public async Awaitable CreateLevel(string leveltoCreate, CancellationTokenSource cancellationTokenSource) {
        LogService.LogTopic($"Create level {leveltoCreate}, track adress: {leveltoCreate}", LogTopicType.LevelTrack);
        _currentLevelTrackData = new LevelTrackData(await _levelFactory.CreateLevel(leveltoCreate, cancellationTokenSource), leveltoCreate);
    }

    public void DestroyTrack(bool shouldReleaseFromMemory) {
        Object.Destroy(_currentLevelTrackData.LevelTrackView.gameObject);

        if (shouldReleaseFromMemory) {
            ReleaseCurrentLevelTrackFromMemory(_currentLevelTrackData.TrackAddress);
        }
    }

    private async void ReleaseCurrentLevelTrackFromMemory(string trackAddress) {
        await _levelFactory.ReleaseLevelFromMemory();
    }

    private class LevelTrackData {
        public readonly LevelScenarioView LevelTrackView;
        public readonly string TrackAddress;

        public LevelTrackData(LevelScenarioView levelTrackView, string trackAddress) {
            LevelTrackView = levelTrackView;
            TrackAddress = trackAddress;
        }
    }
}
