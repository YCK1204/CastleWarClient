namespace GameLogic.Unit
{
    // 직접 공격 없이 아군 유닛 강화 담당
    public class HealerUnit : BaseUnitController
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
    }
}
