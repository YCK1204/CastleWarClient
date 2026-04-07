using System;

namespace GameLogic.Interfaces
{
    public interface IDieable
    {
        Action OnDie { get; set; }
        Action OnDeath { get; set; }
    }
}