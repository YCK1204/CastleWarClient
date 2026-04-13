using System;
using GameLogic.FieldObjects;
using GameLogic.Interfaces;

namespace GameLogic.Unit
{
    public abstract class BaseUnitController : FieldObject, IMoveable
    {
        public ushort PathId { get; set; }
        public float Progress { get; set; }
        public abstract float Speed { get; set; }
        public Action OnMove { get; set; }
        public abstract void Move();
    }
}
