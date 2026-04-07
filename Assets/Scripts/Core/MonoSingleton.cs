using System;
using UnityEngine;

namespace Core
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        public static T Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this as T;
            DontDestroyOnLoad(gameObject);
            Init();
        }

        protected virtual void Init()
        {
        }
    }
}