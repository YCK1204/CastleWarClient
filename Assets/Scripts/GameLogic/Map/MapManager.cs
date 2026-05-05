using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Map
{
    public class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; }

        private readonly Dictionary<ushort, Vector2> _castlePositions = new();
        private readonly Dictionary<ushort, PathData> _paths = new();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void LoadFromJson(int mapId)
        {
            var textAsset = Resources.Load<TextAsset>($"Maps/map_{mapId}");
            if (textAsset == null)
            {
                Debug.LogError($"[MapManager] map_{mapId}.json 을 찾을 수 없음");
                return;
            }

            var data = JsonUtility.FromJson<MapData>(textAsset.text);
            Load(data);
        }

        public void Load(MapData data)
        {
            _castlePositions.Clear();
            _paths.Clear();

            foreach (var castle in data.castles)
                _castlePositions[castle.id] = new Vector2(castle.x, castle.y);

            foreach (var path in data.paths)
                _paths[path.id] = path;
        }

        // PathId + Progress → 월드 좌표
        public Vector2 GetWorldPosition(ushort pathId, float progress)
        {
            if (!_paths.TryGetValue(pathId, out var path)) return Vector2.zero;
            if (!_castlePositions.TryGetValue(path.fromCastleId, out var from)) return Vector2.zero;
            if (!_castlePositions.TryGetValue(path.toCastleId, out var to)) return Vector2.zero;

            return Vector2.Lerp(from, to, progress);
        }
    }
}