using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Map
{
    [Serializable]
    public class CastlePoint
    {
        public ushort id;
        public float x;
        public float y;
    }

    [Serializable]
    public class PathData
    {
        public ushort id;
        public ushort fromCastleId;
        public ushort toCastleId;
    }

    [Serializable]
    public class MapData
    {
        public List<CastlePoint> castles;
        public List<PathData> paths;
    }
}