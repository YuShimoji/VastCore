using UnityEngine;

namespace Vastcore.Core
{
    /// <summary>
    /// 共通のプレイヤー Transform 解決処理を提供するヘルパー。
    /// Generation/Terrain いずれのアセンブリからも参照できるよう Core に配置。
    /// </summary>
    public static class PlayerTransformResolver
    {
        /// <summary>
        /// 既存の Transform が無い場合にシーンからプレイヤー Transform を探索します。
        /// </summary>
        public static Transform Resolve(Transform current = null)
        {
            if (current != null)
            {
                return current;
            }

            var taggedPlayer = TryFindPlayerByTag();
            if (taggedPlayer != null)
            {
                return taggedPlayer.transform;
            }

            var mainCamera = Camera.main;
            return mainCamera != null ? mainCamera.transform : null;
        }

        private static GameObject TryFindPlayerByTag()
        {
            try
            {
                return GameObject.FindGameObjectWithTag("Player");
            }
            catch (UnityException)
            {
                return null;
            }
        }
    }
}
