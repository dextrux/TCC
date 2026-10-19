using System;
using CoreDomain.Scripts.Services.NetworkServices;
using Mirror;
using Netcode.EOS_Scripts.Messages;
using UnityEngine;

namespace Netcode.EOS_Scripts.Services
{
    public class EOSMirrorInventoryService : INetworkInventoryService
    {
        // No Mirror o host tem connectionId 0; o offline usa o mesmo id para o jogador unico
        private const int OfflineConnectionId = 0;

        private Action<int, int> _selectSlot;
        private Action<int, int> _scroll;
        private Action<int> _removePlayer;

        private bool _offline;
        private bool _handlersRegistered;

        public event Action<int> OfflineHeldItemChanged;

        public void Configure(bool offline, Action<int, int> selectSlot, Action<int, int> scroll, Action<int> removePlayer)
        {
            _offline = offline;
            _selectSlot = selectSlot;
            _scroll = scroll;
            _removePlayer = removePlayer;
        }

        public void SetUp()
        {
            if (!NetworkServer.active || _handlersRegistered) return;
            
            NetworkServer.RegisterHandler<InventorySelectSlotMessage>(OnSelectSlotMessage);
            NetworkServer.RegisterHandler<InventoryScrollMessage>(OnScrollMessage);
            _handlersRegistered = true;
        }

        public void StopServer()
        {
            if (!_handlersRegistered) return;

            NetworkServer.UnregisterHandler<InventorySelectSlotMessage>();
            NetworkServer.UnregisterHandler<InventoryScrollMessage>();
            _handlersRegistered = false;
        }

        public void StopClient()
        {
            // Nada a fazer no cliente - a mao vem por meio do SyncVar
        }

        public void RemovePlayer(int connectionId) => _removePlayer?.Invoke(connectionId);

        public void RequestSelectSlot(int index)
        {
            if (_offline)
            {
                _selectSlot?.Invoke(OfflineConnectionId, index);
                return;
            }

            if (!NetworkClient.isConnected) return;

            NetworkClient.Send(new InventorySelectSlotMessage { Index = index });
        }

        public void RequestScroll(int direction)
        {
            if (_offline)
            {
                _scroll?.Invoke(OfflineConnectionId, Mathf.Clamp(direction, -1, 1));
                return;
            }

            if (!NetworkClient.isConnected) return;

            NetworkClient.Send(new InventoryScrollMessage { Direction = direction });
        }

        public void NotifyHeldItemChanged(int connectionId, int itemId)
        {
            if (_offline)
            {
                if (connectionId == OfflineConnectionId) OfflineHeldItemChanged?.Invoke(itemId);
                return;
            }

            // Online so o servidor escreve o SyncVar (clientes puros nao tem inventario)
            if (!NetworkServer.active) return;
            if (!NetworkServer.connections.TryGetValue(connectionId, out var connection)) return;
            if (connection.identity == null) return;

            var target = connection.identity.GetComponent<INetworkHeldItemTarget>();
            target?.ServerSetHeldItem(itemId);
        }

        private void OnSelectSlotMessage(NetworkConnectionToClient connection, InventorySelectSlotMessage message)
        {
            if (connection == null || connection.identity == null) return;

            _selectSlot?.Invoke(connection.connectionId, message.Index);
        }

        private void OnScrollMessage(NetworkConnectionToClient connection, InventoryScrollMessage message)
        {
            if (connection == null || connection.identity == null) return;

            _scroll?.Invoke(connection.connectionId, Mathf.Clamp(message.Direction, -1, 1));
        }
    }
}
