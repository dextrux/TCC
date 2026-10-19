using System;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.ScriptableObject;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.SoundSystem;
using CoreDomain.Scripts.Services.Logger.Base;
using CoreDomain.Scripts.Services.UpdateService;
using Mirror;
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

        private int? _holder;
        public event Action<Vector3, float> OnImpact;
        public event Action<string> OnActionPerfom;

        public ObjectController(ObjectConfigurationSo configuration,
            IUpdateSubscriptionService updateSubscriptionService,
            ObjectView view , IInventoryManager inventoryManager)
        {
            _configuration = configuration;
            _view = view;
            _model = new ObjectModel();
            _updateSubscriptionService = updateSubscriptionService;
            _inventoryManager = inventoryManager;
        }
        
        public void Setup()
        {
            if (_isSetup) return;
            _isSetup = true;
            
            _view.InteractRequested += OnInteract;
            _view.ThrowRequested += OnThrowRequest;
            
            if(_view.TryGetComponent<ObjectSoundEmitter>(out var objectSoundEmitter))
                OnImpact += (_, impactSpeed) => objectSoundEmitter.EmitImpact(impactSpeed);

            if (_view.TryGetComponent<InteractableSoundEmitter>(out var interactableSoundEmitter))
                OnActionPerfom += interactableSoundEmitter.EmitAction;
            
            _updateSubscriptionService.RegisterFixedUpdatable(this);
        }

        public void Throw(Vector3 origin, Vector3 direction)
        {
            if (_configuration.ObjectType != ObjectType.ThrowableObject) return;
            if (direction.sqrMagnitude <= Mathf.Epsilon) return;
            
            _view.Teleport(origin);
            _model.Throw(direction.normalized * _configuration.ThrowForce);
        }

        public void OnInteract(int connectionId)
        {
            switch (_configuration.ObjectType)
            {
                case ObjectType.InteractObject:
                    HandleInteract(connectionId);
                    break;
                case ObjectType.OfferingObject:
                case ObjectType.InventoryObject:
                    HandleInventory(connectionId);
                    break;
                case ObjectType.ThrowableObject:
                    HandlePickup(connectionId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void HandleInteract(int connectionId)
        {
            LogService.Log($"[ObjectController] Player {connectionId} is interacting with {_configuration.ObjectName}.]");
            _model.ToggleOpen();
            _view.SetOpen(_model.IsOpen);
            OnActionPerfom?.Invoke(_model.IsOpen ? _configuration.ActivateAction : _configuration.DeactivateAction);
        }

        private void HandlePickup(int connectionId)
        {
            if(_holder.HasValue) return;
            if (!NetworkServer.connections.TryGetValue(connectionId, out var connection)) return;
            
            LogService.Log($"[ObjectController] Player {connectionId} is picking up {_configuration.ObjectName}.");
            _holder = connectionId;
            _view.SetHeld(true);
            _view.netIdentity.AssignClientAuthority(connection);
        }

        private void HandleInventory(int connectionId)
        {
            if (_isCollected) return;
            
            LogService.Log($"[ObjectController] Player {connectionId} is adding to inventory {_configuration.ObjectName}.");
            var added = _inventoryManager.TryAddItem(connectionId, _configuration.ItemId,
                1, _configuration.MaxStackSize);
            
            if(!added)
            {
                LogService.Log($"[ObjectController] Player {connectionId} failed to add {_configuration.ObjectName} in inventory.");
                return;
            }
            
            _isCollected = true;
            Dispose();
        }

        private void HandleImpact()
        {
            var impactSpeed = _model.Velocity.magnitude;
            
            _model.Stop();
            OnImpact?.Invoke(_view.Position, impactSpeed);
        }
        private void OnThrowRequest(Vector3 origin, Vector3 direction)
        {
            if (!_holder.HasValue) return;

            _holder = null;
            _view.SetHeld(false);
            _view.netIdentity.RemoveClientAuthority();
            
            Throw(origin, direction);
        }
        public void ManagedFixedUpdate()
        {
            if (!_model.IsThrown) return;

            var deltaTime = Time.fixedDeltaTime;

            _model.ApplyGravity(_configuration.Gravity, deltaTime);

            if (_view.TryMove(_model.Velocity * deltaTime, out _)) return;

            HandleImpact();
        }

        public void Dispose()
        {
            if(!_isSetup) return;
            _isSetup = false;
            
            _view.InteractRequested -= OnInteract;
            _view.ThrowRequested -= OnThrowRequest;
            _updateSubscriptionService.UnregisterFixedUpdatable(this);
            
            if(_view != null && NetworkServer.active)
                NetworkServer.Destroy(_view.gameObject);
        }
    }
}