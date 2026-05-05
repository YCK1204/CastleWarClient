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
        /// <summary>0=팀0 시작 성, 1=팀1 시작 성, -1=중립(초기 비어있음)</summary>
        public int defaultTeam;
    }

    [Serializable]
    public class PathData
    {
        public ushort id;
        public ushort castleAId;
        public ushort castleBId;
    }

    [Serializable]
    public class MapData
    {
        public List<CastlePoint> castles;
        public List<PathData> paths;
    }
}