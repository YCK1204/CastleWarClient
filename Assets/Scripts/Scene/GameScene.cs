using GameLogic.Map;
using Network;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils.ResourceManager;

namespace Scene
{
    public class GameScene : BaseScene
    {
        protected override void Init()
        {
            SceneType = SceneType.GameScene;
        }

        protected override void OnAssetLoaded()
        {
            if (SceneManagerEx.Instance.EventSystem == null)
            {
                EventSystem esPrefab = ResourceManager.Instance.Get<EventSystem>("@EventSystem");
                if (esPrefab != null)
                {
                    EventSystem eventSystem = Instantiate(esPrefab);
                    SceneManagerEx.Instance.EventSystem = eventSystem;
                    DontDestroyOnLoad(eventSystem.gameObject);
                }
            }

            if (NetworkManager.Instance == null)
            {
                NetworkManager networkManagerPrefab = ResourceManager.Instance.Get<NetworkManager>("@NetworkManager");
                if (networkManagerPrefab != null)
                    Instantiate(networkManagerPrefab);
            }

            MapManager.Instance.LoadFromJson(0);
        }
    }
}