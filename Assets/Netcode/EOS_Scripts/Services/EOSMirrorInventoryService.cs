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
        private Action<int, Vector3, Vector3> _throw;
        private Action<int> _useSelected;

        private bool _offline;
        private bool _handlersRegistered;

        public event Action<int> OfflineHeldItemChanged;
        public event Action<bool> OfflineHeldItemActiveChanged;

        public void Configure(bool offline, Action<int, int> selectSlot, Action<int, int> scroll, Action<int> removePlayer, Action<int, Vector3, Vector3> throwAction, Action<int> useSelected)
        {
            _offline = offline;
            _selectSlot = selectSlot;
            _scroll = scroll;
            _removePlayer = removePlayer;
            _throw = throwAction;
            _useSelected =  useSelected;
        }

        public void SetUp()
        {
            if (!NetworkServer.active || _handlersRegistered) return;
            
            NetworkServer.RegisterHandler<InventorySelectSlotMessage>(OnSelectSlotMessage);
            NetworkServer.RegisterHandler<InventoryScrollMessage>(OnScrollMessage);
            NetworkServer.RegisterHandler<ThrowSelectedMessage>(OnThrowMessage);
            NetworkServer.RegisterHandler<UseSelectedMessage>(OnUseSelectedMessage);
            _handlersRegistered = true;
        }

        public void StopServer()
        {
            if (!_handlersRegistered) return;

            NetworkServer.UnregisterHandler<InventorySelectSlotMessage>();
            NetworkServer.UnregisterHandler<InventoryScrollMessage>();
            NetworkServer.UnregisterHandler<ThrowSelectedMessage>();
            NetworkServer.UnregisterHandler<UseSelectedMessage>();
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

        public void RequestUseSelected()
        {
            if (_offline)
            {
                _useSelected.Invoke(OfflineConnectionId);
                return;
            }
            
            
            if(!NetworkClient.isConnected) return;
            
            NetworkClient.Send(new UseSelectedMessage());
        }
        
        public void NotifyHeldItemChanged(int connectionId, int itemId)
        {
            if (_offline)
            {
                if (connectionId == OfflineConnectionId) OfflineHeldItemChanged?.Invoke(itemId);
                return;
            }
 
            // Online so o servidor escreve o SyncVar (clientes puros nao tem inventario)
            if (!TryGetHeldTarget(connectionId, out var target)) return;
 
            target.ServerSetHeldItem(itemId);
        }
 
        public void NotifyHeldItemActiveChanged(int connectionId, bool active)
        {
            if (_offline)
            {
                if (connectionId == OfflineConnectionId) OfflineHeldItemActiveChanged?.Invoke(active);
                return;
            }
 
            if (!TryGetHeldTarget(connectionId, out var target)) return;
 
            target.ServerSetHeldItemActive(active);
        }

        private static bool TryGetHeldTarget(int connectionId, out INetworkHeldItemTarget target)
        {
            target = null;
            
            if(!NetworkServer.active) return false;
            if(!NetworkServer.connections.TryGetValue(connectionId, out var connection)) return false;
            if(connection.identity == null) return false;
            
            target = connection.identity.GetComponent<INetworkHeldItemTarget>();
            return target != null;
        }

        public void RequestThrow(Vector3 origin, Vector3 aimPoint)
        {
            if (_offline)
            {
                _throw?.Invoke(OfflineConnectionId, origin, aimPoint);
                return;
            }
            
            if(!NetworkClient.isConnected) return;
            
            NetworkClient.Send(new ThrowSelectedMessage { Origin = origin, AimPoint = aimPoint });
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

        private void OnThrowMessage(NetworkConnectionToClient connection, ThrowSelectedMessage message)
        {
            if (connection == null || connection.identity == null) return;
            
            _throw?.Invoke(connection.connectionId, message.Origin, message.AimPoint);
        }

        private void OnUseSelectedMessage(NetworkConnectionToClient connection, UseSelectedMessage message)
        {
            if (connection == null || connection.identity == null) return;

            try
            {
                _useSelected?.Invoke(connection.connectionId);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
