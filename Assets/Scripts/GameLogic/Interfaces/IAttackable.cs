using GameLogic.FieldObjects;

namespace GameLogic.Interfaces
{
    public interface IAttackable
    {
        void Attack(FieldObject target);
    }
}