using GameLogic;
using GameLogic.Player;
using UnityEngine;
using UnityEngine.AddressableAssets;

public partial class PacketHandler
{
    public static void SC_UNIT_SPAWNEDHandler(PacketSession session, SC_UnitSpawned data)
    {
        ushort networkId = data.Id;
        ulong ownerId = data.OwnerId;

        Addressables.LoadAssetAsync<GameObject>("WarriorUnit").Completed += handle =>
        {
            if (handle.Result == null)
            {
                Debug.LogError("[SC_UNIT_SPAWNED] WarriorUnit 프리팹 로드 실패");
                return;
            }

            var go = Object.Instantiate(handle.Result, Vector3.zero, Quaternion.identity);
            var unit = go.GetComponent<GameLogic.Unit.BaseUnitController>();
            if (unit == null)
            {
                Debug.LogError("[SC_UNIT_SPAWNED] BaseUnitController 컴포넌트 없음");
                return;
            }

            unit.NetworkId = networkId;
            unit.Owner = PlayerManager.Instance.GetPlayer(ownerId);

            UnitManager.Instance.Register(networkId, unit);
            FieldObjectRegistry.Instance.Register(networkId, unit);
        };
    }

    public static void SC_UNIT_POSITIONHandler(PacketSession session, SC_UnitPosition data)
    {
        var unit = UnitManager.Instance.Get(data.Id);
        if (unit == null) return;

        unit.PathId = data.PathId;
        unit.Progress = data.Progress;
        unit.Move();
    }

    public static void SC_UNIT_ATTACKHandler(PacketSession session, SC_UnitAttack data)
    {
        var target = FieldObjectRegistry.Instance.Get(data.TargetId);
        if (target == null) return;

        target.TakeDamage(target.MaxHp - data.NewHp);
    }

    public static void SC_UNIT_DIEDHandler(PacketSession session, SC_UnitDied data)
    {
        var unit = UnitManager.Instance.Get(data.Id);
        if (unit == null) return;

        UnitManager.Instance.Unregister(data.Id);
        FieldObjectRegistry.Instance.Unregister(data.Id);
        Object.Destroy(unit.gameObject);
    }
}