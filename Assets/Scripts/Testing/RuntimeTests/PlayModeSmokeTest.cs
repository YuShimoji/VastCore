using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

namespace Vastcore.Testing.RuntimeTests
{
    /// <summary>
    /// PlayMode 実行環境の健全性を確認する最小限のテスト
    /// </summary>
    public class PlayModeSmokeTest
    {
        [Test]
        public void Runtime_Namespace_Access_Is_Possible()
        {
            // Just verify we can instantiate objects from common namespaces
            var obj = new GameObject("NamespaceTest");
            Assert.IsNotNull(obj);
            Object.DestroyImmediate(obj);
        }

        [UnityTest]
        public IEnumerator Runtime_CanEnterPlayMode()
        {
            // PlayMode に入れるか確認
            yield return null;
            
            Assert.IsTrue(Application.isPlaying, "Application should be in PlayMode");
            Debug.Log("✓ PlayMode Smoke Test: Environment is healthy.");
        }

        [Test]
        public void Namespace_Accessibility_Check()
        {
            // 主要な名前空間にアクセスできるか確認
            var core = new GameObject("CoreTest");
            Assert.IsNotNull(core);
            Object.DestroyImmediate(core);
        }
    }
}
