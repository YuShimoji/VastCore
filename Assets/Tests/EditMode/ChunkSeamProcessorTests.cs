using NUnit.Framework;
using UnityEngine;
using Vastcore.Terrain.Volumetric;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// ChunkSeamProcessor の境界スナップと量子化を検証するテスト。
    /// </summary>
    [TestFixture]
    public class ChunkSeamProcessorTests
    {
        [Test]
        public void Process_WithBorderSnap_SnapsXAndZToChunkBorder()
        {
            Mesh mesh = CreateMesh(
                new Vector3(0.001f, 0f, 31.999f),
                new Vector3(10f, 0f, 10f),
                new Vector3(31.9995f, 0f, 0.001f));

            var processor = new ChunkSeamProcessor(new ChunkSeamProcessor.Options
            {
                snapBorderVertices = true,
                quantizeVertices = false,
                vertexQuantizeStep = 0.01f,
                borderSnapEpsilon = 0.2f
            });

            processor.Process(mesh, 32f, 0.01f);
            Vector3[] vertices = mesh.vertices;

            Assert.AreEqual(0f, vertices[0].x);
            Assert.AreEqual(32f, vertices[0].z);
            Assert.AreEqual(32f, vertices[2].x);
            Assert.AreEqual(0f, vertices[2].z);
        }

        [Test]
        public void Process_WithQuantize_RoundsByStep()
        {
            Mesh mesh = CreateMesh(
                new Vector3(0.123f, 0.26f, 0.371f),
                new Vector3(0.456f, 0.58f, 0.789f),
                new Vector3(0.9f, 1.01f, 1.11f));

            var processor = new ChunkSeamProcessor(new ChunkSeamProcessor.Options
            {
                snapBorderVertices = false,
                quantizeVertices = true,
                vertexQuantizeStep = 0.5f,
                borderSnapEpsilon = 0.2f
            });

            processor.Process(mesh, 32f, 1f);
            Vector3[] vertices = mesh.vertices;

            Assert.AreEqual(0f, vertices[0].x);
            Assert.AreEqual(0.5f, vertices[0].y);
            Assert.AreEqual(0.5f, vertices[0].z);
        }

        private static Mesh CreateMesh(Vector3 a, Vector3 b, Vector3 c)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = new[] { a, b, c };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
