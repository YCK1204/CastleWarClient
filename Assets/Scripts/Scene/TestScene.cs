using Network;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils.ResourceManager;

namespace Scene
{
    public class TestScene : BaseScene
    {
        protected override void Init()
        {
            SceneType = SceneType.TestScene;
        }

        // EventSystem, NetworkManager 생성 및 초기화
        protected override void OnAssetLoaded()
        {
            if (SceneManagerEx.Instance.EventSystem != null)
                return;
            
            EventSystem esPrefab = ResourceManager.Instance.Get<EventSystem>("@EventSystem");
            if (esPrefab == null)
            {
                Debug.LogError("@EventSystem not set");
                return;
            }
            
            EventSystem eventSystem = Instantiate(esPrefab);
            SceneManagerEx.Instance.EventSystem = eventSystem;
            DontDestroyOnLoad(eventSystem.gameObject);

            if (NetworkManager.Instance != null)
                return;
            
            NetworkManager networkManagerPrefab = ResourceManager.Instance.Get<NetworkManager>("@NetworkManager");
            if (networkManagerPrefab == null)
            {
                Debug.LogError("@NetworkManager not set");
                return;
            }
            
            Instantiate(networkManagerPrefab);
            
            Canvas canvas = ResourceManager.Instance.Get<Canvas>("Canvas");
            var go = Instantiate(canvas);
        }
    }
}