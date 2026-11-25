using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Data;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces;
using CoreDomain.GameDomain.Scripts.AudioSystem.Datas;
using CoreDomain.GameDomain.Scripts.AudioSystem.Interfaces;
using CoreDomain.Scripts.Services.NetworkService;
using CoreDomain.Scripts.Services.NetworkServices;
using Mirror;
using System;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.GameplayNetwork
{
    public class GameplayNetworkBridge : IGameplayNetworkBridge
    {
        private readonly INetworkSoundService _networkSoundService;
        private readonly INetworkInventoryService _networkInventoryService;
        private readonly IAudioManager _audioManager;
        private readonly INoiseManager _noiseManager;
        private readonly IInventoryManager _inventoryManager;
        private readonly IInventoryActionManager _inventoryActionManager;

        private bool _configured;

        public GameplayNetworkBridge(INetworkSoundService networkSoundService,
            INetworkInventoryService networkInventoryService, IAudioManager audioManager,
            INoiseManager noiseManager, IInventoryManager inventoryManager,
            IInventoryActionManager inventoryActionManager)
        {
            _networkSoundService = networkSoundService;
            _networkInventoryService = networkInventoryService;
            _audioManager = audioManager;
            _noiseManager = noiseManager;
            _inventoryManager = inventoryManager;
            _inventoryActionManager = inventoryActionManager;
        }
        
        public void Configure(bool offline)
        {
            if (_configured) return;
            _configured = true;

            _networkSoundService.Configure(offline, PlayAudio, ReportNoise);

            _networkInventoryService.Configure(offline, SelectSlot, ScrollSlot, RemoveInventory,
                OnThrowRequested, OnUseRequested, AddPlayerInventory);
            _inventoryManager.InventoryChanged += OnInventoryChanged;

        }

        public void Release()
        {
            if (!_configured) return;
            _configured = false;

            _networkSoundService.Configure(false, null, null);

            _inventoryManager.InventoryChanged -= OnInventoryChanged;
            _networkInventoryService.Configure(false, null, null, null, 
                null, null, null);

        }

#region Som e ruido
        private void PlayAudio(int soundId, Vector3 position, float volumeMultiplier)
        {
            if (!Enum.IsDefined(typeof(AudioEventId), soundId))
            {
                Debug.LogWarning("[GameplayNetworkBridge] Unknown AudioEventId.");
                return;
            }

            AudioEventId audioId = (AudioEventId)soundId;

            if (audioId == AudioEventId.None)
            {
                return;
            }

            var signal = new AudioSignal(audioId, position, null, volumeMultiplier);
            _audioManager.ReportSignal(signal);
        }

        private void ReportNoise(Vector3 position, float loudness, GameObject source)
        {
            var signal = new NoiseSignal(position, loudness, source);
            _noiseManager.ReportSignal(signal);
        }
#endregion Som e ruido

#region Inventario
        private void SelectSlot(int connectionId, int index)
        {
            _inventoryManager.GetOrCreateInventory(connectionId).Selected(index);
        }

        private void ScrollSlot(int connectionId, int direction)
        {
            _inventoryManager.GetOrCreateInventory(connectionId).ScrollToSelected(direction);
        }

        private void RemoveInventory(int connectionId)
        {
            _inventoryActionManager.OnHeldItemChanged(connectionId, 0);
            _inventoryManager.Remove(connectionId);
        }

        private void OnInventoryChanged(int connectionId, PlayerInventory inventory)
        {
            var stack = inventory.SelectedStack;
            var heldItemId = stack.IsEmpty() ? 0 : stack.ItemId;

            _inventoryActionManager.OnHeldItemChanged(connectionId, heldItemId);
            _networkInventoryService.NotifyHeldItemChanged(connectionId, heldItemId);
        }

        private void OnThrowRequested(int connectionId, Vector3 origin, Vector3 aimPoint)
        {
            _inventoryActionManager.TryThrowSelected(connectionId, origin, aimPoint);
        }

        private void OnUseRequested(int connectionId)
        {
            if (_inventoryActionManager.TryUseSelected(connectionId, out var active))
                _networkInventoryService.NotifyHeldItemActiveChanged(connectionId, active);
        }
        
        private void AddPlayerInventory(int connectionId) => _inventoryManager.GiveStartingItems(connectionId);
#endregion Inventario
    }
}
