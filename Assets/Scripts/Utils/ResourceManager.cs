using System;
using System.Collections.Generic;
using System.Threading;
using Core;
using CWFramework;
using Scene;
using Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utils.ResourceManager
{
    public class ResourceManager : Singleton<ResourceManager>
    {
        private string CurrentScene => SceneManagerEx.Instance.CurrentSceneName;

        // T1=씬이름, T2=에셋키(Addressable address), T3=핸들
        private readonly DoubleDictionary<string, string, AsyncOperationHandle> _handleCache = new();

        // 로딩 중 dedup: T1=씬이름, T2=에셋키
        private readonly Dictionary<string, Dictionary<string, List<Action<object>>>> _pendingCallbacks = new();

        // ──────────────────────────────────────────
        // Get — 프리로드된 에셋 조회
        // ──────────────────────────────────────────

        /// <summary>
        /// PreloadSceneAsync로 미리 로드된 에셋을 이름으로 즉시 반환.
        /// 캐시에 없으면 null 반환.
        /// </summary>
        public T Get<T>(string key) where T : UnityEngine.Object
        {
            if (!_handleCache.TryGetValue(CurrentScene, key, out var handle))
            {
                Debug.LogWarning($"[ResourceManager] Get: '{key}' 캐시 없음. PreloadSceneAsync 먼저 호출 필요");
                return null;
            }

            // T가 에셋 타입 자체인 경우 (GameObject, Texture, AudioClip 등)
            if (handle.Result is T result) return result;

            // T가 컴포넌트인 경우 프리팹에서 GetComponent
            if (handle.Result is GameObject go)
                return go.GetComponent(typeof(T)) as T;

            return null;
        }

        // ──────────────────────────────────────────
        // Load — 콜백
        // ──────────────────────────────────────────

        /// <summary>
        /// 단일 에셋 비동기 로드 (콜백).
        /// 같은 키 중복 요청 시 핸들을 하나만 생성하고 콜백을 모두 보장함.
        /// </summary>
        public void LoadAsync<T>(string key, Action<T> callback) where T : UnityEngine.Object
        {
            var scene = CurrentScene;

            if (_handleCache.TryGetValue(scene, key, out var cached))
            {
                callback?.Invoke(cached.Result as T);
                return;
            }

            if (IsPending(scene, key))
            {
                AddPendingCallback(scene, key, obj => callback?.Invoke(obj as T));
                return;
            }

            SetPending(scene, key, new List<Action<object>> { obj => callback?.Invoke(obj as T) });

            var handle = Addressables.LoadAssetAsync<T>(key);
            handle.Completed += h =>
            {
                var pending = ConsumePending(scene, key);

                if (h.Status == AsyncOperationStatus.Succeeded)
                {
                    _handleCache.Set(scene, key, h);
                    foreach (var cb in pending) cb?.Invoke(h.Result);
                }
                else
                {
                    Debug.LogError($"[ResourceManager] LoadAsync Failed: {key}");
                    h.Release();
                }
            };
        }

        // ──────────────────────────────────────────
        // Load — Awaitable
        // ──────────────────────────────────────────

        /// <summary>
        /// 단일 에셋 비동기 로드 (Awaitable).
        /// 로드 중인 키 요청 시 완료까지 대기 후 반환.
        /// 취소 또는 실패 시 null 반환.
        /// </summary>
        public async Awaitable<T> LoadAsync<T>(string key, CancellationToken ct = default) where T : UnityEngine.Object
        {
            var scene = CurrentScene;

            if (_handleCache.TryGetValue(scene, key, out var cached))
                return cached.Result as T;

            if (IsPending(scene, key))
            {
                while (IsPending(scene, key))
                    await Awaitable.NextFrameAsync(ct);

                return _handleCache.TryGetValue(scene, key, out var done)
                    ? done.Result as T
                    : null;
            }

            SetPending(scene, key, new List<Action<object>>());

            var handle = Addressables.LoadAssetAsync<T>(key);
            try { await handle.ToAwaitable(ct: ct); }
            catch
            {
                ConsumePending(scene, key);
                handle.Release();
                throw;
            }

            ConsumePending(scene, key);

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[ResourceManager] LoadAsync Failed: {key}");
                handle.Release();
                return null;
            }

            _handleCache.Set(scene, key, handle);
            return handle.Result;
        }

        // ──────────────────────────────────────────
        // Load — 동기
        // ──────────────────────────────────────────

        /// <summary>
        /// 단일 에셋 동기 로드. 메인 스레드 블로킹 주의, 소형 에셋에만 사용.
        /// </summary>
        public T Load<T>(string key) where T : UnityEngine.Object
        {
            var scene = CurrentScene;

            if (_handleCache.TryGetValue(scene, key, out var cached))
                return cached.Result as T;

            var handle = Addressables.LoadAssetAsync<T>(key);
            handle.WaitForCompletion();

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[ResourceManager] Load Failed: {key}");
                handle.Release();
                return null;
            }

            _handleCache.Set(scene, key, handle);
            return handle.Result;
        }

        // ──────────────────────────────────────────
        // Preload
        // ──────────────────────────────────────────

        /// <summary>
        /// 씬 이름(= Addressables 라벨)으로 씬 에셋 전부 프리로드.
        /// 각 에셋을 개별 핸들로 _handleCache[scene][address]에 저장.
        /// 완료 후 Get<T>(key)로 즉시 접근 가능.
        /// </summary>
        public async Awaitable PreloadSceneAsync(string scene, CancellationToken ct = default)
        {
            if (_handleCache.ContainsKey(scene)) return;

            // 씬 라벨의 모든 리소스 위치 조회
            var locHandle = Addressables.LoadResourceLocationsAsync(scene, typeof(UnityEngine.Object));
            await locHandle.ToAwaitable(ct: ct);

            if (locHandle.Status != AsyncOperationStatus.Succeeded || locHandle.Result.Count == 0)
            {
                Debug.LogWarning($"[ResourceManager] PreloadSceneAsync: '{scene}' 라벨 에셋 없음. 건너뜀.");
                Addressables.Release(locHandle);
                return;
            }

            // 모든 에셋 로드 동시 시작
            var pendingLoads = new List<(string key, AsyncOperationHandle<UnityEngine.Object> handle)>(locHandle.Result.Count);
            foreach (var loc in locHandle.Result)
                pendingLoads.Add((loc.PrimaryKey, Addressables.LoadAssetAsync<UnityEngine.Object>(loc)));

            Addressables.Release(locHandle);

            // 완료 대기 및 개별 핸들 캐싱
            foreach (var (key, assetHandle) in pendingLoads)
            {
                try { await assetHandle.ToAwaitable(ct: ct); }
                catch
                {
                    assetHandle.Release();
                    throw;
                }

                if (assetHandle.Status == AsyncOperationStatus.Succeeded)
                    _handleCache.Set(scene, key, assetHandle);
                else
                {
                    Debug.LogError($"[ResourceManager] PreloadSceneAsync: '{key}' 로드 실패");
                    assetHandle.Release();
                }
            }
        }

        // ──────────────────────────────────────────
        // Unload
        // ──────────────────────────────────────────

        /// <summary>현재 씬의 특정 키 에셋 해제.</summary>
        public void Unload(string key)
        {
            var scene = CurrentScene;
            if (_handleCache.TryGetValue(scene, key, out var handle))
            {
                handle.Release();
                _handleCache.Remove(scene, key);
            }
        }

        /// <summary>씬에 등록된 에셋 전부 해제.</summary>
        public void UnloadScene(string scene)
        {
            if (!_handleCache.TryGetInner(scene, out var sceneDict)) return;

            foreach (var handle in sceneDict.Values)
                handle.Release();

            _handleCache.Remove(scene);
        }

        /// <summary>전체 에셋 해제 (앱 종료 시).</summary>
        public void UnloadAll()
        {
            foreach (var inner in _handleCache.InnerValues())
                foreach (var handle in inner.Values)
                    handle.Release();

            // 새 인스턴스로 교체
            foreach (var inner in _handleCache.InnerValues())
                inner.Clear();
        }

        // ──────────────────────────────────────────
        // Internal — pending 관리
        // ──────────────────────────────────────────

        private bool IsPending(string scene, string key) =>
            _pendingCallbacks.TryGetValue(scene, out var inner) && inner.ContainsKey(key);

        private void AddPendingCallback(string scene, string key, Action<object> cb) =>
            _pendingCallbacks[scene][key].Add(cb);

        private void SetPending(string scene, string key, List<Action<object>> callbacks)
        {
            if (!_pendingCallbacks.ContainsKey(scene))
                _pendingCallbacks[scene] = new Dictionary<string, List<Action<object>>>();
            _pendingCallbacks[scene][key] = callbacks;
        }

        private List<Action<object>> ConsumePending(string scene, string key)
        {
            if (!_pendingCallbacks.TryGetValue(scene, out var inner)) return new List<Action<object>>();
            var list = inner.TryGetValue(key, out var callbacks) ? callbacks : new List<Action<object>>();
            inner.Remove(key);
            return list;
        }

#if UNITY_EDITOR
        /// <summary>QA용 동기 로드 (경로 예시: "Assets/Prefabs/Foo.prefab").</summary>
        public T LoadForEditor<T>(string path) where T : UnityEngine.Object
            => AssetDatabase.LoadAssetAtPath<T>(path);
#endif
    }
}
