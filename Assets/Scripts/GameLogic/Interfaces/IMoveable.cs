using System;
using UnityEngine;

namespace GameLogic.Interfaces
{
    public interface IMoveable
    {
        Vector2 TargetPoint { get; set; }
        Vector2 Direction { get; set; }
        float Speed { get; set; }
        Action OnMove { get; set; }

        void Move();
    }
}