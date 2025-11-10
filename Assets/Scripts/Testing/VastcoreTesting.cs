using UnityEngine;

namespace Vastcore.Testing
{
    /// <summary>
    /// テスト用アセンブリ - Burstコンパイラの参照解決用
    /// </summary>
    public static class TestUtilities
    {
        /// <summary>
        /// テスト用ダミーメソッド
        /// </summary>
        public static void DummyTest()
        {
            Debug.Log("Vastcore.Testing assembly loaded successfully");
        }
    }
}
