using NUnit.Framework;
using UnityEngine;
using Vastcore.Terrain.Volumetric;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// VolumetricChunk のメッシュ寿命管理を検証するテスト。
    /// </summary>
    [TestFixture]
    public class VolumetricChunkMeshLifecycleTests
    {
        private GameObject _go;
        private VolumetricChunk _chunk;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("VolumetricChunkMeshLifecycleTests");
            _chunk = _go.AddComponent<VolumetricChunk>();
            _chunk.Initialize(null);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
                _go = null;
            }
        }

        [Test]
        public void BuildMesh_Rebuild_DestroysPreviousRuntimeMesh()
        {
            Mesh first = CreateTriangleMesh();
            Mesh second = CreateTriangleMesh();

            _chunk.BuildMesh(first);
            _chunk.BuildMesh(second);

            Assert.IsTrue(first == null, "First mesh should be destroyed on rebuild.");
            Assert.IsTrue(second != null, "Second mesh should remain alive.");
        }

        [Test]
        public void ClearMesh_DestroysCurrentRuntimeMesh()
        {
            Mesh mesh = CreateTriangleMesh();
            _chunk.BuildMesh(mesh);

            _chunk.ClearMesh();

            Assert.IsTrue(mesh == null, "Mesh should be destroyed on clear.");
        }

        private static Mesh CreateTriangleMesh()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(1f, 0f, 0f),
                new Vector3(0f, 1f, 0f)
            };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
