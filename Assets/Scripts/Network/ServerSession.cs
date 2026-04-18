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
            // RecvBuffer는 재사용되므로 큐에 넣기 전에 반드시 복사해야 함.
            // 복사 없이 ArraySegment를 저장하면 Clean() 또는 다음 수신 시
            // 버퍼가 덮어써져 메인 스레드에서 쓰레기 데이터를 읽게 됨.
            byte[] copy = new byte[data.Count];
            Buffer.BlockCopy(data.Array, data.Offset, copy, 0, data.Count);
            NetworkManager.Instance.EnqueuePacket(new ArraySegment<byte>(copy));
        }
    }
}