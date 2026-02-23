using NUnit.Framework;
using UnityEngine;
using Vastcore.Terrain.Volumetric;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// VolumetricDirtyRegionTracker のチャンク影響範囲計算を検証するテスト。
    /// </summary>
    [TestFixture]
    public class VolumetricDirtyRegionTrackerTests
    {
        [Test]
        public void MarkDirty_SingleChunkBounds_TracksOneChunk()
        {
            var tracker = new VolumetricDirtyRegionTracker();
            var bounds = new Bounds(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.2f, 0.2f, 0.2f));

            tracker.MarkDirty(bounds, 1f, Vector3.zero);

            Assert.AreEqual(1, tracker.Count);
        }

        [Test]
        public void MarkDirty_MultipleBounds_DeduplicatesOverlaps()
        {
            var tracker = new VolumetricDirtyRegionTracker();
            var a = new Bounds(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(1f, 1f, 1f));
            var b = new Bounds(new Vector3(0.75f, 0.75f, 0.75f), new Vector3(1f, 1f, 1f));

            tracker.MarkDirty(a, 1f, Vector3.zero);
            tracker.MarkDirty(b, 1f, Vector3.zero);

            Assert.AreEqual(8, tracker.Count);
        }

        [Test]
        public void ClearAll_RemovesAllDirtyChunks()
        {
            var tracker = new VolumetricDirtyRegionTracker();
            var bounds = new Bounds(Vector3.zero, new Vector3(4f, 4f, 4f));

            tracker.MarkDirty(bounds, 1f, Vector3.zero);
            Assert.Greater(tracker.Count, 0);

            tracker.ClearAll();
            Assert.AreEqual(0, tracker.Count);
        }
    }
}
