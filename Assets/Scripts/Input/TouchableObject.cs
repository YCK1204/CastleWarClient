using System;
using UnityEngine;

namespace GameLogic.Input
{
    public class TouchableObject : MonoBehaviour, ITouchable
    {
        private Action _touchDown;
        private Action _touchUp;
        private Action<Vector2> _touchDrag;

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
    }
}