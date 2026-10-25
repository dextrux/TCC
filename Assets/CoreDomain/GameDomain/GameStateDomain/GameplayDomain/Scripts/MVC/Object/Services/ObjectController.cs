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

        public event Action<Vector3, float> OnImpact;
        public event Action<string> OnActionPerfom;

        public ObjectController(ObjectConfigurationSo configuration,
            IUpdateSubscriptionService updateSubscriptionService,
            ObjectView view, IInventoryManager inventoryManager)
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

            if (_view.TryGetComponent<ObjectSoundEmitter>(out var objectSoundEmitter))
                OnImpact += (_, impactSpeed) => objectSoundEmitter.EmitImpact(impactSpeed);

            if (_view.TryGetComponent<InteractableSoundEmitter>(out var interactableSoundEmitter))
                OnActionPerfom += interactableSoundEmitter.EmitAction;

            _updateSubscriptionService.RegisterFixedUpdatable(this);
        }

        /// <summary>
        /// Chamado pelo servidor quando o objeto e lancado (ver InventoryActionService).
        /// </summary>
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
                // Tudo que nao e interagivel vai para o inventario, que decide o que fazer com o item
                case ObjectType.OfferingObject:
                case ObjectType.ThrowableObject:
                case ObjectType.InventoryObject:
                    HandleInventory(connectionId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void HandleInteract(int connectionId)
        {
            // O requisito vale so para ACIONAR (acender, abrir...). Desligar nao exige o item
            if (!_model.IsOpen && !MeetsRequirement(connectionId))
            {
                LogService.Log($"[ObjectController] Player {connectionId} need {_configuration.RequiredHeldItem.ObjectName} in hands to use {_configuration.ObjectName}.");

                if (!string.IsNullOrEmpty(_configuration.BlockedAction))
                    OnActionPerfom?.Invoke(_configuration.BlockedAction);
                return;
            }

            LogService.Log($"[ObjectController] Player {connectionId} is interacting with {_configuration.ObjectName}.");
            _model.ToggleOpen();
            _view.SetOpen(_model.IsOpen);
            OnActionPerfom?.Invoke(_model.IsOpen ? _configuration.ActivateAction : _configuration.DeactivateAction);
        }

        private bool MeetsRequirement(int connectionId)
        {
            var required = _configuration.RequiredHeldItem;
            return required == null || _inventoryManager.IsHolding(connectionId, required.ItemId);
        }

        private void HandleInventory(int connectionId)
        {
            // Objeto durante arremesso nao pode ser coletado
            if (_isCollected || _model.IsThrown) return;

            LogService.Log($"[ObjectController] Player {connectionId} is adding to inventory {_configuration.ObjectName}.");
            var added = _inventoryManager.TryAddItem(connectionId, _configuration.ItemId,
                1, _configuration.MaxStackSize);

            if (!added)
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
            if (!_isSetup) return;
            _isSetup = false;

            _view.InteractRequested -= OnInteract;
            _updateSubscriptionService.UnregisterFixedUpdatable(this);

            if (_view != null && NetworkServer.active)
                NetworkServer.Destroy(_view.gameObject);
        }
    }
}
