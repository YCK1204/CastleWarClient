using System;
using Google.FlatBuffers;
using Network;
using UnityEngine;
using UnityEngine.UI;

public class TestButton : MonoBehaviour
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(ReadyToStartGame);
    }

    private void ReadyToStartGame()
    {
        FlatBufferBuilder builder = new FlatBufferBuilder(1);
        
        CS_GameReady.StartCS_GameReady(builder);
        Offset<CS_GameReady> offset = CS_GameReady.EndCS_GameReady(builder);
        
        var pkt = PacketManager.Instance.CreatePacketWithAes(offset, builder, CW_PKT_PreGame.CS_GAME_READY);
        NetworkManager.Instance.Send(pkt);
    }
}