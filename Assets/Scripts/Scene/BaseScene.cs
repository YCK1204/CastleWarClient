using System;
using UnityEngine;
using Utils.ResourceManager;

namespace Scene
{
    public enum SceneType
    {
        TestScene
    }

    public class BaseScene : MonoBehaviour
    {
        private SceneType _sceneType;

        public SceneType SceneType
        {
            get => _sceneType;
            protected set
            {
                _sceneType = value;
                SceneManagerEx.Instance.RegisterScene(this);
            }
        }

        protected virtual void Init(){}

        protected virtual async void Awake()
        {
            Init();
            SceneManagerEx.Instance.RegisterScene(this);
            await ResourceManager.Instance.PreloadSceneAsync(_sceneType.ToString(), destroyCancellationToken);
            OnAssetLoaded();
        }
        
        protected virtual void OnAssetLoaded(){}
    }
}