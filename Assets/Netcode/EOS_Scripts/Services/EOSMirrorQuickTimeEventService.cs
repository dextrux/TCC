using System;
using CoreDomain.Scripts.Services.NetworkServices;
using Mirror;
using Netcode.EOS_Scripts.Messages;

namespace Netcode.EOS_Scripts.Services
{
    public class EOSMirrorQuickTimeEventService : INetworkQuickTimeEventService
    {
        private const int OfflineConnectionId = 0;

        private Action<int> _startAction;
        private Action<int, int> _inputAction;

        private bool _offline;
        private bool _serverRegistered;
        private bool _clientRegistered;

        public event Action<string, int, int> QTEUpdated;

        public void Configure(bool offline, Action<int> startAction,
            Action<int, int> inputAction)
        {
            _offline = offline;
            _startAction = startAction;
            _inputAction = inputAction;
        }

        public void SetUp()
        {
            if (NetworkServer.active && !_serverRegistered)
            {
                NetworkServer.RegisterHandler<QTEStartMessage>(OnStartMessage);
                NetworkServer.RegisterHandler<QTEInputMessage>(OnInputMessage);

                _serverRegistered = true;
            }

            if (NetworkClient.active && !_clientRegistered)
            {
                NetworkClient.RegisterHandler<QTEUpdateMessage>(OnUpdateMessage);

                _clientRegistered = true;
            }
        }

        public void StopServer()
        {
            if (!_serverRegistered) return;

            NetworkServer.UnregisterHandler<QTEStartMessage>();
            NetworkServer.UnregisterHandler<QTEInputMessage>();

            _serverRegistered = false;
        }

        public void StopClient()
        {
            if (!_clientRegistered) return;

            NetworkClient.UnregisterHandler<QTEUpdateMessage>();

            _clientRegistered = false;
        }

        public void RequestStart()
        {
            if (_offline)
            {
                _startAction?.Invoke(OfflineConnectionId);
                return;
            }

            if (!NetworkClient.isConnected) return;

            NetworkClient.Send(new QTEStartMessage());
        }

        public void RequestInput(int key)
        {
            if (_offline)
            {
                _inputAction?.Invoke(OfflineConnectionId, key);
                return;
            }

            if (!NetworkClient.isConnected) return;

            NetworkClient.Send(new QTEInputMessage
            {
                Key = key
            });
        }

        public void SendUpdate(int connectionId, string letter, int step, int result)
        {
            if (_offline)
            {
                QTEUpdated?.Invoke(letter, step, result);
                return;
            }

            if (!NetworkServer.active) return;

            if (!NetworkServer.connections.TryGetValue(connectionId, out var connection))
                return;

            connection.Send(new QTEUpdateMessage
            {
                Letter = letter,
                Step = step,
                Result = result
            });
        }

        private void OnStartMessage(NetworkConnectionToClient connection,
            QTEStartMessage message)
        {
            if (connection?.identity == null) return;

            _startAction?.Invoke(connection.connectionId);
        }

        private void OnInputMessage(NetworkConnectionToClient connection,
            QTEInputMessage message)
        {
            if (connection?.identity == null) return;

            _inputAction?.Invoke(connection.connectionId, message.Key);
        }

        private void OnUpdateMessage(QTEUpdateMessage message)
        {
            QTEUpdated?.Invoke(message.Letter, message.Step, message.Result);
        }
    }
}