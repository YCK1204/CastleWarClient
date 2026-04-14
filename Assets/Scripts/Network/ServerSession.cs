using System;
using System.Net;
using System.Net.Sockets;

namespace Network
{
    public class ServerSession : PacketSession
    {
        public ServerSession(Socket socket) : base(socket)
        {
        }

        protected override void OnConnected(EndPoint endpoint)
        {
        }

        protected override void OnDisConnected(EndPoint endpoint)
        {
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