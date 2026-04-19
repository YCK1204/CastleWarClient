using UnityEngine;

public partial class PacketHandler
{
    public static void SC_GAME_STARTHandler(PacketSession session, byte[] buffer)
    {
        Debug.Log("Game Start!!");
    }
    public static void SC_DRAW_REQUESTHandler(PacketSession session, byte[] buffer)
    {
        
    }
    public static void SC_DRAW_REQUEST_REJECTIONHandler(PacketSession session, byte[] buffer)
    {
        
    }
    
    public static void SC_GAME_RESULTHandler(PacketSession session, byte[] buffer)
    {
        
    }
}