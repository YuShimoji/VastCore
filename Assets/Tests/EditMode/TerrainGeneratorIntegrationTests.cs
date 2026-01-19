using System.Collections;
using NUnit.Framework;
using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// TerrainGenerator と HeightMapGenerator の統合テスト
    /// 実際に Terrain を生成し、基本的な連携が機能していることを検証する。
    /// </summary>
    [TestFixture]
    public class TerrainGeneratorIntegrationTests
    {
        private GameObject _testObject;
        private TerrainGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestTerrainGenerator_Integration");
            _generator = _testObject.AddComponent<TerrainGenerator>();

            // 小さめの設定でテスト（パフォーマンス・安定性のため）
            _generator.Width = 100;
            _generator.Height = 100;
            _generator.Depth = 50;
            _generator.Resolution = 33;

            // マテリアルに依存しないよう、標準シェーダーで簡易マテリアルを設定
            var shader = Shader.Find("Standard");
            if (shader != null)
            {
                _generator.TerrainMaterial = new Material(shader);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }

            // 生成された Terrain GameObject をクリーンアップ
            var terrains = Object.FindObjectsByType<UnityEngine.Terrain>(FindObjectsSortMode.None);
            foreach (var terrain in terrains)
            {
                if (terrain != null)
                {
                    Object.DestroyImmediate(terrain.gameObject);
                }
            }
        }

        [Test]
        public void GenerateTerrain_NoiseMode_CreatesTerrainWithCorrectSize()
        {
            // Arrange
            _generator.GenerationMode = TerrainGenerationMode.Noise;

            // Act
            IEnumerator routine = _generator.GenerateTerrain();
            while (routine.MoveNext()) { }

            // Assert
            Assert.IsNotNull(_generator.GeneratedTerrain, "GeneratedTerrain should not be null after generation.");

            var data = _generator.GeneratedTerrain.terrainData;
            Assert.AreEqual(_generator.Resolution, data.heightmapResolution, "Heightmap resolution should match generator setting.");

            var expectedSize = new Vector3(_generator.Width, _generator.Depth, _generator.Height);
            Assert.AreEqual(expectedSize, data.size, "TerrainData size should match generator settings.");
        }

        [Test]
        public void GenerateTerrain_HeightMapMode_UsesProvidedHeightMap()
        {
            // Arrange: シンプルな高さマップテクスチャを用意
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var colors = new Color[16];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.black;
            }
            // 一部のピクセルだけ白にすることで非ゼロ高さを期待
            colors[5] = Color.white;
            colors[10] = Color.gray;
            texture.SetPixels(colors);
            texture.Apply();

            _generator.GenerationMode = TerrainGenerationMode.HeightMap;
            _generator.HeightMap = texture;
            _generator.HeightMapScale = 1f;
            _generator.HeightMapOffset = 0f;

            // Act
            IEnumerator routine = _generator.GenerateTerrain();
            while (routine.MoveNext()) { }

            // Assert
            Assert.IsNotNull(_generator.GeneratedTerrain, "GeneratedTerrain should not be null after heightmap generation.");
            var data = _generator.GeneratedTerrain.terrainData;

            bool hasNonZeroHeight = false;
            int res = data.heightmapResolution;
            for (int y = 0; y < res && !hasNonZeroHeight; y++)
            {
                for (int x = 0; x < res && !hasNonZeroHeight; x++)
                {
                    if (data.GetHeight(x, y) > 0f)
                    {
                        hasNonZeroHeight = true;
                    }
                }
            }

            Assert.IsTrue(hasNonZeroHeight, "Generated terrain should contain non-zero heights when a heightmap is provided.");
        }

        [Test]
        public void GetHighestPoint_ReturnsNonZeroWorldPositionAfterGeneration()
        {
            // Arrange
            _generator.GenerationMode = TerrainGenerationMode.Noise;

            // Act
            IEnumerator routine = _generator.GenerateTerrain();
            while (routine.MoveNext()) { }

            Vector3 highestPoint = _generator.GetHighestPoint();

            // Assert
            Assert.AreNotEqual(Vector3.zero, highestPoint, "Highest point should not be the zero vector after terrain generation.");
        }
    }
}
