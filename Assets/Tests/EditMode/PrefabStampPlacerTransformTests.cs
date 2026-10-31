using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Vastcore.Terrain.DualGrid;

namespace Vastcore.Tests.EditMode
{
    public class PrefabStampPlacerTransformTests
    {
        private class RecordingHeightSampler : IHeightSampler
        {
            public Vector2 LastPosition;
            public float SampleHeight(float x, float z)
            {
                LastPosition = new Vector2(x, z);
                return x * 0.01f + z * 0.02f;
            }
        }

        [TestCase(1f)]
        [TestCase(0.75f)]
        public void Placement_UsesGridParentCoordinatesAndPreservesPrefabDimensions(float variation)
        {
            var parent = new GameObject("GridParent");
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var definition = ScriptableObject.CreateInstance<PrefabStampDefinition>();
            var placer = new PrefabStampPlacer(parent.transform);
            try
            {
                parent.transform.position = new Vector3(132, 0, 232);
                prefab.transform.localScale = new Vector3(5, 6, 7);
                var serialized = new SerializedObject(definition);
                serialized.FindProperty("m_Prefab").objectReferenceValue = prefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var grid = new IrregularGrid();
                grid.GenerateGrid(1);
                grid.ApplyRelaxation(42, 0f, true);
                Assert.That(grid.Cells.Count, Is.GreaterThan(0));
                var cell = grid.Cells[0];
                var expected = parent.transform.TransformPoint(cell.GetCenter());
                var sampler = new RecordingHeightSampler();
                placer.SetHeightSampler(sampler);
                var placement = new StampPlacement(0, definition, cell, 0f, 2, variation);
                var instance = placer.Instantiate(placement, grid);
                Assert.IsNotNull(instance);
                TestContext.WriteLine($"Cell={cell.Id}, center={cell.GetCenter():F6}, actual position={instance.transform.position:F6}, expected XZ={expected:F6}, actual scale={instance.transform.localScale:F6}, expected scale={prefab.transform.localScale * variation:F6}");
                Assert.That(instance.transform.position.x, Is.EqualTo(expected.x).Within(0.001f));
                Assert.That(instance.transform.position.z, Is.EqualTo(expected.z).Within(0.001f));
                Assert.That(Vector2.Distance(sampler.LastPosition, new Vector2(expected.x, expected.z)), Is.LessThan(0.001f));
                Assert.That(instance.transform.position.y, Is.EqualTo(expected.x * 0.01f + expected.z * 0.02f + 2f).Within(0.001f));
                Assert.That(Vector3.Distance(instance.transform.localScale, prefab.transform.localScale * variation), Is.LessThan(0.0001f));
            }
            finally
            {
                placer.DestroyAll();
                Object.DestroyImmediate(parent);
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(definition);
            }
        }
    }
}
