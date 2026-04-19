
using System.Collections.Generic;
using System;
using System.Buffers;
using CWFramework;
using Google.FlatBuffers;
using K4os.Compression.LZ4;
using Network;
using UnityEngine;


public class PacketManager : Singleton<PacketManager>
{
    private Dictionary<ushort, Action<ServerSession, byte[]>> _packetHandlers =
        new Dictionary<ushort, Action<ServerSession, byte[]>>();

    private const ushort PacketHeaderSize = PacketConstants.HeaderSize;
    private const int CompressionThreshold = 512;
    private const byte FlagCompressed = 0x01;

    public PacketManager()
    {
        
        _packetHandlers.Add((ushort)CW_PKT_HeartBeat.SC_PING, PacketHandler.SC_PINGHandler);
        _packetHandlers.Add((ushort)CW_PKT_PreGame.SC_GAME_START, PacketHandler.SC_GAME_STARTHandler);
        _packetHandlers.Add((ushort)CW_PKT_InGame.SC_UNIT_SPAWNED, PacketHandler.SC_UNIT_SPAWNEDHandler);
        _packetHandlers.Add((ushort)CW_PKT_InGame.SC_LAST_CASTLE_SWITCHED, PacketHandler.SC_LAST_CASTLE_SWITCHEDHandler);
        _packetHandlers.Add((ushort)CW_PKT_InGame.SC_UNIT_POSITION, PacketHandler.SC_UNIT_POSITIONHandler);
        _packetHandlers.Add((ushort)CW_PKT_InGame.SC_UNIT_ATTACK, PacketHandler.SC_UNIT_ATTACKHandler);
        _packetHandlers.Add((ushort)CW_PKT_InGame.SC_CASTLE_ACATIVATED, PacketHandler.SC_CASTLE_ACATIVATEDHandler);
        _packetHandlers.Add((ushort)CW_PKT_InGame.SC_CASTLE_UPDATE, PacketHandler.SC_CASTLE_UPDATEHandler);
        _packetHandlers.Add((ushort)CW_PKT_InGame.SC_CASTLE_UPGRADE, PacketHandler.SC_CASTLE_UPGRADEHandler);
        _packetHandlers.Add((ushort)CW_PKT_GameInfo.SC_DRAW_REQUEST, PacketHandler.SC_DRAW_REQUESTHandler);
        _packetHandlers.Add((ushort)CW_PKT_GameInfo.SC_DRAW_REQUEST_REJECTION, PacketHandler.SC_DRAW_REQUEST_REJECTIONHandler);
        _packetHandlers.Add((ushort)CW_PKT_GameInfo.SC_GAME_RESULT, PacketHandler.SC_GAME_RESULTHandler);
        _packetHandlers.Add((ushort)CW_PKT_Security.SC_RSA_PUB_KEY, PacketHandler.SC_RSA_PUB_KEYHandler);
        _packetHandlers.Add((ushort)CW_PKT_Auth.SC_LOGIN, PacketHandler.SC_LOGINHandler);
    }

    public void OnRecvPacket(ServerSession session, ArraySegment<byte> data)
    {
        byte[]? buffer = null;
        bool usedArrayPool = false;

        try
        {
            ushort count = 0;
            ushort packetSize = BitConverter.ToUInt16(data.Array, data.Offset + count);
            count += sizeof(ushort);
            ushort packetId = BitConverter.ToUInt16(data.Array, data.Offset + count);
            count += sizeof(ushort);
            byte flags = data.Array[data.Offset + count];
            bool compressed = (flags & FlagCompressed) != 0;

            int bodyLen = packetSize - PacketHeaderSize;
            if (bodyLen < 0 || bodyLen > ushort.MaxValue)
            {
                session.Disconnect();
                return;
            }

            // body 추출
            byte[] bodyBytes;
            int validBodyLen;
            bool bodyFromPool = false;
            if (compressed)
            {
                bodyBytes = LZ4Pickler.Unpickle(data.Array, data.Offset + PacketHeaderSize, bodyLen);
                validBodyLen = bodyBytes.Length;
            }
            else
            {
                bodyBytes = ArrayPool<byte>.Shared.Rent(bodyLen);
                Array.Copy(data.Array, data.Offset + PacketHeaderSize, bodyBytes, 0, bodyLen);
                bodyFromPool = true;
                validBodyLen = bodyLen;
            }

            // AES 복호화 (키 교환 완료 이후 패킷)
            if (session.IsAesInit)
            {
                buffer = SecurityManager.Instance.AesDecrypt(bodyBytes, validBodyLen, session);
                if (bodyFromPool) ArrayPool<byte>.Shared.Return(bodyBytes);
                if (buffer == null)
                {
                    session.Disconnect();
                    return;
                }
            }
            else
            {
                buffer = bodyBytes;
                usedArrayPool = bodyFromPool;
            }

            if (_packetHandlers.TryGetValue(packetId, out var handler))
            {
                handler.Invoke(session, buffer);
            }
            else
            {
                
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
        finally
        {
            if (buffer != null && usedArrayPool)
                ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public byte[]? CreatePacket<T>(Offset<T> data, FlatBufferBuilder builder, Enum id, bool useRsa = false) where T : struct
    {
        builder.Finish(data.Value);
        var bytes = builder.SizedByteArray();
        if (useRsa)
            bytes = SecurityManager.Instance.RsaEncrypt(bytes);

        return Serialize(bytes, id);
    }

    
    public byte[]? CreatePacketWithAes<T>(Offset<T> data, FlatBufferBuilder builder, Enum id) where T : struct
    {
        var session = NetworkManager.Instance.Session;
        builder.Finish(data.Value);
        var bytes = builder.SizedByteArray();
        var encryptedData = SecurityManager.Instance.AesEncrypt(bytes, session);

        if (encryptedData == null)
        {
            session.Disconnect();
            return null;
        }

        return Serialize(encryptedData, id);
    }

    public byte[] Serialize(byte[] data, Enum id)
    {
        bool compress = data.Length >= CompressionThreshold;
        byte[] body = compress ? LZ4Pickler.Pickle(data) : data;
        byte flags = compress ? FlagCompressed : (byte)0;

        ushort totalSize = (ushort)(body.Length + PacketHeaderSize);
        byte[] packet = new byte[totalSize];
        int pos = 0;

        BitConverter.TryWriteBytes(new Span<byte>(packet, pos, sizeof(ushort)), totalSize);
        pos += sizeof(ushort);
        BitConverter.TryWriteBytes(new Span<byte>(packet, pos, sizeof(ushort)), Convert.ToUInt16(id));
        pos += sizeof(ushort);
        packet[pos] = flags;
        pos += 1;
        Buffer.BlockCopy(body, 0, packet, pos, body.Length);

        return packet;
    }
}
