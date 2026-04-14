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
            SceneType = SceneType.Test;
        }

        // EventSystem, NetworkManager 생성 및 초기화
        protected override void OnAssetLoaded()
        {
            EventSystem esPrefab = ResourceManager.Instance.Load<EventSystem>("@EventSystem");
            if (esPrefab == null)
            {
                Debug.LogError("@EventSystem not set");
                return;
            }
            
            EventSystem eventSystem = Instantiate(esPrefab);
            SceneManagerEx.Instance.EventSystem = eventSystem;

            if (NetworkManager.Instance != null)
                return;
            
            NetworkManager networkManagerPrefab = ResourceManager.Instance.Load<NetworkManager>("@NetworkManager");
            if (networkManagerPrefab == null)
            {
                Debug.LogError("@NetworkManager not set");
                return;
            }
            
            Instantiate(networkManagerPrefab);
        }
    }
}