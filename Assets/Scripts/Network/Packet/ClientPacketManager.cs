
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
        
        Register<SC_Ping>((ushort)CW_PKT_HeartBeat.SC_PING, SC_Ping.GetRootAsSC_Ping, PacketHandler.SC_PINGHandler);
        Register<SC_GameStart>((ushort)CW_PKT_PreGame.SC_GAME_START, SC_GameStart.GetRootAsSC_GameStart, PacketHandler.SC_GAME_STARTHandler);
        Register<SC_UnitSpawned>((ushort)CW_PKT_InGame.SC_UNIT_SPAWNED, SC_UnitSpawned.GetRootAsSC_UnitSpawned, PacketHandler.SC_UNIT_SPAWNEDHandler);
        Register<SC_UnitPosition>((ushort)CW_PKT_InGame.SC_UNIT_POSITION, SC_UnitPosition.GetRootAsSC_UnitPosition, PacketHandler.SC_UNIT_POSITIONHandler);
        Register<SC_UnitAttack>((ushort)CW_PKT_InGame.SC_UNIT_ATTACK, SC_UnitAttack.GetRootAsSC_UnitAttack, PacketHandler.SC_UNIT_ATTACKHandler);
        Register<SC_UnitDied>((ushort)CW_PKT_InGame.SC_UNIT_DIED, SC_UnitDied.GetRootAsSC_UnitDied, PacketHandler.SC_UNIT_DIEDHandler);
        Register<SC_LastCastleSwitched>((ushort)CW_PKT_InGame.SC_LAST_CASTLE_SWITCHED, SC_LastCastleSwitched.GetRootAsSC_LastCastleSwitched, PacketHandler.SC_LAST_CASTLE_SWITCHEDHandler);
        Register<SC_CastleActivated>((ushort)CW_PKT_InGame.SC_CASTLE_ACTIVATED, SC_CastleActivated.GetRootAsSC_CastleActivated, PacketHandler.SC_CASTLE_ACTIVATEDHandler);
        Register<SC_CastleUpdate>((ushort)CW_PKT_InGame.SC_CASTLE_UPDATE, SC_CastleUpdate.GetRootAsSC_CastleUpdate, PacketHandler.SC_CASTLE_UPDATEHandler);
        Register<SC_CastleUpgraded>((ushort)CW_PKT_InGame.SC_CASTLE_UPGRADED, SC_CastleUpgraded.GetRootAsSC_CastleUpgraded, PacketHandler.SC_CASTLE_UPGRADEDHandler);
        Register<SC_CastleDeactivated>((ushort)CW_PKT_InGame.SC_CASTLE_DEACTIVATED, SC_CastleDeactivated.GetRootAsSC_CastleDeactivated, PacketHandler.SC_CASTLE_DEACTIVATEDHandler);
        Register<SC_SkillUsed>((ushort)CW_PKT_InGame.SC_SKILL_USED, SC_SkillUsed.GetRootAsSC_SkillUsed, PacketHandler.SC_SKILL_USEDHandler);
        Register<SC_DrawOffered>((ushort)CW_PKT_GameResult.SC_DRAW_OFFERED, SC_DrawOffered.GetRootAsSC_DrawOffered, PacketHandler.SC_DRAW_OFFEREDHandler);
        Register<SC_DrawRejected>((ushort)CW_PKT_GameResult.SC_DRAW_REJECTED, SC_DrawRejected.GetRootAsSC_DrawRejected, PacketHandler.SC_DRAW_REJECTEDHandler);
        Register<SC_GameResult>((ushort)CW_PKT_GameResult.SC_GAME_RESULT, SC_GameResult.GetRootAsSC_GameResult, PacketHandler.SC_GAME_RESULTHandler);
        Register<SC_RsaPubKey>((ushort)CW_PKT_Security.SC_RSA_PUB_KEY, SC_RsaPubKey.GetRootAsSC_RsaPubKey, PacketHandler.SC_RSA_PUB_KEYHandler);
        Register<SC_Login>((ushort)CW_PKT_Auth.SC_LOGIN, SC_Login.GetRootAsSC_Login, PacketHandler.SC_LOGINHandler);
    }

    /// <summary>FlatBuffers 역직렬화를 핸들러 호출 전에 수행하는 래퍼를 등록합니다.</summary>
    private void Register<T>(ushort id, Func<ByteBuffer, T> deserializer, Action<ServerSession, T> handler)
        where T : struct, IFlatbufferObject
    {
        _packetHandlers[id] = (session, buffer) =>
        {
            var packet = deserializer(new ByteBuffer(buffer));
            handler(session, packet);
        };
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
