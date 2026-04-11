using System;
using System.Collections.Generic;
using System.Threading;
using Core;
using CWFramework;
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
        // 로드 완료된 핸들 캐시
        private readonly Dictionary<string, AsyncOperationHandle> _handleCache = new();

        // 로드 중인 키 → 대기 콜백 목록 (중복 요청 dedup)
        private readonly Dictionary<string, List<Action<object>>> _pendingCallbacks = new();

        // 씬별 키 목록 (씬 언로드 시 일괄 해제용)
        private readonly Dictionary<string, HashSet<string>> _sceneHandles = new();

        // ──────────────────────────────────────────
        // Load — 콜백
        // ──────────────────────────────────────────

        /// <summary>
        /// 단일 에셋 비동기 로드 (콜백).
        /// 같은 키 중복 요청 시 핸들을 하나만 생성하고 콜백을 모두 보장함.
        /// </summary>
        public void LoadAsync<T>(string key, Action<T> callback, string scene = null) where T : UnityEngine.Object
        {
            if (_handleCache.TryGetValue(key, out var cached))
            {
                RegisterSceneHandle(scene, key);
                callback?.Invoke(cached.Convert<T>().Result);
                return;
            }

            if (_pendingCallbacks.ContainsKey(key))
            {
                _pendingCallbacks[key].Add(obj => callback?.Invoke(obj as T));
                return;
            }

            _pendingCallbacks[key] = new List<Action<object>> { obj => callback?.Invoke(obj as T) };

            var handle = Addressables.LoadAssetAsync<T>(key);
            handle.Completed += h =>
            {
                var pending = _pendingCallbacks[key];
                _pendingCallbacks.Remove(key);

                if (h.Status == AsyncOperationStatus.Succeeded)
                {
                    _handleCache[key] = h;
                    RegisterSceneHandle(scene, key);
                    foreach (var cb in pending)
                        cb?.Invoke(h.Result);
                }
                else
                {
                    Debug.LogError($"[ResourceManager] LoadAsync Failed : {key}");
                    h.Release();
                }
            };
        }

        /// <summary>
        /// 태그 기반 다중 에셋 비동기 로드 (콜백).
        /// 같은 태그 중복 요청 시 핸들을 하나만 생성하고 콜백을 모두 보장함.
        /// </summary>
        public void LoadAsyncByTag<T>(string tagKey, Action<IList<T>> callback, string scene = null) where T : UnityEngine.Object
        {
            if (_handleCache.TryGetValue(tagKey, out var cached))
            {
                RegisterSceneHandle(scene, tagKey);
                callback?.Invoke(cached.Convert<IList<T>>().Result);
                return;
            }

            if (_pendingCallbacks.ContainsKey(tagKey))
            {
                _pendingCallbacks[tagKey].Add(obj => callback?.Invoke(obj as IList<T>));
                return;
            }

            _pendingCallbacks[tagKey] = new List<Action<object>> { obj => callback?.Invoke(obj as IList<T>) };

            var handle = Addressables.LoadAssetsAsync<T>(tagKey, null);
            handle.Completed += h =>
            {
                var pending = _pendingCallbacks[tagKey];
                _pendingCallbacks.Remove(tagKey);

                if (h.Status == AsyncOperationStatus.Succeeded)
                {
                    _handleCache[tagKey] = h;
                    RegisterSceneHandle(scene, tagKey);
                    foreach (var cb in pending)
                        cb?.Invoke(h.Result);
                }
                else
                {
                    Debug.LogError($"[ResourceManager] LoadAsyncByTag Failed : {tagKey}");
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
        /// 취소 또는 실패 시 null 반환, 핸들 누수 없음.
        /// </summary>
        public async Awaitable<T> LoadAsync<T>(string key, string scene = null, CancellationToken ct = default) where T : UnityEngine.Object
        {
            if (_handleCache.TryGetValue(key, out var cached))
            {
                RegisterSceneHandle(scene, key);
                return cached.Convert<T>().Result;
            }

            // 이미 로딩 중이면 완료까지 대기
            if (_pendingCallbacks.ContainsKey(key))
            {
                while (_pendingCallbacks.ContainsKey(key))
                    await Awaitable.NextFrameAsync(ct);

                if (_handleCache.TryGetValue(key, out var done))
                {
                    RegisterSceneHandle(scene, key);
                    return done.Convert<T>().Result;
                }
                return null;
            }

            _pendingCallbacks[key] = new List<Action<object>>();
            var handle = Addressables.LoadAssetAsync<T>(key);

            try
            {
                await handle.ToAwaitable(ct: ct);
            }
            catch
            {
                _pendingCallbacks.Remove(key);
                handle.Release();
                throw;
            }

            _pendingCallbacks.Remove(key);

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[ResourceManager] LoadAsync Failed : {key}");
                handle.Release();
                return null;
            }

            _handleCache[key] = handle;
            RegisterSceneHandle(scene, key);
            return handle.Result;
        }

        // ──────────────────────────────────────────
        // Load — 동기
        // ──────────────────────────────────────────

        /// <summary>
        /// 단일 에셋 동기 로드. 메인 스레드 블로킹 주의, 소형 에셋에만 사용.
        /// </summary>
        public T Load<T>(string key, string scene = null) where T : UnityEngine.Object
        {
            if (_handleCache.TryGetValue(key, out var cached))
            {
                RegisterSceneHandle(scene, key);
                return cached.Convert<T>().Result;
            }

            var handle = Addressables.LoadAssetAsync<T>(key);
            handle.WaitForCompletion();

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[ResourceManager] Load Failed : {key}");
                handle.Release();
                return null;
            }

            _handleCache[key] = handle;
            RegisterSceneHandle(scene, key);
            return handle.Result;
        }

        // ──────────────────────────────────────────
        // Preload
        // ──────────────────────────────────────────

        /// <summary>
        /// 씬 이름(= Addressables 라벨)으로 씬 에셋 전부 프리로드.
        /// 이미 로드됐거나 로드 중이면 대기 후 반환.
        /// 실패 시 로그만 남기고 정상 종료 (게임은 계속 진행).
        /// </summary>
        public async Awaitable PreloadSceneAsync(string scene, CancellationToken ct = default)
        {
            // 이미 캐시됨
            if (_handleCache.ContainsKey(scene)) return;

            // 로딩 중이면 완료까지 대기
            if (_pendingCallbacks.ContainsKey(scene))
            {
                while (_pendingCallbacks.ContainsKey(scene))
                    await Awaitable.NextFrameAsync(ct);
                return;
            }

            _pendingCallbacks[scene] = new List<Action<object>>();
            var handle = Addressables.LoadAssetsAsync<UnityEngine.Object>(scene, null);

            try
            {
                await handle.ToAwaitable(ct: ct);
            }
            catch
            {
                _pendingCallbacks.Remove(scene);
                handle.Release();
                throw;
            }

            _pendingCallbacks.Remove(scene);

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogWarning($"[ResourceManager] PreloadSceneAsync: '{scene}' 라벨 에셋 없음 또는 실패. 건너뜀.");
                handle.Release();
                return;
            }

            _handleCache[scene] = handle;
            if (!_sceneHandles.ContainsKey(scene))
                _sceneHandles[scene] = new HashSet<string>();
            _sceneHandles[scene].Add(scene);
        }

        // ──────────────────────────────────────────
        // Unload
        // ──────────────────────────────────────────

        /// <summary>특정 키 에셋 해제.</summary>
        public void Unload(string key)
        {
            if (_handleCache.TryGetValue(key, out var handle))
            {
                handle.Release();
                _handleCache.Remove(key);
            }

            foreach (var keys in _sceneHandles.Values)
                keys.Remove(key);
        }

        /// <summary>씬에 등록된 에셋 전부 해제.</summary>
        public void UnloadScene(string scene)
        {
            if (!_sceneHandles.TryGetValue(scene, out var keys)) return;

            foreach (var key in keys)
            {
                if (_handleCache.TryGetValue(key, out var handle))
                {
                    handle.Release();
                    _handleCache.Remove(key);
                }
            }

            _sceneHandles.Remove(scene);
        }

        /// <summary>전체 에셋 해제 (앱 종료 시).</summary>
        public void UnloadAll()
        {
            foreach (var handle in _handleCache.Values)
                handle.Release();
            _handleCache.Clear();
            _sceneHandles.Clear();
        }

        // ──────────────────────────────────────────
        // Internal
        // ──────────────────────────────────────────

        private void RegisterSceneHandle(string scene, string key)
        {
            if (scene == null) return;
            if (!_sceneHandles.ContainsKey(scene))
                _sceneHandles[scene] = new HashSet<string>();
            _sceneHandles[scene].Add(key);
        }

#if UNITY_EDITOR
        /// <summary>QA용 동기 로드 (경로 예시: "Assets/Prefabs/Foo.prefab").</summary>
        public T LoadForEditor<T>(string path) where T : UnityEngine.Object
            => AssetDatabase.LoadAssetAtPath<T>(path);
#endif
    }
}
