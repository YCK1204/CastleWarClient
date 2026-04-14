using System;

namespace GameLogic.Interfaces
{
    public interface IMoveable
    {
        /// <summary>맵 JSON paths 배열에서 from/to로 정의된 경로 id</summary>
        ushort PathId { get; set; }

        /// <summary>현재 경로에서의 진행도 (0.0 ~ 1.0)</summary>
        float Progress { get; set; }

        float Speed { get; set; }
        Action OnMove { get; set; }

        void Move();
    }
}