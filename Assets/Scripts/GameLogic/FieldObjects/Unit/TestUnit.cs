using System;
using GameLogic.FieldObjects;
using GameLogic.Interfaces;

namespace GameLogic.Unit
{
    public class TestUnit : BaseUnitController
    {
        public override float Speed { get; set; }
        public override void TakeDamage(float damage)
        {
        }

        public override void Move()
        {
            
        }

        public override void Attack(FieldObject target)
        {
            if (target != null && !target.IsDead)
            {
                target.TakeDamage(AttackPower);
            }
        }
    }
}