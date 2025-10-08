using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Vastcore.Tests.PlayMode
{
    public class PlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator FramesAdvanceInPlayMode()
        {
            int start = Time.frameCount;
            yield return null;
            yield return null;
            Assert.Greater(Time.frameCount, start, "Frame count should advance in PlayMode test.");
        }

        [UnityTest]
        public IEnumerator CanInstantiateModernUIManager()
        {
            var go = new GameObject("ModernUIManager_Test");
            System.Type t = System.Type.GetType("Vastcore.UI.ModernUIManager, Vastcore.UI");
            if (t == null)
            {
                Assert.Ignore("ModernUIManager not found. Skipping component instantiation smoke.");
                yield break;
            }
            go.AddComponent(t);
            yield return null;
            Assert.IsNotNull(go.GetComponent(t));
            Object.Destroy(go);
        }
    }
}
