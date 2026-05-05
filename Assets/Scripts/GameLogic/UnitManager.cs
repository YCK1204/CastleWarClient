using System.Collections.Generic;
using GameLogic.Unit;
using UnityEngine;

namespace GameLogic
{
    public class UnitManager : MonoBehaviour
    {
        public static UnitManager Instance { get; private set; }

        private readonly Dictionary<ushort, BaseUnitController> _units = new();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Register(ushort networkId, BaseUnitController unit)
        {
            _units[networkId] = unit;
        }

        public void Unregister(ushort networkId)
        {
            _units.Remove(networkId);
        }

        public BaseUnitController Get(ushort networkId)
        {
            _units.TryGetValue(networkId, out var unit);
            return unit;
        }
    }
}