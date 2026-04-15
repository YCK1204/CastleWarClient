using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Network
{
    public class ServerSession : PacketSession
    {
        public ServerSession(Socket socket) : base(socket)
        {
        }

        public override void OnConnected(EndPoint endpoint)
        {
            Debug.Log("OnConnected");
            StartReceive();
        }

        protected override void OnDisConnected(EndPoint endpoint)
        {
            Debug.Log("OnDisConnected");
        }

        protected override void OnSend(int numOfBytes)
        {
        }

        protected override void OnRecvPacket(ArraySegment<byte> data)
        {
            NetworkManager.Instance.EnqueuePacket(data);
        }
    }
}