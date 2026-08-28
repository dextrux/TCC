using System.Collections.Generic;
using System.Threading;
using CoreDomain.Scripts.Services.Logger.Base;
using CoreDomain.Scripts.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CoreDomain.Scripts.Services.AddressablesLoader
{
    public class AddressablesLoaderService : IAddressablesLoaderService
    {
        private readonly Dictionary<string, Object> _cachedAssetsPerAddress = new();

        public async Awaitable<T> LoadAsync<T>(string address, CancellationTokenSource cancellationTokenSource) where T : Object
        {
            if (_cachedAssetsPerAddress.TryGetValue(address, out var cachedAsset))
            {
                return TryGetComponent<T>(address, cachedAsset);
            }

            var handle = Addressables.LoadAssetAsync<Object>(address);
            await handle.WithCancellation(cancellationTokenSource.Token);
            cancellationTokenSource.Token.ThrowIfCancellationRequested();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var asset = handle.Result;
                _cachedAssetsPerAddress[address] = asset;
                return TryGetComponent<T>(address, asset);
            }

            LogService.LogError($"Failed to load asset at address: {address}");
            return null;
        }

        public void Release(string address)
        {
            if (!_cachedAssetsPerAddress.TryGetValue(address, out var obj))
            {
                return;
            }

            Addressables.Release(obj);
            _cachedAssetsPerAddress.Remove(address);
        }
    
        public void ReleaseAll()
        {
            foreach (var kvp in _cachedAssetsPerAddress)
            {
                Addressables.Release(kvp.Value);
            }
            _cachedAssetsPerAddress.Clear();
        }
    
        public bool IsLoaded(string address)
        {
            return _cachedAssetsPerAddress.ContainsKey(address);
        }

        private T TryGetComponent<T>(string address, Object cachedAsset) where T : Object
        {
            if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)) && cachedAsset is GameObject go)
            {
                var component = go.GetComponent<T>();
                if (component != null)
                {
                    return component;
                }

                LogService.LogError($"GameObject at address '{address}' does not have a component of type {typeof(T).Name}");
                return null;
            }
                
            return cachedAsset as T;
        }
    }
}
