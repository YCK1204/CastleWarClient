using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Core
{
    public static class Extensions
    {
        // ──────────────────────────────────────────
        // String
        // ──────────────────────────────────────────

        /// <summary>문자열을 SHA256 해시(Base64)로 변환</summary>
        public static string ToHash(this string value)
        {
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        // ──────────────────────────────────────────
        // Byte Array (FlatBuffers 호환)
        // ──────────────────────────────────────────

        /// <summary>byte[] → sbyte[] (FlatBuffers VectorBlock 전달 시 사용)</summary>
        public static sbyte[] ToSBytes(this byte[] bytes) => (sbyte[])(Array)bytes;

        /// <summary>sbyte[] → byte[] (FlatBuffers 수신 데이터 변환 시 사용)</summary>
        public static byte[] ToBytes(this sbyte[] sbytes) => (byte[])(Array)sbytes;

        // ──────────────────────────────────────────
        // ArraySegment
        // ──────────────────────────────────────────

        /// <summary>ArraySegment를 새 byte[]로 복사</summary>
        public static byte[] ToArray(this ArraySegment<byte> segment)
        {
            byte[] result = new byte[segment.Count];
            Buffer.BlockCopy(segment.Array, segment.Offset, result, 0, segment.Count);
            return result;
        }


        public static T FindChild<T>(this Transform transform, string name = null, bool recursive = false)
            where T : Transform
        {
            if (recursive)
            {
                var children = transform.GetComponentsInChildren<T>();
                if (children != null && children.Length > 0)
                {
                    foreach (var child in children)
                    {
                        if (name == null)
                            return child;
                        if (child.name == name)
                            return child;
                    }
                }
            }
            else
            {
                var chilcCount = transform.childCount;

                for (int i = 0; i < chilcCount; i++)
                {
                    var child = transform.GetChild(i);
                    if (child.TryGetComponent<T>(out var component))
                    {
                        if (name == null)
                            return component;
                        if (child.name == name)
                            return component;
                    }
                }
            }

            return null;
        }

        public static List<T> FindChildren<T>(this Transform transform, bool recursive = false)
        {
            if (recursive)
            {
                var children = transform.GetComponentsInChildren<T>();
                if (children != null && children.Length > 0)
                {
                    return children.ToList();
                }
            }
            else
            {
                var chilcCount = transform.childCount;

                List<T> children = new List<T>();
                for (int i = 0; i < chilcCount; i++)
                {
                    var child = transform.GetChild(i);
                    if (child.TryGetComponent<T>(out var component))
                    {
                        children.Add(component);
                    }
                }

                if (children.Count > 0)
                    return children;
            }

            return null;
        }

        public static float DistanceTo(this Vector2 vector, Vector2 vector2)
        {
            return Vector2.Distance(vector, vector2);
        }

        public static Vector2 DirectionTo(this Vector2 vector, Vector2 vector2)
        {
            return vector2 - vector;
        }

        // ──────────────────────────────────────────
        // Awaitable 유틸
        // ──────────────────────────────────────────

        /// <summary>지정한 초만큼 대기 (Awaitable, CancellationToken 지원)</summary>
        public static async Awaitable WaitForSecondsAsync(float seconds, CancellationToken ct = default)
        {
            await Awaitable.WaitForSecondsAsync(seconds, ct);
        }

        /// <summary>다음 프레임까지 대기</summary>
        public static async Awaitable NextFrameAsync(CancellationToken ct = default)
        {
            await Awaitable.NextFrameAsync(ct);
        }

        /// <summary>지정 프레임 수만큼 대기</summary>
        public static async Awaitable WaitForFramesAsync(int frames, CancellationToken ct = default)
        {
            for (int i = 0; i < frames; i++)
                await Awaitable.NextFrameAsync(ct);
        }

        /// <summary>조건이 true가 될 때까지 매 프레임 대기</summary>
        public static async Awaitable WaitUntilAsync(Func<bool> condition, CancellationToken ct = default)
        {
            while (!condition())
                await Awaitable.NextFrameAsync(ct);
        }

        /// <summary>조건이 false가 될 때까지 매 프레임 대기</summary>
        public static async Awaitable WaitWhileAsync(Func<bool> condition, CancellationToken ct = default)
        {
            while (condition())
                await Awaitable.NextFrameAsync(ct);
        }

        /// <summary>End of Frame까지 대기</summary>
        public static async Awaitable WaitForEndOfFrameAsync(CancellationToken ct = default)
        {
            await Awaitable.EndOfFrameAsync(ct);
        }

        /// <summary>백그라운드 스레드로 전환 (무거운 연산용)</summary>
        public static async Awaitable SwitchToBackgroundAsync()
        {
            await Awaitable.BackgroundThreadAsync();
        }

        /// <summary>메인 스레드로 복귀 (Unity API 호출 전 필수)</summary>
        public static async Awaitable SwitchToMainThreadAsync()
        {
            await Awaitable.MainThreadAsync();
        }

        // ──────────────────────────────────────────
        // AsyncOperation
        // ──────────────────────────────────────────

        /// <summary>
        /// AsyncOperation을 Awaitable로 변환, progress 콜백 지원
        /// activationThreshold: 이 값에 도달하면 완료로 간주 (씬 로드는 0.9f)
        /// </summary>
        public static async Awaitable ToAwaitable(
            this AsyncOperation op,
            Action<float> onProgress = null,
            float activationThreshold = 1f,
            CancellationToken ct = default)
        {
            while (op.progress < activationThreshold)
            {
                ct.ThrowIfCancellationRequested();
                onProgress?.Invoke(Mathf.Clamp01(op.progress / activationThreshold));
                await Awaitable.NextFrameAsync(ct);
            }
            onProgress?.Invoke(1f);
        }

        /// <summary>
        /// AsyncOperationHandle을 Awaitable로 변환 (Addressables용)
        /// </summary>
        public static async Awaitable ToAwaitable(
            this AsyncOperationHandle handle,
            Action<float> onProgress = null,
            CancellationToken ct = default)
        {
            while (!handle.IsDone)
            {
                ct.ThrowIfCancellationRequested();
                onProgress?.Invoke(handle.PercentComplete);
                await Awaitable.NextFrameAsync(ct);
            }
            onProgress?.Invoke(1f);
        }

        /// <summary>
        /// AsyncOperationHandle&lt;T&gt;을 Awaitable로 변환 (Addressables 제네릭 핸들용)
        /// </summary>
        public static async Awaitable ToAwaitable<T>(
            this AsyncOperationHandle<T> handle,
            Action<float> onProgress = null,
            CancellationToken ct = default)
        {
            while (!handle.IsDone)
            {
                ct.ThrowIfCancellationRequested();
                onProgress?.Invoke(handle.PercentComplete);
                await Awaitable.NextFrameAsync(ct);
            }
            onProgress?.Invoke(1f);
        }
    }
}