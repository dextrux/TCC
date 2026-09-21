using System.Threading;
using CoreDomain.Scripts.Services.Logger.Base;
using CoreDomain.Scripts.Services.NetworkService;
using EpicTransport;
using Mirror;
using UnityEngine;

public class EOSMirrorNetworkService : INetworkService
{
    private readonly EOSMirrorNetworkManager _networkManager;

    public string LocalProductUserId
    {
        get
        {
            if (EOSSDKComponent.LocalUserProductId == null)
            {
                return string.Empty;
            }

            return EOSSDKComponent.LocalUserProductId.ToString();
        }
    }

    public bool IsInitialized
    {
        get
        {
            return EOSSDKComponent.Initialized && EOSSDKComponent.LocalUserProductId != null;
        }
    }

    public EOSMirrorNetworkService(EOSMirrorNetworkManager networkManager)
    {
        _networkManager = networkManager;
    }

    public async Awaitable WaitUntilInitialized(CancellationTokenSource cancellationTokenSource)
    {
        while (!IsInitialized)
        {
            cancellationTokenSource.Token.ThrowIfCancellationRequested();
            await Awaitable.NextFrameAsync();
        }
    }

    public void StartHost()
    {
        if (!IsInitialized)
        {
            LogService.LogError("EOS is not initialized.");
            return;
        }

        if (NetworkServer.active || NetworkClient.active)
        {
            LogService.LogWarning("Network session is already active.");
            return;
        }

        LogService.Log("Starting Mirror host.");
        _networkManager.StartHost();
    }

    public void StartClient(string hostProductUserId)
    {
        if (!IsInitialized)
        {
            LogService.LogError("EOS is not initialized.");
            return;
        }

        if (NetworkServer.active || NetworkClient.active)
        {
            LogService.LogWarning("Network session is already active.");
            return;
        }

        if (string.IsNullOrWhiteSpace(hostProductUserId))
        {
            LogService.LogError("Host Product User ID is empty.");
            return;
        }

        _networkManager.networkAddress = hostProductUserId.Trim();

        LogService.Log("Starting Mirror client.");
        _networkManager.StartClient();
    }

    public void Disconnect()
    {
        if (NetworkServer.active && NetworkClient.active)
        {
            _networkManager.StopHost();
            return;
        }

        if (NetworkClient.active)
        {
            _networkManager.StopClient();
            return;
        }

        if (NetworkServer.active)
        {
            _networkManager.StopServer();
        }
    }
}