using System.Threading;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace CoreDomain.Scripts.Services.AddressablesLoader {
    public interface IAddressablesLoaderService {
        Awaitable<T> LoadAsync<T>(string address, CancellationTokenSource cancellationTokenSource) where T : Object;
        void Release(string address);
        void ReleaseAll();
        bool IsLoaded(string address);

        Awaitable<SceneInstance> LoadSceneAsync(string address, LoadSceneMode loadMode, CancellationTokenSource cancellationTokenSource);
        Awaitable UnloadSceneAsync(SceneInstance sceneInstance);
    }
}