using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Core;

namespace Network
{
    // 서버와의 연결 및 패킷 통신을 담당
    public class NetworkManager : MonoSingleton<NetworkManager>
    {
        private Connector _connector = new Connector();
        private ConcurrentQueue<ArraySegment<byte>> _queue = new ConcurrentQueue<ArraySegment<byte>>();
        private ServerSession _session;
        private Queue<ArraySegment<byte>> _processingQueue = new Queue<ArraySegment<byte>>();

        protected override void Init()
        {
            _connector.Connect(
                (socket) =>
                {
                    _session = new ServerSession(socket);
                    ProcessPacket();
                    return _session;
                },
                IPAddress.Loopback,
                8080);
        }

        public void Send(ArraySegment<byte> data)
        {
            _session?.Send(data);
        }

        // 소켓 스레드에서 호출 → thread-safe
        public void EnqueuePacket(ArraySegment<byte> data)
        {
            _queue.Enqueue(data);
        }

        private async Awaitable ProcessPacket()
        {
            while (_session != null && _session.Disconnected == false)
            {
                while (_processingQueue.Count > 0)
                {
                    var packet = _processingQueue.Dequeue();
                    PacketManager.Instance.OnRecvPacket(_session, packet);
                }

                await Awaitable.NextFrameAsync();
            }
        }

        private void Update()
        {
            while (_queue.TryDequeue(out var packet))
            {
                _processingQueue.Enqueue(packet);
            }
        }
    }
}