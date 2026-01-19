using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Testing
{
    /// <summary>
    /// 最低限のテスト用ロガー。実際の実装が無くてもコンパイルできるようにするスタブです。
    /// </summary>
    public class TestLogger
    {
        public void Log(string message)
        {
            Debug.Log(message);
        }

        public void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }
    }

    /// <summary>
    /// 統合テストマネージャのスタブ。メモリテストなどから参照される最低限のプロパティのみを提供します。
    /// </summary>
    public class VastcoreIntegrationTestManager : MonoBehaviour
    {
        public TestLogger Logger { get; } = new TestLogger();

        public Transform TestPlayer { get; set; }

        public RuntimeTerrainManager RuntimeTerrainManager { get; set; }

        public PrimitiveTerrainManager PrimitiveTerrainManager { get; set; }
    }
}
