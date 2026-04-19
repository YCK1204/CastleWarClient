using Google.FlatBuffers;
using Network;

public partial class PacketHandler
{
    public static void SC_PINGHandler(PacketSession session, byte[] buffer)
    {
        var ping = CW_SC_Ping.GetRootAsCW_SC_Ping(new ByteBuffer(buffer));

        var builder = new FlatBufferBuilder(64);
        var pongOffset = CW_CS_Pong.CreateCW_CS_Pong(builder, ping.Timestamp);
        var packet = PacketManager.Instance.CreatePacket(pongOffset, builder, CW_PKT_HeartBeat.CS_PONG);

        if (packet != null)
            NetworkManager.Instance.Send(packet);
    }
}
