using System;
using GameLogic.Input;
using GameLogic.Interfaces;
using UnityEngine;

namespace GameLogic.FieldObjects
{
    public class Castle : FieldObject, ITouchable
    {
        private Action _touchDown;
        private Action _touchUp;
        private Action<Vector2> _touchDrag;
        #region TouchHandle
        private void OnEnable()
        {
            TouchObjectManager.Instance.RegisterTouchObject(this);
        }

        private void OnDisable()
        {
            TouchObjectManager.Instance.UnregisterTouchObject(this);
        }
        Action ITouchable.OnTouchDown
        {
            get => _touchDown;
            set => _touchDown = value;
        }

        Action ITouchable.OnTouchUp
        {
            get => _touchUp;
            set => _touchUp = value;
        }

        Action<Vector2> ITouchable.OnTouchDrag
        {
            get => _touchDrag;
            set => _touchDrag = value;
        }
        #endregion

        public bool IsActive { get; set; }
        public float CurrentCost { get; set; }
        public float MaxCost { get; set; }
        public int Level { get; set; }

        public override void TakeDamage(float damage)
        {
            Hp -= damage;
        }
    }
}