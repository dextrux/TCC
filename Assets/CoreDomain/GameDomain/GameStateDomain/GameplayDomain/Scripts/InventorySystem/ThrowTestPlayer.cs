using System.Collections.Generic;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.Datas;
using CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.MVC.Object.ScriptableObject;
using CoreDomain.Scripts.Services.NetworkServices;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem
{
    /// <summary>
    /// CODIGO FEITO PELO CLAUDIO PARA TESTAR OS ARREMESSOS DE OBJETOS, NAO EH A VERSAO FINAL!!! ABRA AQUI
    /// SCRIPT DE TESTE - colocar no prefab do player (apague quando o player real tiver o arremesso).
    ///   E -> pega o objeto para onde a camera aponta (vai para o inventario)
    ///   Botao direito -> liga/desliga a mira (mostra o arco, se configurado)
    ///   Botao esquerdo -> arremessa o item selecionado no ponto mirado
    /// Os slots 1-3 e o scroll continuam sendo tratados pelo InventorySelectionInput.
    /// </summary>
    public class ThrowTestPlayer : NetworkBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Vazio = Camera.main")]
        [SerializeField] private Transform cameraTransform;
        [Tooltip("Opcional: LineRenderer para desenhar o arco")]
        [SerializeField] private LineRenderer trajectoryLine;
        [Tooltip("Opcional: SO do item arremessavel, so para a previa usar a velocidade e a gravidade dele")]
        [SerializeField] private ObjectConfigurationSo previewConfig;

        [Header("Ajustes")]
        [SerializeField] private float interactRange = 3f;
        [SerializeField] private float aimMaxDistance = 30f;
        [SerializeField] private float originForwardOffset = 0.8f;
        [Tooltip("Tire a camada do proprio player daqui")]
        [SerializeField] private LayerMask aimMask = ~0;

        private INetworkInventoryService _networkInventoryService;
        private readonly List<Vector3> _points = new List<Vector3>();
        private bool _isAiming;
        private bool _loggedMissingService;

        // Funciona se o prefab for injetado pelo Zenject (offline); online o Mirror spawna o player,
        // entao ResolveService() busca o servico nos containers.
        [Inject]
        public void Construct(INetworkInventoryService networkInventoryService)
            => _networkInventoryService = networkInventoryService;

        private void Update()
        {
            // Offline: nao ha NetworkClient. Online: so o player local.
            if (NetworkClient.active && !isLocalPlayer) return;

            var cam = GetCamera();
            if (cam == null) return;

            if (Input.GetKeyDown(KeyCode.E)) TryInteract(cam);

            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                _isAiming = !_isAiming;
                Debug.Log($"[ThrowTest] Mira {(_isAiming ? "LIGADA" : "desligada")}");
                if (!_isAiming) HideTrajectory();
            }

            if (!_isAiming) return;

            var origin = cam.position + cam.forward * originForwardOffset;
            var aimPoint = GetAimPoint(cam);

            DrawTrajectory(origin, aimPoint);

            if (!Input.GetKeyDown(KeyCode.Mouse0)) return;

            var service = ResolveService();
            if (service == null) return;

            Debug.Log($"[ThrowTest] RequestThrow | origem={origin} | mira={aimPoint}");
            service.RequestThrow(origin, aimPoint);

            _isAiming = false;
            HideTrajectory();
        }

        private void OnDisable() => HideTrajectory();

        private Transform GetCamera()
        {
            if (cameraTransform != null) return cameraTransform;

            if (Camera.main != null) cameraTransform = Camera.main.transform;
            return cameraTransform;
        }

        private void TryInteract(Transform cam)
        {
            if (!Physics.Raycast(cam.position, cam.forward, out var hit, interactRange, aimMask,
                    QueryTriggerInteraction.Ignore))
            {
                Debug.Log("[ThrowTest] E: nada na frente.");
                return;
            }

            var view = hit.collider.GetComponentInParent<ObjectView>();
            if (view == null)
            {
                Debug.Log($"[ThrowTest] E: '{hit.collider.name}' nao tem ObjectView.");
                return;
            }

            Debug.Log($"[ThrowTest] E: interagindo com '{view.name}'");
            view.CmdInteract();
        }

        private Vector3 GetAimPoint(Transform cam)
        {
            return Physics.Raycast(cam.position, cam.forward, out var hit, aimMaxDistance, aimMask,
                QueryTriggerInteraction.Ignore)
                ? hit.point
                : cam.position + cam.forward * aimMaxDistance;
        }

        private void DrawTrajectory(Vector3 origin, Vector3 aimPoint)
        {
            if (trajectoryLine == null || previewConfig == null) return;

            var speed = previewConfig.ThrowForce;
            var gravity = previewConfig.Gravity;

            var velocity = ThrowAim.GetLaunchDirection(origin, aimPoint, speed, gravity) * speed;
            var dt = Time.fixedDeltaTime;
            var position = origin;

            _points.Clear();
            _points.Add(position);

            for (var step = 1; step <= 200; step++)
            {
                velocity += Vector3.up * (gravity * dt);
                var next = position + velocity * dt;

                if (Physics.Linecast(position, next, out var hit, aimMask, QueryTriggerInteraction.Ignore))
                {
                    _points.Add(hit.point);
                    break;
                }

                position = next;
                if (step % 3 == 0) _points.Add(position);
            }

            trajectoryLine.enabled = true;
            trajectoryLine.positionCount = _points.Count;
            trajectoryLine.SetPositions(_points.ToArray());
        }

        private void HideTrajectory()
        {
            if (trajectoryLine != null) trajectoryLine.enabled = false;
        }

        private INetworkInventoryService ResolveService()
        {
            if (_networkInventoryService != null) return _networkInventoryService;

            var projectContainer = ProjectContext.Instance.Container;
            _networkInventoryService = projectContainer.TryResolve<INetworkInventoryService>();

            if (_networkInventoryService == null)
            {
                var registry = projectContainer.TryResolve<SceneContextRegistry>();

                for (var i = 0; registry != null && _networkInventoryService == null && i < SceneManager.sceneCount; i++)
                {
                    var sceneContext = registry.TryGetSceneContextForScene(SceneManager.GetSceneAt(i));
                    _networkInventoryService = sceneContext?.Container.TryResolve<INetworkInventoryService>();
                }
            }

            if (_networkInventoryService == null && !_loggedMissingService)
            {
                _loggedMissingService = true;
                Debug.LogError("[ThrowTest] INetworkInventoryService nao encontrado em nenhum container.");
            }

            return _networkInventoryService;
        }
    }
}