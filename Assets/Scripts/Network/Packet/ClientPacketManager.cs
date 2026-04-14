
using System.Collections.Generic;
using System;
using System.Buffers;
using CWFramework;
using Google.FlatBuffers;
using K4os.Compression.LZ4;
using Network;

public class PacketManager : Singleton<PacketManager>
{
    private Dictionary<ushort, Action<ServerSession, byte[]>> _packetHandlers =
        new Dictionary<ushort, Action<ServerSession, byte[]>>();

    private const ushort PacketHeaderSize = PacketConstants.HeaderSize;
    private const int CompressionThreshold = 512;
    private const byte FlagCompressed = 0x01;

    public PacketManager()
    {
        
        _packetHandlers.Add((ushort)CW_PKT_Type.SC_RSA_PUB_KEY, PacketHandler.SC_RSA_PUB_KEYHandler);
        _packetHandlers.Add((ushort)CW_PKT_Type.SC_PING, PacketHandler.SC_PINGHandler);
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

            if (_packetHandlers.TryGetValue(packetId, out var handler))
            {
                if (compressed)
                {
                    buffer = LZ4Pickler.Unpickle(data.Array, data.Offset + PacketHeaderSize, bodyLen);
                    usedArrayPool = false;
                }
                else
                {
                    buffer = ArrayPool<byte>.Shared.Rent(bodyLen);
                    Array.Copy(data.Array, data.Offset + PacketHeaderSize, buffer, 0, bodyLen);
                    usedArrayPool = true;
                }
                handler.Invoke(session, buffer);
            }
            else
            {
                
            }
        }
        catch (Exception ex)
        {
            
        }
        finally
        {
            if (buffer != null && usedArrayPool)
                ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public byte[]? CreatePacket<T>(Offset<T> data, FlatBufferBuilder builder, CW_PKT_Type id, bool useRsa = false) where T : struct
    {
        builder.Finish(data.Value);
        var bytes = builder.SizedByteArray();
        if (useRsa)
            bytes = SecurityManager.Instance.RsaEncrypt(bytes);
        
        return Serialize(bytes, id);
    }

    public byte[]? CreatePacketWithAes<T>(Offset<T> data, FlatBufferBuilder builder, CW_PKT_Type id, ServerSession session) where T : struct
    {
        builder.Finish(data.Value);
        var bytes = builder.SizedByteArray();
        var encrpytedData = SecurityManager.Instance.AesEncrypt(bytes, session);

        if (encrpytedData == null)
        {
            session.Disconnect();
            return null;
        }
        
        return Serialize(encrpytedData, id);
    }
    public byte[] Serialize(byte[] data, CW_PKT_Type id)
    {
        bool compress = data.Length >= CompressionThreshold;
        byte[] body = compress ? LZ4Pickler.Pickle(data) : data;
        byte flags = compress ? FlagCompressed : (byte)0;

        ushort totalSize = (ushort)(body.Length + PacketHeaderSize);
        byte[] packet = new byte[totalSize];
        int pos = 0;

        BitConverter.TryWriteBytes(new Span<byte>(packet, pos, sizeof(ushort)), totalSize);
        pos += sizeof(ushort);
        BitConverter.TryWriteBytes(new Span<byte>(packet, pos, sizeof(ushort)), (ushort)id);
        pos += sizeof(ushort);
        packet[pos] = flags;
        pos += 1;
        Buffer.BlockCopy(body, 0, packet, pos, body.Length);

        return packet;
    }
}
