
using System;
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

        string address = hostProductUserId.Trim();

        if (address.StartsWith("kcp://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(address, UriKind.Absolute, out Uri uri) || uri.Scheme != "kcp" || string.IsNullOrWhiteSpace(uri.Host) || uri.Port <= 0 || uri.Port > ushort.MaxValue)
            {
                LogService.LogError("Invalid KCP address: " + address);
                return;
            }

            LogService.Log("Starting Mirror client using KCP: " + address);
            _networkManager.StartClient(uri);
            return;
        }

        _networkManager.networkAddress = address;

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
