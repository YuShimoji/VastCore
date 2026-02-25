using System.Collections.Generic;
using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Testing
{
    /// <summary>
    /// テスト実行時に参照される最小スタブ実装。Unity 依存ログは保持しない。
    /// </summary>
    public sealed class TestLogger
    {
        private readonly List<string> _messages = new List<string>();

        public IReadOnlyList<string> Messages => _messages;

        public void Log(string message)
        {
            _messages.Add(message ?? string.Empty);
        }

        public void LogWarning(string message)
        {
            _messages.Add($"WARN:{message ?? string.Empty}");
        }
    }

    /// <summary>
    /// 既存統合テストが参照する最小依存スタブ。
    /// </summary>
    public sealed class VastcoreIntegrationTestManager : MonoBehaviour
    {
        public TestLogger Logger { get; } = new TestLogger();

        public Transform TestPlayer { get; set; }

        public RuntimeTerrainManager RuntimeTerrainManager { get; set; }

        public PrimitiveTerrainManager PrimitiveTerrainManager { get; set; }
    }
}
