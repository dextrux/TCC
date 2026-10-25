using Mirror;
using UnityEngine;

namespace Netcode.EOS_Scripts.Messages
{
    public struct ThrowSelectedMessage : NetworkMessage
    {
        public Vector3 Origin;
        public Vector3 AimPoint;
    }
}