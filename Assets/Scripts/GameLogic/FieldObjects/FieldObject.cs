using System;
using GameLogic.Interfaces;
using UnityEngine;

namespace GameLogic.FieldObjects
{
    public abstract class FieldObject : MonoBehaviour, IDamageable, IDieable
    {
        /// <summary>서버에서 할당한 식별 id</summary>
        public ushort NetworkId { get; set; }

        public Player.Player Owner { get; set; }
        private float _hp;

        protected virtual float Hp
        {
            get => _hp;
            set
            {
                float diff = Mathf.Abs(value - _hp);
                _hp = value;
                if (_hp < 0)
                {
                    _hp = 0;
                    OnDie?.Invoke();
                }
                else
                {
                    OnDamageTaken?.Invoke(diff);
                }
            }
        }

        public float MaxHp { get; set; }

        public bool IsDead => Hp <= 0;

        public float AttackPower { get; set; }
        public float AttackRange { get; set; }
        public float DefensePower { get; set; }


        public abstract void TakeDamage(float damage);

        public Action<float> OnDamageTaken { get; set; }
        public Action OnDie { get; set; }
        public Action OnDeath { get; set; }
    }
}