using System;
using UnityEngine;

namespace GameLogic.Input
{
    public interface ITouchable
    {
        internal Action OnTouchDown { get; set; }
        internal Action OnTouchUp { get; set; }
        internal Action<Vector2> OnTouchDrag { get; set; }

        public void AddOnTouchDown(Action action)
        {
            OnTouchDown += action;
        }

        public void AddOnTouchUp(Action action)
        {
            OnTouchUp += action;
        }

        public void AddOnTouchDrag(Action<Vector2> action)
        {
            OnTouchDrag += action;
        }

        public void SetOnTouchDown(Action action)
        {
            OnTouchDown = action;
        }

        public void SetOnTouchUp(Action action)
        {
            OnTouchUp = action;
        }

        public void SetOnTouchDrag(Action<Vector2> action)
        {
            OnTouchDrag = action;
        }
    }
}