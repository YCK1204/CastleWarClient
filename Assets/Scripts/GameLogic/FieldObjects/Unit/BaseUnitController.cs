using System;
using GameLogic.FieldObjects;
using GameLogic.Interfaces;
using UnityEngine;

namespace GameLogic.Unit
{
    public abstract class BaseUnitController : FieldObject, IMoveable, IAttackable
    {
        private float _hp;
        public Vector2 TargetPoint { get; set; }
        public Vector2 Direction { get; set; }
        public abstract float Speed { get; set; }
        public Action OnMove { get; set; }
        public abstract void Move();
        public abstract void Attack(FieldObject target);
    }
}