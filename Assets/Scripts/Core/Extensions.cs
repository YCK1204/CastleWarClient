using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

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
    }
}