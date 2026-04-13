using GameLogic.Interfaces;

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
            // PathId + Progress 기반 이동 처리 (MapManager에서 경로 좌표 조회)
        }

        public void Attack(GameLogic.FieldObjects.FieldObject target)
        {
            if (target != null && !target.IsDead)
                target.TakeDamage(AttackPower);
        }
    }
}
