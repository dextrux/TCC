using System;
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

        private bool _isSetup;

        public event Action<Vector3, float> Impacted;
        public event Action<string> ActionPerformed;

        public ObjectController(
            ObjectConfigurationSo objectConfiguration,
            IUpdateSubscriptionService updateSubscriptionService,
            ObjectView objectView)
        {
            _configuration = objectConfiguration;
            _updateSubscriptionService = updateSubscriptionService;
            _view = objectView;
            _model = new ObjectModel();
        }

        public void Setup()
        {
            if (_isSetup) return;
            _isSetup = true;

            _view.InteractRequested += OnInteract;
            
            /// <summary>
            /// Linkando ao sistema de som
            /// </summary>
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
        public void Throw(Vector3 direction)
        {
            if(_configuration.objectType != ObjectType.ThrowableObject) return;
            if (direction.sqrMagnitude <= Mathf.Epsilon) return;

            _model.Throw(direction.normalized * _configuration.objectSpeedWhenThrown);
        }

        public void OnInteract(ulong clientId)
        {
            switch (_configuration.objectType)
            {
                case ObjectType.InteractObject:
                    HandleInteract();
                    break;
                case ObjectType.OfferingObject:
                    // TODO: Mandar pro inventario do jogador que interagiu com o item usando o clientId
                    Debug.Log($"Offering Object (client {clientId})");
                    break;
                case ObjectType.ThrowableObject:
                    // TODO: Linkar no jogador que interagiu com o item usando o clientId
                    Debug.Log($"Throwable Object (client {clientId})");
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

        private void HandleInteract()
        {
            _model.ToggleOpen();
            _view.SetOpen(_model.IsOpen);
            ActionPerformed?.Invoke(_model.IsOpen ? _configuration.activateAction : _configuration.deactivateAction);
        }
    }
}