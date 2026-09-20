using System.Threading;
using CoreDomain.Scripts.Services.AddressablesLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Mvc.Level {
    public class LevelFactory {
        private readonly IAddressablesLoaderService _addressablesLoaderService;

        private SceneInstance _loadedScene;

        public LevelFactory(IAddressablesLoaderService addressablesLoaderService) {
            _addressablesLoaderService = addressablesLoaderService;
        }

        public async Awaitable<LevelScenarioView> CreateLevel(string trackAddress, CancellationTokenSource cancellationTokenSource) {

            _loadedScene = await _addressablesLoaderService.LoadSceneAsync(
                trackAddress,
                LoadSceneMode.Additive,
                cancellationTokenSource
            );

            foreach (var rootObject in _loadedScene.Scene.GetRootGameObjects()) {
                if (rootObject.TryGetComponent<LevelScenarioView>(out var view)) {
                    return view;
                }
            }

            Debug.LogError($"[LevelFactory] LevelScenarioView não encontrado nos objetos raiz da cena: {trackAddress}");
            return null;
        }

        public async Awaitable ReleaseLevelFromMemory() {
            if (_loadedScene.Scene.isLoaded) {
                await _addressablesLoaderService.UnloadSceneAsync(_loadedScene);
            }
        }
    }
}