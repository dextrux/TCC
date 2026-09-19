using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Mvc.Level;
using CoreDomain.Scripts.Services.AddressablesLoader;
using CoreDomain.Scripts.Services.Logger.Base;
using System.Threading;
using UnityEngine;

public class LevelScenarioController : MonoBehaviour {
    private readonly ILevelsDataService _levelsDataService;
    private readonly LevelFactory _levelFactory;

    private LevelTrackData _currentLevelTrackData;
    public LevelScenarioView CurrentLevelTrackView => _currentLevelTrackData.LevelTrackView;

    public LevelScenarioController(IAddressablesLoaderService addressablesLoaderService, ILevelsDataService levelsDataService) {
        _levelsDataService = levelsDataService;
        _levelFactory = new LevelFactory(addressablesLoaderService);
    }

    public async Awaitable CreateLevelTrack(int levelNumber, CancellationTokenSource cancellationTokenSource) {
        //LogService.LogTopic($"Create level {levelNumber} track , track adress: {trackAddress}", LogTopicType.LevelTrack);
        //_currentLevelTrackData = new LevelTrackData(await _levelFactory.CreateLevel(levelNumber, cancellationTokenSource), trackAddress);
    }

    public void DestroyTrack(bool shouldReleaseFromMemory) {
        Object.Destroy(_currentLevelTrackData.LevelTrackView.gameObject);

        if (shouldReleaseFromMemory) {
            ReleaseCurrentLevelTrackFromMemory(_currentLevelTrackData.TrackAddress);
        }
    }

    private void ReleaseCurrentLevelTrackFromMemory(string trackAddress) {
        _levelFactory.ReleaseTrackFromMemory(trackAddress);
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
