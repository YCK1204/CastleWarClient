using System;
using GameLogic.Input;
using GameLogic.Interfaces;
using Google.FlatBuffers;
using Network;
using UnityEngine;

namespace GameLogic.FieldObjects
{
    public class Castle : FieldObject, ITouchable
    {
        private Action _touchDown;
        private Action _touchUp;
        private Action<Vector2> _touchDrag;

        // 소환할 유닛 타입 ID (Inspector에서 설정)
        [SerializeField] private ushort _unitTypeId = 0;
        // 소환할 유닛 수 (코스트 = count * RequiredCost)
        [SerializeField] private ushort _spawnCount = 1;

        #region TouchHandle
        private void Awake()
        {
            ((ITouchable)this).OnTouchDown = OnTouched;
        }

        private void Start()
        {
            TouchObjectManager.Instance.RegisterTouchObject(this);
        }

        private void OnDisable()
        {
            TouchObjectManager.Instance?.UnregisterTouchObject(this);
        }

        Action ITouchable.OnTouchDown
        {
            get => _touchDown;
            set => _touchDown = value;
        }

        Action ITouchable.OnTouchUp
        {
            get => _touchUp;
            set => _touchUp = value;
        }

        Action<Vector2> ITouchable.OnTouchDrag
        {
            get => _touchDrag;
            set => _touchDrag = value;
        }
        #endregion

        public bool IsActive { get; set; }
        public float CurrentCost { get; set; }
        public float MaxCost { get; set; }
        public int Level { get; set; }

        public override void TakeDamage(float damage)
        {
            Hp -= damage;
        }

        private void OnTouched()
        {
            Debug.Log("[Castle] 터치됨");
            SendUnitSpawn(_unitTypeId, _spawnCount);
        }

        public void SendUnitSpawn(ushort unitTypeId, ushort count)
        {
            var session = NetworkManager.Instance.Session;
            Debug.Log($"[Castle] SendUnitSpawn - Session: {(session == null ? "null" : "있음")}, AesInit: {session?.IsAesInit}");

            var builder = new FlatBufferBuilder(64);
            var wayPoints = CS_UnitSpawn.CreateWayPointsVector(builder, Array.Empty<ushort>());
            var offset = CS_UnitSpawn.CreateCS_UnitSpawn(builder,
                unit_id: unitTypeId,
                count: count,
                way_pointsOffset: wayPoints,
                start_point: NetworkId);

            var packet = PacketManager.Instance.CreatePacketWithAes(offset, builder, CW_PKT_InGame.CS_UNIT_SPAWN);
            Debug.Log($"[Castle] 패킷: {(packet != null ? packet.Length + " bytes" : "null")}");
            if (packet != null)
                NetworkManager.Instance.Send(packet);
        }
    }
}