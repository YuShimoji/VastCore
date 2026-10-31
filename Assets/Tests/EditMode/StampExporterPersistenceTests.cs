using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using Vastcore.Editor.Generation;
using Object = UnityEngine.Object;

namespace Vastcore.Tests.EditMode
{
    public class StampExporterPersistenceTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void Export_PreservesSourceAndReloadsPersistentMeshes(bool persistentSource)
        {
            string prefix = "StampExportTest_" + Guid.NewGuid().ToString("N");
            var source = ShapeGenerator.CreateShape(ShapeType.Cube);
            var child = ShapeGenerator.CreateShape(ShapeType.Cube);
            GameObject instance = null;
            try
            {
                source.name = prefix;
                source.transform.SetPositionAndRotation(new Vector3(3, 4, 5), Quaternion.Euler(10, 20, 30));
                source.transform.localScale = new Vector3(2, 3, 4);
                child.transform.SetParent(source.transform, false);
                child.gameObject.SetActive(false);
                var sourceFilter = source.GetComponent<MeshFilter>();
                if (persistentSource)
                {
                    var persistent = Object.Instantiate(sourceFilter.sharedMesh);
                    persistent.hideFlags = HideFlags.None;
                    AssetDatabase.CreateAsset(persistent, $"Assets/{prefix}_Source.asset");
                    sourceFilter.sharedMesh = persistent;
                    source.preserveMeshAssetOnDestroy = true;
                }
                var mesh = sourceFilter.sharedMesh;
                var vertices = mesh.vertices;
                var triangles = mesh.triangles;
                string materialPath = AssetDatabase.GetAssetPath(source.GetComponent<MeshRenderer>().sharedMaterial);
                var childMesh = child.GetComponent<MeshFilter>().sharedMesh;
                bool preserve = source.preserveMeshAssetOnDestroy;
                var collider = source.GetComponent<MeshCollider>();
                if (collider == null) collider = source.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;

                var definition = StampExporter.ExportAsStamp(source.gameObject);
                Assert.IsNotNull(definition);
                Assert.IsTrue(mesh != null, "Export cleanup destroyed the source mesh");
                Assert.AreSame(mesh, sourceFilter.sharedMesh);
                CollectionAssert.AreEqual(vertices, mesh.vertices);
                CollectionAssert.AreEqual(triangles, mesh.triangles);
                Assert.AreSame(childMesh, child.GetComponent<MeshFilter>().sharedMesh);
                Assert.IsTrue(childMesh != null, "Export cleanup destroyed the inactive child's mesh");
                Assert.AreEqual(preserve, source.preserveMeshAssetOnDestroy);
                Assert.AreEqual(new Vector3(3, 4, 5), source.transform.position);
                Assert.That(Quaternion.Angle(Quaternion.Euler(10, 20, 30), source.transform.rotation), Is.LessThan(0.001f));
                Assert.AreEqual(new Vector3(2, 3, 4), source.transform.localScale);

                string prefabPath = AssetDatabase.GetAssetPath(definition.Prefab);
                AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.AreEqual(0, prefab.GetComponentsInChildren<ProBuilderMesh>(true).Length);
                foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                    Assert.IsTrue(filter.sharedMesh != null && AssetDatabase.Contains(filter.sharedMesh), "Prefab mesh must be an asset");
                var savedMesh = prefab.GetComponent<MeshFilter>().sharedMesh;
                Assert.AreSame(savedMesh, prefab.GetComponent<MeshCollider>().sharedMesh);
                Assert.IsNotEmpty(materialPath);
                Assert.AreEqual(materialPath, AssetDatabase.GetAssetPath(prefab.GetComponent<MeshRenderer>().sharedMaterial));
                CollectionAssert.AreEqual(vertices, savedMesh.vertices);
                CollectionAssert.AreEqual(triangles, savedMesh.triangles);
                Assert.AreEqual(Vector3.zero, prefab.transform.position);
                Assert.That(Quaternion.Angle(Quaternion.identity, prefab.transform.rotation), Is.LessThan(0.001f));
                Assert.AreEqual(source.transform.localScale, prefab.transform.localScale);
                if (persistentSource) Assert.AreEqual($"Assets/{prefix}_Source.asset", AssetDatabase.GetAssetPath(savedMesh));

                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Assert.AreSame(savedMesh, instance.GetComponent<MeshFilter>().sharedMesh);
                Assert.AreEqual(mesh.bounds, instance.GetComponent<MeshFilter>().sharedMesh.bounds);
            }
            finally
            {
                if (instance != null) Object.DestroyImmediate(instance);
                if (source != null) Object.DestroyImmediate(source.gameObject);
                AssetDatabase.DeleteAsset($"Assets/{prefix}_Source.asset");
                foreach (string guid in AssetDatabase.FindAssets(prefix, new[] { "Assets/Resources/Stamps" }))
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (System.IO.Path.GetFileName(assetPath).StartsWith(prefix, StringComparison.Ordinal))
                        AssetDatabase.DeleteAsset(assetPath);
                }
            }
        }
    }
}
