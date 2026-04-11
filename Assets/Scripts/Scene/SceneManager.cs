using System;
using System.Collections.Generic;
using System.Threading;
using Core;
using CWFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils.ResourceManager;

namespace Scene
{
    public class SceneManagerEx : Singleton<SceneManagerEx>
    {
        private BaseScene _currentScene = null;
        private bool _isLoading = false;

        // Additive로 로드된 씬 목록
        private readonly HashSet<SceneType> _additiveScenes = new();

        /// <summary>현재 활성 씬.</summary>
        public BaseScene CurrentScene => _currentScene;

        /// <summary>씬 로드 중 여부. 중복 요청 방지 및 로딩 UI 표시에 활용.</summary>
        public bool IsLoading => _isLoading;

        /// <summary>
        /// BaseScene.Awake에서 호출 — 현재 씬 등록.
        /// 씬 전환 완료 후 새 BaseScene이 자신을 등록함.
        /// </summary>
        public void RegisterScene(BaseScene scene) => _currentScene = scene;
        // ──────────────────────────────────────────
        // Single 씬 로드
        // ──────────────────────────────────────────

        /// <summary>
        /// 비동기 씬 로드.
        /// 로드 중이면 무시. 취소 시 _isLoading이 반드시 false로 복구됨.
        /// </summary>
        public async Awaitable LoadSceneAsync(SceneType sceneType, Action onComplete = null,
            CancellationToken ct = default)
        {
            if (_isLoading) return;
            await LoadSceneAsyncInternal(sceneType, null, ct);
            onComplete?.Invoke();
        }

        /// <summary>
        /// 비동기 씬 로드 (프로그래스바용).
        /// onProgress: 0~1 사이 진행도 콜백.
        /// </summary>
        public async Awaitable LoadSceneAsync(SceneType sceneType, Action<float> onProgress, Action onComplete = null,
            CancellationToken ct = default)
        {
            if (_isLoading) return;
            await LoadSceneAsyncInternal(sceneType, onProgress, ct);
            onComplete?.Invoke();
        }

        /// <summary>
        /// 동기 씬 로드. 에디터/테스트 전용.
        /// </summary>
        public void LoadScene(SceneType sceneType)
        {
            if (_isLoading) return;
            SceneManager.LoadScene(sceneType.ToString());
        }

        // ──────────────────────────────────────────
        // Additive 씬 로드/언로드
        // ──────────────────────────────────────────

        /// <summary>
        /// Additive 방식으로 씬 추가 로드 (팝업, UI 씬 등).
        /// 이미 로드된 씬은 무시. 취소/실패 시 목록에서 롤백.
        /// </summary>
        public async Awaitable LoadSceneAdditiveAsync(SceneType sceneType, Action onComplete = null,
            CancellationToken ct = default)
        {
            if (_additiveScenes.Contains(sceneType)) return;

            _additiveScenes.Add(sceneType);
            try
            {
                var op = SceneManager.LoadSceneAsync(sceneType.ToString(), LoadSceneMode.Additive);
                await op.ToAwaitable(ct: ct);
                onComplete?.Invoke();
            }
            catch
            {
                _additiveScenes.Remove(sceneType);
                throw;
            }
        }

        /// <summary>
        /// Additive로 로드된 씬 언로드.
        /// Unity 씬 언로드 완료 후 에셋 해제 (오브젝트 소멸 후 해제 보장).
        /// </summary>
        public async Awaitable UnloadSceneAdditiveAsync(SceneType sceneType, Action onComplete = null,
            CancellationToken ct = default)
        {
            if (!_additiveScenes.Contains(sceneType)) return;

            _additiveScenes.Remove(sceneType);
            var op = SceneManager.UnloadSceneAsync(sceneType.ToString());
            await op.ToAwaitable(ct: ct);

            // 씬 언로드 완료 후 에셋 해제 (오브젝트들이 모두 소멸된 뒤)
            ResourceManager.Instance.UnloadScene(sceneType.ToString());
            onComplete?.Invoke();
        }

        // ──────────────────────────────────────────
        // Internal
        // ──────────────────────────────────────────

        private async Awaitable LoadSceneAsyncInternal(SceneType sceneType, Action<float> onProgress,
            CancellationToken ct)
        {
            _isLoading = true;
            try
            {
                // 이전 씬 에셋 해제
                if (_currentScene != null)
                    ResourceManager.Instance.UnloadScene(_currentScene.SceneType.ToString());

                // 새 씬 어드레서블 리소스 프리로드 (씬 활성화 전에 완료)
                await ResourceManager.Instance.PreloadSceneAsync(sceneType.ToString(), ct);

                // Unity 씬 로드 (allowSceneActivation = false → 0.9f까지 진행 후 활성화)
                var op = SceneManager.LoadSceneAsync(sceneType.ToString());
                op.allowSceneActivation = false;
                await op.ToAwaitable(onProgress, activationThreshold: 0.9f, ct: ct);
                op.allowSceneActivation = true;

                // 한 프레임 대기 — BaseScene.Awake + RegisterScene 실행 보장
                await Awaitable.NextFrameAsync(ct);
            }
            finally
            {
                // 취소/예외 시에도 반드시 복구
                _isLoading = false;
            }
        }
    }
}