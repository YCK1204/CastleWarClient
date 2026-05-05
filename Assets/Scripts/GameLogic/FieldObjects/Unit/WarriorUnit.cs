using GameLogic.Interfaces;
using GameLogic.Map;
using UnityEngine;

namespace GameLogic.Unit
{
    public class WarriorUnit : BaseUnitController, IAttackable
    {
        public override float Speed { get; set; }

        public override void TakeDamage(float damage)
        {
            Hp -= damage;
        }

        public override void Move()
        {
            var worldPos = MapManager.Instance.GetWorldPosition(PathId, Progress);
            transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
        }

        public void Attack(FieldObjects.FieldObject target)
        {
            if (target != null && !target.IsDead)
                target.TakeDamage(AttackPower);
        }
    }
}
