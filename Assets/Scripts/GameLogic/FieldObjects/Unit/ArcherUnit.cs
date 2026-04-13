using GameLogic.Interfaces;

namespace GameLogic.Unit
{
    public class ArcherUnit : BaseUnitController, IAttackable
    {
        public override float Speed { get; set; }

        public override void TakeDamage(float damage)
        {
            Hp -= damage;
        }

        public override void Move()
        {
            // PathId + Progress 기반 이동 처리
        }

        public void Attack(GameLogic.FieldObjects.FieldObject target)
        {
            if (target != null && !target.IsDead)
                target.TakeDamage(AttackPower);
        }
    }
}
