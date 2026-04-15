using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;

public abstract class Session
{
    private Socket _socket;
    public ulong Id;
    private RecvBuffer _recvBuffer = new RecvBuffer(ushort.MaxValue);
    private SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();
    private SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
    private int _disconnected = 0;
    private List<ArraySegment<byte>> _sendList = new List<ArraySegment<byte>>();
    private ConcurrentQueue<ArraySegment<byte>> _sendQueue = new ConcurrentQueue<ArraySegment<byte>>();
    private int _sending = 0;
    public bool Disconnected => Volatile.Read(ref _disconnected) == 1;

    public abstract void OnConnected(EndPoint endpoint);
    protected abstract void OnDisConnected(EndPoint endpoint);
    protected abstract int OnRecv(ArraySegment<byte> data);
    protected abstract void OnSend(int numOfBytes);

    public Session(Socket socket)
    {
        _socket = socket;
        _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
        _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
    }

    #region Receive

    public void StartReceive()
    {
        RegisterRecv(_recvArgs);
    }

    private void RegisterRecv(SocketAsyncEventArgs e)
    {
        if (Disconnected)
            return;
        try
        {
            _recvBuffer.Clean();
            var writeSegment = _recvBuffer.WriteSegment;
            _recvArgs.SetBuffer(writeSegment.Array, writeSegment.Offset, writeSegment.Count);
            bool pending = _socket.ReceiveAsync(_recvArgs);
            if (!pending)
                OnRecvCompleted(null, _recvArgs);
        }
        catch (Exception ex)
        {
        }
    }

    private void OnRecvCompleted(object? sender, SocketAsyncEventArgs e)
    {
        try
        {
            if (e.SocketError == SocketError.Success)
            {
                if (e.BytesTransferred == 0)
                {
                    Disconnect();
                    return;
                }

                if (_recvBuffer.OnWrite(e.BytesTransferred) == false)
                {
                    Disconnect();
                    return;
                }

                int processLen = OnRecv(_recvBuffer.ReadSegment);
                if (_recvBuffer.ReadSize < processLen)
                {
                    Disconnect();
                    return;
                }

                if (_recvBuffer.OnRead(processLen) == false)
                {
                    Disconnect();
                    return;
                }

                RegisterRecv(e);
            }
            else
            {
                Disconnect();
            }
        }
        catch (Exception ex)
        {
            Disconnect();
        }
    }

    #endregion

    #region Send

    public void Send(ArraySegment<byte> data)
    {
        if (Disconnected)
            return;
        try
        {
            _sendQueue.Enqueue(data);
            RegisterSend(_sendArgs);
        }
        catch (Exception ex)
        {
            Disconnect();
        }
    }

    private void RegisterSend(SocketAsyncEventArgs e)
    {
        if (Interlocked.CompareExchange(ref _sending, 1, 0) != 0)
            return;

        e.BufferList = null;
        _sendList.Clear();
        while (_sendQueue.TryDequeue(out var data))
        {
            _sendList.Add(data);
        }

        if (_sendList.Count == 0)
        {
            Interlocked.Exchange(ref _sending, 0);
            // 락 해제와 큐 확인 사이에 들어온 항목 처리
            if (!_sendQueue.IsEmpty)
                RegisterSend(e);
            return;
        }

        e.BufferList = _sendList;
        bool pending = _socket.SendAsync(e);
        if (!pending)
            OnSendCompleted(null, _sendArgs);
    }

    private void OnSendCompleted(object? sender, SocketAsyncEventArgs e)
    {
        try
        {
            if (e.SocketError == SocketError.Success)
            {
                OnSend(e.BytesTransferred);

                Interlocked.Exchange(ref _sending, 0);
                RegisterSend(e);
            }
            else if (e.SocketError == SocketError.OperationAborted)
            {
            }
            else
            {
                Disconnect();
            }
        }
        catch (Exception ex)
        {
            Disconnect();
        }
    }

    #endregion

    public void Disconnect()
    {
        if (Interlocked.CompareExchange(ref _disconnected, 1, 0) == 1)
            return;
        var remoteEndPoint = _socket.RemoteEndPoint;
        try { _socket.Shutdown(SocketShutdown.Both); } catch { }
        _socket.Close();
        _recvArgs.Dispose();
        _sendArgs.Dispose();
        OnDisConnected(remoteEndPoint);
    }
}

public abstract class PacketSession : Session
{
    #region 통신

    private const ushort HeaderSize = Network.PacketConstants.HeaderSize;

    protected PacketSession(Socket socket) : base(socket)
    {
        _isAesInit = false;
    }

    protected abstract void OnRecvPacket(ArraySegment<byte> data);

    protected sealed override int OnRecv(ArraySegment<byte> data)
    {
        int processLen = 0;

        while (data.Count >= HeaderSize)
        {
            ushort headerSize = BitConverter.ToUInt16(data.Array, data.Offset);
            if (data.Count < headerSize)
                break;
            OnRecvPacket(new ArraySegment<byte>(data.Array, data.Offset, headerSize));
            processLen += headerSize;
            data = new ArraySegment<byte>(data.Array, data.Offset + headerSize, data.Count - headerSize);
        }

        return processLen;
    }

    #endregion

    private volatile bool _isAesInit = false;
    private byte[] _aesKey;
    public bool IsAesInit => _isAesInit;
    public byte[] AesKey => _isAesInit ? _aesKey : null;

    public void SetAes(byte[] iv, byte[] key)
    {
        try
        {
            _aesKey = key;
            _isAesInit = true;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[세션] AES 키 설정 실패 - {ex.Message}");
        }
    }
}