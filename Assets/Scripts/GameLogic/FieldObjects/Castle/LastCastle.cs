using GameLogic.Interfaces;

namespace GameLogic.FieldObjects
{
    public class LastCastle : Castle, IAttackable
    {
        public void Attack(FieldObject target)
        {
            if (target != null && !target.IsDead)
                target.TakeDamage(AttackPower);
        }
    }
}
