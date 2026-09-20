using Unity.Netcode;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.AudioSystem
{
    public class LocalAudioListener : NetworkBehaviour
    {
#region Settings Variables
        [Header("Local Listener Settings")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private AudioListener audioListener;
#endregion Settings Variables

        public override void OnNetworkSpawn()
        {
            SetupCamera();
            
            if (IsOwner)
                AttachListenerToLocalCamera();
            else
                DisableRemoteListener();
        }

        protected override void OnNetworkSessionSynchronized()
        {
            if (!IsOwner) return;
            
            DisableOtherListenersInScene();
        }

        private void SetupCamera()
        {
            if(playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();
            if (audioListener == null)
                audioListener = GetComponentInChildren<AudioListener>();
        }

        // Ativa a camera/listener deste jogador e desativa qualquer outro na cena
        private void AttachListenerToLocalCamera()
        {
            if (playerCamera != null) playerCamera.enabled = true;
            if (audioListener != null) audioListener.enabled = true;

            DisableOtherListenersInScene();
        }

        // Jogadores remotos nao devem ter camera nem listener ativos neste cliente
        private void DisableRemoteListener()
        {
            if (playerCamera != null) playerCamera.enabled = false;
            if (audioListener != null) audioListener.enabled = false;
        }

        private void DisableOtherListenersInScene()
        {
            var allListeners = FindObjectsByType<AudioListener>(sortMode: FindObjectsSortMode.None);
            foreach (var listener in allListeners)
            {
                if (listener != audioListener)
                    listener.enabled = false;
            }
        }
    }
}
