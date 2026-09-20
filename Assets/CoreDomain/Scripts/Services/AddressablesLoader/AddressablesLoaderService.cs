using System.Collections.Generic;
using System.Threading;
using CoreDomain.Scripts.Services.Logger.Base;
using CoreDomain.Scripts.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace CoreDomain.Scripts.Services.AddressablesLoader {
    public class AddressablesLoaderService : IAddressablesLoaderService {
        private readonly Dictionary<string, Object> _cachedAssetsPerAddress = new();
        private readonly Dictionary<string, SceneInstance> _cachedScenesPerAddress = new();

        public async Awaitable<T> LoadAsync<T>(string address, CancellationTokenSource cancellationTokenSource) where T : Object {
            if (_cachedAssetsPerAddress.TryGetValue(address, out var cachedAsset)) {
                return TryGetComponent<T>(address, cachedAsset);
            }

            var handle = Addressables.LoadAssetAsync<Object>(address);
            await handle.WithCancellation(cancellationTokenSource.Token);
            cancellationTokenSource.Token.ThrowIfCancellationRequested();

            if (handle.Status == AsyncOperationStatus.Succeeded) {
                var asset = handle.Result;
                _cachedAssetsPerAddress[address] = asset;
                return TryGetComponent<T>(address, asset);
            }

            LogService.LogError($"Failed to load asset at address: {address}");
            return null;
        }

        public async Awaitable<SceneInstance> LoadSceneAsync(string address, LoadSceneMode loadMode, CancellationTokenSource cancellationTokenSource) {
            if (_cachedScenesPerAddress.TryGetValue(address, out var cachedScene)) {
                return cachedScene;
            }

            var handle = Addressables.LoadSceneAsync(address, loadMode);
            await handle.WithCancellation(cancellationTokenSource.Token);
            cancellationTokenSource.Token.ThrowIfCancellationRequested();

            if (handle.Status == AsyncOperationStatus.Succeeded) {
                var sceneInstance = handle.Result;
                _cachedScenesPerAddress[address] = sceneInstance;
                return sceneInstance;
            }

            LogService.LogError($"Failed to load scene at address: {address}");
            return default;
        }

        public async Awaitable UnloadSceneAsync(SceneInstance sceneInstance) {
            string addressToRemove = null;
            foreach (var kvp in _cachedScenesPerAddress) {
                if (kvp.Value.Scene == sceneInstance.Scene) {
                    addressToRemove = kvp.Key;
                    break;
                }
            }

            if (addressToRemove != null) {
                _cachedScenesPerAddress.Remove(addressToRemove);
            }

            var handle = Addressables.UnloadSceneAsync(sceneInstance);
            await handle.Task;
        }


        public void Release(string address) {
            if (_cachedAssetsPerAddress.TryGetValue(address, out var obj)) {
                Addressables.Release(obj);
                _cachedAssetsPerAddress.Remove(address);
                return;
            }

            if (_cachedScenesPerAddress.TryGetValue(address, out var sceneInstance)) {
                Addressables.UnloadSceneAsync(sceneInstance);
                _cachedScenesPerAddress.Remove(address);
            }
        }

        public void ReleaseAll() {
            foreach (var kvp in _cachedAssetsPerAddress) {
                Addressables.Release(kvp.Value);
            }
            _cachedAssetsPerAddress.Clear();

            foreach (var kvp in _cachedScenesPerAddress) {
                Addressables.UnloadSceneAsync(kvp.Value);
            }
            _cachedScenesPerAddress.Clear();
        }

        public bool IsLoaded(string address) {
            return _cachedAssetsPerAddress.ContainsKey(address) || _cachedScenesPerAddress.ContainsKey(address);
        }

        private T TryGetComponent<T>(string address, Object cachedAsset) where T : Object {
            if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)) && cachedAsset is GameObject go) {
                var component = go.GetComponent<T>();
                if (component != null) {
                    return component;
                }

                LogService.LogError($"GameObject at address '{address}' does not have a component of type {typeof(T).Name}");
                return null;
            }

            return cachedAsset as T;
        }
    }
}