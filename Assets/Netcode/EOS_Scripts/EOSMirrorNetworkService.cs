
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
    private readonly Transport _defaultTransport;
    private readonly kcp2k.KcpTransport _kcpTransport;

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

    public bool IsInitialized => EOSSDKComponent.Initialized && EOSSDKComponent.LocalUserProductId != null;

    public bool SupportsLocalKcp => _kcpTransport != null && _kcpTransport.Available();

    public ushort LocalKcpPort => _kcpTransport != null ? _kcpTransport.Port : (ushort)7777;

    public EOSMirrorNetworkService(EOSMirrorNetworkManager networkManager)
    {
        _networkManager = networkManager;
        _defaultTransport = networkManager.transport;
        _kcpTransport = networkManager.GetComponent<kcp2k.KcpTransport>();
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

        SelectTransport(_defaultTransport);

        LogService.Log("Starting Mirror host using EOS.");
        _networkManager.StartHost();
    }

    public void StartLocalHost()
    {
        if (!SupportsLocalKcp)
        {
            LogService.LogError("KCP Transport is not available.");
            return;
        }

        if (NetworkServer.active || NetworkClient.active)
        {
            LogService.LogWarning("Network session is already active.");
            return;
        }

        SelectTransport(_kcpTransport);

        LogService.Log("Starting Mirror host using KCP on port " + LocalKcpPort);
        _networkManager.StartHost();
    }

    public void StartClient(string address)
    {
        if (NetworkServer.active || NetworkClient.active)
        {
            LogService.LogWarning("Network session is already active.");
            return;
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            LogService.LogError("Network address is empty.");
            return;
        }

        address = address.Trim();

        if (address.StartsWith("kcp://", StringComparison.OrdinalIgnoreCase))
        {
            StartKcpClient(address);
            return;
        }

        if (!IsInitialized)
        {
            LogService.LogError("EOS is not initialized.");
            return;
        }

        SelectTransport(_defaultTransport);

        _networkManager.networkAddress = address;

        LogService.Log("Starting Mirror client using EOS.");
        _networkManager.StartClient();
    }

    private void StartKcpClient(string address)
    {
        if (!SupportsLocalKcp)
        {
            LogService.LogError("KCP Transport is not available.");
            return;
        }

        if (!Uri.TryCreate(address, UriKind.Absolute, out Uri uri))
        {
            LogService.LogError("Invalid KCP address: " + address);
            return;
        }

        if (!string.Equals(uri.Scheme, "kcp", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(uri.Host) || uri.Port <= 0 || uri.Port > ushort.MaxValue)
        {
            LogService.LogError("Invalid KCP address. Expected: kcp://127.0.0.1:7777");
            return;
        }

        SelectTransport(_kcpTransport);

        LogService.Log("Starting Mirror client using KCP: " + uri);
        _networkManager.StartClient(uri);
    }

    private void SelectTransport(Transport selectedTransport)
    {
        if (selectedTransport == null)
        {
            throw new InvalidOperationException("Selected Mirror Transport is null.");
        }

        if (NetworkServer.active || NetworkClient.active)
        {
            throw new InvalidOperationException("Cannot change transport while a Mirror session is active.");
        }

        _networkManager.transport = selectedTransport;
        Transport.active = selectedTransport;

        LogService.Log("Selected Mirror Transport: " + selectedTransport.GetType().Name);
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

    public void NotifyGameplayReady()
    {
        _networkManager.NotifyGameplayReady();
    }
}
