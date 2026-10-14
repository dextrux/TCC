using System;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.ScriptableObjects;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.SoundSystem;
using CoreDomain.Scripts.Services.UpdateService;
using Unity.Netcode;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Services
{
    public class ObjectController : IObjectController, IFixedUpdatable
    {
        private readonly ObjectConfigurationSo _configuration;
        private readonly ObjectView _view;
        private readonly ObjectModel _model;
        private readonly IUpdateSubscriptionService _updateSubscriptionService;
        private readonly IInventoryManager _inventoryManager;

        private bool _isSetup;
        private bool _isCollected;

        private ulong? _holder;
        public event Action<Vector3, float> Impacted;
        public event Action<string> ActionPerformed;

        public ObjectController(
            ObjectConfigurationSo objectConfiguration,
            IUpdateSubscriptionService updateSubscriptionService,
            IInventoryManager inventoryManager,
            ObjectView objectView)
        {
            _configuration = objectConfiguration;
            _updateSubscriptionService = updateSubscriptionService;
            _inventoryManager = inventoryManager;
            _view = objectView;
            _model = new ObjectModel();
        }

        public void Setup()
        {
            if (_isSetup) return;
            _isSetup = true;

            _view.InteractRequested += OnInteract;
            _view.ThrowRequested += OnThrowRequested;
            
            // Linkando ao sistema de som
            if (_view.TryGetComponent<ObjectSoundEmitter>(out var objectSoundEmitter))
                Impacted += (_, impactSpeed) => objectSoundEmitter.EmitImpact(impactSpeed);

            if (_view.TryGetComponent<InteractableSoundEmitter>(out var interactableSoundEmitter))
                ActionPerformed += interactableSoundEmitter.EmitAction;
            
            _updateSubscriptionService.RegisterFixedUpdatable(this);
        }

        public void Dispose()
        {
            if (!_isSetup) return;
            _isSetup = false;

            _view.InteractRequested -= OnInteract;
            _view.ThrowRequested -= OnThrowRequested;
            _updateSubscriptionService.UnregisterFixedUpdatable(this);
            
            var networkManager = NetworkManager.Singleton;
            if (_view != null && networkManager != null && networkManager.IsServer && _view.NetworkObject.IsSpawned)
                _view.NetworkObject.Despawn();
        }

        public void ManagedFixedUpdate()
        {
            if (!_model.IsThrown) return;

            var deltaTime = Time.fixedDeltaTime;

            _model.ApplyGravity(_configuration.gravity, deltaTime);

            if (_view.TryMove(_model.Velocity * deltaTime, out _)) return;

            HandleImpact();
        }

        // TODO: Chamar quando o jogador arremessar o objeto
        public void Throw(Vector3 origin,Vector3 direction)
        {
            if(_configuration.objectType != ObjectType.ThrowableObject) return;
            if (direction.sqrMagnitude <= Mathf.Epsilon) return;

            _view.Teleport(origin);
            _model.Throw(direction.normalized * _configuration.objectSpeedWhenThrown);
        }

        public void OnInteract(ulong clientId)
        {
            switch (_configuration.objectType)
            {
                case ObjectType.InteractObject:
                    HandleInteract(clientId);
                    break;
                case ObjectType.OfferingObject:
                    HandleInventoryPickup(clientId);
                    Debug.Log($"Offering Object (client {clientId})");
                    break;
                case ObjectType.ThrowableObject:
                    HandlePickup(clientId);
                    Debug.Log($"Throwable Object (client {clientId})");
                    break;
                case ObjectType.InventoryObject:
                    HandleInventoryPickup(clientId);
                    Debug.Log($"Inventory Object (client {clientId})");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void HandleImpact()
        {
            var impactSpeed = _model.Velocity.magnitude;

            _model.Stop();
            Impacted?.Invoke(_view.Position, impactSpeed);
        }

        private void HandlePickup(ulong clientId)
        {
            if (_holder.HasValue) return;

            Debug.Log($"[ObjectController] {clientId} pegou na mao o objeto {_configuration.objectName}");
            _holder = clientId;
            _view.SetHeld(true);
            _view.NetworkObject.ChangeOwnership(clientId);
        }

        private void HandleInventoryPickup(ulong clientId)
        {
            if (_isCollected) return;

            Debug.Log($"[ObjectController] {clientId} pegou a oferenda {_configuration.objectName}");
            var added = _inventoryManager.TryAddItem(
                clientId,
                _configuration.ItemId,
                1,
                _configuration.maxStackSize);

            if (!added)
            {
                //TODO: Avisar que inventario esta cheio
                Debug.Log($"[ObjectController] Inventario do jogador {clientId} esta cheio");
            }
            
            _isCollected = true;
            Dispose();
        }

        private void HandleInteract(ulong clientId)
        {
            Debug.Log($"[ObjectController] {clientId} interagiu com o objeto {_configuration.objectName}");
            _model.ToggleOpen();
            _view.SetOpen(_model.IsOpen);
            ActionPerformed?.Invoke(_model.IsOpen ? _configuration.activateAction : _configuration.deactivateAction);
        }

        private void OnThrowRequested(Vector3 origin, Vector3 direction) 
        {
            if (!_holder.HasValue) return;

            _holder = null;
            _view.SetHeld(false);
            _view.NetworkObject.RemoveOwnership();

            Throw(origin, direction);
        }
    }
}