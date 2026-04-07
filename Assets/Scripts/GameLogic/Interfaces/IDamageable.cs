using System;

namespace GameLogic.Interfaces
{
    public interface IDamageable
    {
        void TakeDamage(float damage);
        Action<float> OnDamageTaken { get; set; }
    }
}