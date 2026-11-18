using System;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Enemy.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Enemy.Interface;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Enemy.ScriptableObject;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.NoiseSystem.Interfaces;
using CoreDomain.Scripts.Services.Logger.Base;
using CoreDomain.Scripts.Services.UpdateService;
using Mirror;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Enemy.Services
{
    public class EnemyController : IEnemyController, IFixedUpdatable
    {
        private const float RepathInterval = 0.2f;

        private readonly EnemyConfigurationSo _configuration;
        private readonly EnemyView _view;
        private readonly EnemyModel _model;
        private readonly Vector3[] _waypoints;
        private readonly IUpdateSubscriptionService _updateSubscriptionService;
        private readonly INoiseManager _noiseManager;

        private Transform _target;
        private float _scanTimer;
        private float _repathTimer;
        private bool _isSetup;

        public EnemyController(EnemyConfigurationSo configuration, EnemyView view, Vector3[] waypoints,
            IUpdateSubscriptionService updateSubscriptionService, INoiseManager noiseManager)
        {
            _configuration = configuration;
            _view = view;
            _waypoints = waypoints ?? Array.Empty<Vector3>();
            _model = new EnemyModel();
            _updateSubscriptionService = updateSubscriptionService;
            _noiseManager = noiseManager;
        }

        public void Setup()
        {
            if (_isSetup) return;
            _isSetup = true;

            var listener = _view.NoiseListener;
            if (listener != null)
            {
                // "Mais proximo" = menor caminho pela NavMesh ate o ruido
                listener.SetResponseCostProvider(_view.GetPathDistance);
                listener.NoiseHeard += OnNoiseHeard;
                listener.Construct(_noiseManager);
            }

            _updateSubscriptionService.RegisterFixedUpdatable(this);
            EnterPatrol();
        }

        public void ManagedFixedUpdate()
        {
            if (!_isSetup) return;

            var deltaTime = Time.fixedDeltaTime;

            ScanForTarget(deltaTime);

            switch (_model.State)
            {
                case EnemyState.Patrol:
                    UpdatePatrol(deltaTime);
                    break;
                case EnemyState.Investigate:
                    UpdateInvestigate(deltaTime);
                    break;
                case EnemyState.Chase:
                    UpdateChase(deltaTime);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _view.SetAnimation(_view.IsMoving ? EnemyAnimState.Walk : EnemyAnimState.Idle);
        }

#region Perception
        private void ScanForTarget(float deltaTime)
        {
            _scanTimer -= deltaTime;
            if (_scanTimer > 0f) return;
            _scanTimer = _configuration.VisionScanInterval;

            if (_view.TryGetVisibleTarget(out var seen))
            {
                _target = seen;
                _model.SeeTarget(seen.position);

                if (_model.State != EnemyState.Chase) EnterChase();
                return;
            }

            if (_model.State != EnemyState.Chase) return;

            _model.AddLoseTime(_configuration.VisionScanInterval);
            if (_model.LoseTargetTimer < _configuration.LoseTargetTime) return;

            // Perdeu o jogador: vai ate onde o viu pela ultima vez
            _target = null;
            EnterInvestigate(_model.LastKnownTargetPosition);
        }

        // So chega aqui se este inimigo foi o mais proximo do ruido (ver NoiseManager)
        private void OnNoiseHeard(NoiseSignal signal, float perceivedIntensity)
        {
            if (_model.State == EnemyState.Chase) return;

            LogService.Log($"[EnemyController] {_configuration.EnemyName} vai investigar ruido em {signal.Position}");
            EnterInvestigate(signal.Position);
        }
#endregion Perception

#region States
        private void EnterPatrol()
        {
            _model.SetState(EnemyState.Patrol);
            _view.SetSpeed(_configuration.PatrolSpeed);

            if (_waypoints.Length == 0)
            {
                _view.StopMoving();
                return;
            }

            GoToWaypoint(_model.WaypointIndex);
        }

        private void EnterInvestigate(Vector3 point)
        {
            _model.SetState(EnemyState.Investigate);
            _view.SetSpeed(_configuration.InvestigateSpeed);

            if (!_view.SetDestination(point))
                EnterPatrol();
        }

        private void EnterChase()
        {
            _model.SetState(EnemyState.Chase);
            _view.SetSpeed(_configuration.ChaseSpeed);
            _repathTimer = 0f;
        }

        private void UpdatePatrol(float deltaTime)
        {
            if (_waypoints.Length == 0) return;

            if (_model.IsWaiting)
            {
                if (_model.TickWait(deltaTime))
                    GoToWaypoint(_model.NextWaypoint(_waypoints.Length));
                return;
            }

            if (!_view.HasReachedDestination()) return;

            _view.StopMoving();
            _model.StartWaiting(_configuration.WaypointWaitTime);
        }

        private void UpdateInvestigate(float deltaTime)
        {
            if (_model.IsWaiting)
            {
                if (_model.TickWait(deltaTime)) EnterPatrol();
                return;
            }

            if (!_view.HasReachedDestination()) return;

            _view.StopMoving();
            _model.StartWaiting(_configuration.InvestigateWaitTime);
        }

        private void UpdateChase(float deltaTime)
        {
            // Jogador sumiu (desconectou ou foi destruido)
            if (_target == null)
            {
                EnterInvestigate(_model.LastKnownTargetPosition);
                return;
            }

            _repathTimer -= deltaTime;
            if (_repathTimer > 0f) return;
            _repathTimer = RepathInterval;

            var destination = _model.IsTargetVisible ? _target.position : _model.LastKnownTargetPosition;
            _view.SetDestination(destination);
            
            _target.TryGetComponent<PlayerSanity>(out var targetSanity);
            targetSanity?.DecreaseSanity();
        }

        private void GoToWaypoint(int index)
        {
            if (_view.SetDestination(_waypoints[index])) return;

            // Waypoint fora da NavMesh: espera um pouco e tenta o proximo
            LogService.Log($"[EnemyController] {_configuration.EnemyName}: waypoint {index} fora da NavMesh.");
            _model.StartWaiting(1f);
        }
#endregion States

        public void Dispose()
        {
            if (!_isSetup) return;
            _isSetup = false;

            _updateSubscriptionService.UnregisterFixedUpdatable(this);

            var listener = _view != null ? _view.NoiseListener : null;
            if (listener != null)
            {
                listener.NoiseHeard -= OnNoiseHeard;
                listener.Release();
            }

            if (_view != null && NetworkServer.active)
                NetworkServer.Destroy(_view.gameObject);
        }
    }
}