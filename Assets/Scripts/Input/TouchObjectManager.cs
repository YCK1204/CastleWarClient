using System.Collections.Generic;
using UnityEngine;
using Core;

namespace GameLogic.Input
{
    public class TouchObjectManager : MonoSingleton<TouchObjectManager>
    {
        private HashSet<ITouchable> _touchObjects = new HashSet<ITouchable>();
        private ITouchable _currentTouched;

        protected override void Init()
        {
        }

        public void RegisterTouchObject(ITouchable touchObject)
        {
            _touchObjects.Add(touchObject);
        }

        public void UnregisterTouchObject(ITouchable touchObject)
        {
            _touchObjects.Remove(touchObject);
        }

        public void Clear()
        {
            _touchObjects.Clear();
        }

        private void Update()
        {
            if (UnityEngine.Input.touchCount > 0)
                HandleTouchInput();
            else
                HandleMouseInput();
        }

        private void HandleTouchInput()
        {
            if (UnityEngine.Input.touchCount == 0) return;

            var touch = UnityEngine.Input.GetTouch(0);
            var worldPos = Camera.main.ScreenToWorldPoint(touch.position);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    var hit = Physics2D.Raycast(worldPos, Vector2.zero);
                    if (hit.collider != null && hit.collider.TryGetComponent<ITouchable>(out var touchable))
                    {
                        _currentTouched = touchable;
                        _currentTouched.OnTouchDown?.Invoke();
                    }
                    break;

                case TouchPhase.Moved:
                    _currentTouched?.OnTouchDrag?.Invoke(worldPos);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    _currentTouched?.OnTouchUp?.Invoke();
                    _currentTouched = null;
                    break;
            }
        }

        private void HandleMouseInput()
        {
            var worldPos = (Vector2)Camera.main.ScreenToWorldPoint(UnityEngine.Input.mousePosition);

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                var hit = Physics2D.Raycast(worldPos, Vector2.zero);
                if (hit.collider != null && hit.collider.TryGetComponent<ITouchable>(out var touchable))
                {
                    _currentTouched = touchable;
                    _currentTouched.OnTouchDown?.Invoke();
                }
            }
            else if (UnityEngine.Input.GetMouseButton(0))
            {
                _currentTouched?.OnTouchDrag?.Invoke(worldPos);
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                _currentTouched?.OnTouchUp?.Invoke();
                _currentTouched = null;
            }
        }
    }
}