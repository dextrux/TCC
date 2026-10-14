
using System;
using System.Collections.Generic;
using CoreDomain.Scripts.Services.NetworkService;
using Mirror;
using UnityEngine;

public class EOSMirrorSoundService : INetworkSoundService
{
    private readonly Dictionary<int, double> _lastSoundTime = new Dictionary<int, double>();

    private Action<int, Vector3, float> _playAudio;
    private Action<Vector3, float, GameObject> _reportNoise;

    private bool _offline;
    private bool _serverHandlerRegistered;
    private bool _clientHandlerRegistered;

    private const double MinimumSoundInterval = 0.08d;
    private const float MaxAudioLoudness = 15f;

    public void Configure(bool offline, Action<int, Vector3, float> playAudio, Action<Vector3, float, GameObject> reportNoise)
    {
        _offline = offline;
        _playAudio = playAudio;
        _reportNoise = reportNoise;
    }

    public void SetUp()
    {
        if (NetworkServer.active && !_serverHandlerRegistered)
        {
            NetworkServer.RegisterHandler<PlayerSoundInputMessage>(OnPlayerSoundInputMessage);
            _serverHandlerRegistered = true;
        }

        if (NetworkClient.active && !_clientHandlerRegistered)
        {
            NetworkClient.RegisterHandler<PlaySoundMessage>(OnPlaySoundMessage);
            _clientHandlerRegistered = true;
        }
    }

    public void StopServer()
    {
        if (_serverHandlerRegistered)
        {
            NetworkServer.UnregisterHandler<PlayerSoundInputMessage>();
            _serverHandlerRegistered = false;
        }

        _lastSoundTime.Clear();
    }

    public void StopClient()
    {
        if (_clientHandlerRegistered)
        {
            NetworkClient.UnregisterHandler<PlaySoundMessage>();
            _clientHandlerRegistered = false;
        }
    }

    public void RequestSound(int soundId, float loudness, Vector3 position)
    {
        if (!TryGetSoundSettings(soundId, out float maxLoudness, out bool clientAllowed))
        {
            Debug.LogWarning("[EOSMirrorSoundService] Unknown sound ID.");
            return;
        }

        if (!IsValidLoudness(loudness, maxLoudness))
        {
            Debug.LogWarning("[EOSMirrorSoundService] Invalid loudness.");
            return;
        }

        if (_offline)
        {
            ProcessLocalSound(soundId, loudness, position, null);
            return;
        }

        if (!clientAllowed)
        {
            Debug.LogWarning("[EOSMirrorSoundService] This sound is server-only.");
            return;
        }

        if (!NetworkClient.isConnected)
        {
            Debug.LogWarning("[EOSMirrorSoundService] Client is not connected.");
            return;
        }

        NetworkClient.Send(new PlayerSoundInputMessage
        {
            SoundId = soundId,
            Loudness = loudness
        });
    }

    public void EmitServerSound(int soundId, float loudness, Vector3 position, GameObject source = null)
    {
        if (!TryGetSoundSettings(soundId, out float maxLoudness, out _))
        {
            Debug.LogWarning("[EOSMirrorSoundService] Unknown sound ID.");
            return;
        }

        if (!IsValidLoudness(loudness, maxLoudness))
        {
            Debug.LogWarning("[EOSMirrorSoundService] Invalid loudness.");
            return;
        }

        if (_offline)
        {
            ProcessLocalSound(soundId, loudness, position, source);
            return;
        }

        if (!NetworkServer.active)
        {
            return;
        }

        ProcessServerSound(soundId, loudness, position, source);
    }

    private void OnPlayerSoundInputMessage(NetworkConnectionToClient connection, PlayerSoundInputMessage message)
    {
        if (!NetworkServer.active || connection == null || connection.identity == null)
        {
            return;
        }

        if (!TryGetSoundSettings(message.SoundId, out float maxLoudness, out bool clientAllowed))
        {
            return;
        }

        if (!clientAllowed || !IsValidLoudness(message.Loudness, maxLoudness))
        {
            return;
        }

        int connectionId = connection.connectionId;
        double currentTime = NetworkTime.time;

        if (_lastSoundTime.TryGetValue(connectionId, out double lastTime))
        {
            if (currentTime - lastTime < MinimumSoundInterval)
            {
                return;
            }
        }

        GameObject player = connection.identity.gameObject;

        if (!player.activeInHierarchy)
        {
            return;
        }

        _lastSoundTime[connectionId] = currentTime;

        // A posicao vem do jogador no servidor, nunca da mensagem do cliente.
        ProcessServerSound(message.SoundId, message.Loudness, player.transform.position, player);
    }

    private void ProcessServerSound(int soundId, float loudness, Vector3 position, GameObject source)
    {
        if (loudness > 0f)
        {
            if (_reportNoise != null)
            {
                _reportNoise.Invoke(position, loudness, source);
            }
            else
            {
                Debug.LogWarning("[EOSMirrorSoundService] Server noise handler is not configured.");
            }
        }

        if (soundId == 0)
        {
            return;
        }

        float volumeMultiplier = GetVolumeMultiplier(loudness);

        NetworkServer.SendToAll(new PlaySoundMessage
        {
            SoundId = soundId,
            Position = position,
            VolumeMultiplier = volumeMultiplier
        });
    }

    private void ProcessLocalSound(int soundId, float loudness, Vector3 position, GameObject source)
    {
        if (loudness > 0f)
        {
            _reportNoise?.Invoke(position, loudness, source);
        }

        if (soundId == 0)
        {
            return;
        }

        _playAudio?.Invoke(soundId, position, GetVolumeMultiplier(loudness));
    }

    private void OnPlaySoundMessage(PlaySoundMessage message)
    {
        if (!TryGetSoundSettings(message.SoundId, out _, out _))
        {
            return;
        }

        if (message.SoundId == 0)
        {
            return;
        }

        if (float.IsNaN(message.VolumeMultiplier) || float.IsInfinity(message.VolumeMultiplier))
        {
            return;
        }

        _playAudio?.Invoke(message.SoundId, message.Position, Mathf.Clamp01(message.VolumeMultiplier));
    }

    private float GetVolumeMultiplier(float loudness)
    {
        if (loudness <= 0f)
        {
            return 1f;
        }

        return Mathf.Clamp01(loudness / MaxAudioLoudness);
    }

    private bool IsValidLoudness(float loudness, float maxLoudness)
    {
        return !float.IsNaN(loudness) && !float.IsInfinity(loudness) && loudness >= 0f && loudness <= maxLoudness;
    }

    private bool TryGetSoundSettings(int soundId, out float maxLoudness, out bool clientAllowed)
    {
        maxLoudness = 0f;
        clientAllowed = false;

        // IDs correspondentes ao AudioEventId da branch DevMergeTeste.
        switch (soundId)
        {
            case 0:
            {
                // None: somente ruido.
                maxLoudness = 20f;
                clientAllowed = true;
                return true;
            }

            case 1:
            {
                // Footstep.
                maxLoudness = 12f;
                clientAllowed = true;
                return true;
            }

            case 2:
            {
                // DoorOpen.
                maxLoudness = 20f;
                return true;
            }

            case 3:
            {
                // DoorClose.
                maxLoudness = 20f;
                return true;
            }

            case 4:
            {
                // Torch.
                maxLoudness = 3f;
                clientAllowed = true;
                return true;
            }

            case 5:
            {
                // Test.
                maxLoudness = 10f;
                clientAllowed = true;
                return true;
            }

            default:
            {
                return false;
            }
        }
    }
}
