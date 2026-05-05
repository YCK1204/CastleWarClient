using System;
using Google.FlatBuffers;
using Network;

public partial class PacketHandler
{
    public static void SC_PINGHandler(PacketSession session, SC_Ping data)
    {
        try
        {
            var builder = new FlatBufferBuilder(64);
            var pongOffset = CS_Pong.CreateCS_Pong(builder, data.Timestamp);
            var s = NetworkManager.Instance.Session;
            var packet = s != null && s.IsAesInit
                ? PacketManager.Instance.CreatePacketWithAes(pongOffset, builder, CW_PKT_HeartBeat.CS_PONG)
                : PacketManager.Instance.CreatePacket(pongOffset, builder, CW_PKT_HeartBeat.CS_PONG);

            if (packet != null)
                NetworkManager.Instance.Send(packet);
        }
        catch (Exception e)
        {
        }
    }
}