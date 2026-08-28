using System;
using System.Threading;
using CoreDomain.Scripts.Mvc.LoadingScreen;
using CoreDomain.Scripts.Services.Logger.Base;
using CoreDomain.Scripts.Services.SceneService;
using UnityEngine;
using Zenject;

namespace CoreDomain.Scripts.CoreInitiator {
    public class CoreInitiator : MonoBehaviour {
        private GameInputActions _gameInputActions;
        private ISceneLoaderService _sceneLoaderService;
        private ILoadingScreenController _loadingScreenController;

        [Inject]
        private void Setup(GameInputActions gameInputActions, ISceneLoaderService sceneLoaderService, ILoadingScreenController loadingScreenController) {
            _gameInputActions = gameInputActions;
            _sceneLoaderService = sceneLoaderService;
            _loadingScreenController = loadingScreenController;
        }

        private void Start() {
            _ = InitEntryPoint(CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken));
        }

        private async Awaitable InitEntryPoint(CancellationTokenSource cancellationTokenSource) {
            try {
                UpdateApplicationSettings();
                _loadingScreenController.Show();
                InitializeServices();
                await LoadGameScene(cancellationTokenSource);
                await _loadingScreenController.SetLoadingSlider(1, cancellationTokenSource);
            }
            catch (OperationCanceledException) {
                LogService.Log("Operation init core was cancelled");
            }
            catch (Exception e) {
                LogService.LogError(e.Message);
                throw;
            }

            _loadingScreenController.Hide();
        }

        private void UpdateApplicationSettings() {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 60;
        }

        private void InitializeServices() {
            _gameInputActions.Enable();
            _sceneLoaderService.InitEntryPoint();
        }

        private async Awaitable LoadGameScene(CancellationTokenSource cancellationTokenSource) {
            await _sceneLoaderService.TryLoadScene(SceneType.GameScene, new GameInitiatorEnterData(), cancellationTokenSource);
        }
    }
}