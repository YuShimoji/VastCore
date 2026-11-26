using NUnit.Framework;
using UnityEngine;
using Vastcore.RegionMap;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// 表面の写真素材の取り込み（RegionSurfaceMaps）が、素材のファイル名を読み分け、配列に入れる 3 枚を正しい向きと大きさで作ることを確かめる。
    /// </summary>
    public class RegionSurfaceMapsTests
    {
        [TestCase("Ground054_1K-JPG_Color.jpg", RegionSurfaceMaps.MapKind.Color)]
        [TestCase("Ground054_1K-JPG_NormalGL.jpg", RegionSurfaceMaps.MapKind.NormalGL)]
        [TestCase("Ground054_1K-JPG_NormalDX.jpg", RegionSurfaceMaps.MapKind.NormalDX)]
        [TestCase("Ground054_1K-JPG_Displacement.jpg", RegionSurfaceMaps.MapKind.Height)]
        [TestCase("Ground054_1K-JPG_Roughness.jpg", RegionSurfaceMaps.MapKind.Roughness)]
        [TestCase("Ground054_1K-JPG_AmbientOcclusion.jpg", RegionSurfaceMaps.MapKind.Occlusion)]
        [TestCase("red_sandstone_wall_diff_2k.jpg", RegionSurfaceMaps.MapKind.Color)]
        [TestCase("red_sandstone_wall_nor_gl_2k.png", RegionSurfaceMaps.MapKind.NormalGL)]
        [TestCase("red_sandstone_wall_nor_dx_2k.png", RegionSurfaceMaps.MapKind.NormalDX)]
        [TestCase("red_sandstone_wall_disp_2k.png", RegionSurfaceMaps.MapKind.Height)]
        [TestCase("red_sandstone_wall_arm_2k.jpg", RegionSurfaceMaps.MapKind.Arm)]
        [TestCase("surface-normal.png", RegionSurfaceMaps.MapKind.Generated)]
        [TestCase("Ground054.png", RegionSurfaceMaps.MapKind.None)]
        [TestCase("Ground054_1K-JPG_Color.exr", RegionSurfaceMaps.MapKind.None)]
        public void Classify_ReadsAmbientCgAndPolyHavenNames(string fileName, RegionSurfaceMaps.MapKind expected)
        {
            Assert.AreEqual(expected, RegionSurfaceMaps.Classify(fileName));
        }

        [Test]
        public void SurfaceRoles_AddDepositAndWashAfterTheFirstSix()
        {
            int roles = System.Enum.GetValues(typeof(RegionSurfaceRole)).Length;
            Assert.AreEqual(8, roles);
            Assert.AreEqual(roles, RegionSurfaceMaps.Folders.Length);
            Assert.AreEqual(roles, RegionSurfaceSettings.Defaults().Length);
            // 先の 6 役の番号とフォルダは変えない（シェーダーの番号と、置かれた素材の場所）
            Assert.AreEqual("LooseRock", RegionSurfaceMaps.Folders[(int)RegionSurfaceRole.LooseRock]);
            Assert.AreEqual(5, (int)RegionSurfaceRole.LooseRock);
            Assert.AreEqual("Deposit", RegionSurfaceMaps.Folders[(int)RegionSurfaceRole.Deposit]);
            Assert.AreEqual("Wash", RegionSurfaceMaps.Folders[(int)RegionSurfaceRole.Wash]);
            foreach (RegionSurfaceSettings.Entry entry in RegionSurfaceSettings.Defaults())
                Assert.Greater(entry.TileMetres, 0f);
        }

        [Test]
        public void SedimentLayers_PackDepositWashScreeAndCoarse()
        {
            var data = new RegionData(new RegionSpec { Resolution = 2, SizeKm = 0.002f });
            data.Deposit = new[] { 1f, 0f, 0.5f, 0f };
            data.Wear = new[] { 0f, 0.4f, 0f, 0f };
            data.SedimentFlow = new[] { 0f, 0.2f, 0f, 0.6f };
            data.Scree = new[] { 0f, 0f, 1f, 0f };
            data.Coarse = new[] { 0f, 0f, 0.25f, 2f };
            Color32[] pixels = RegionSedimentLayers.Pack(data);
            Assert.AreEqual(4, pixels.Length);
            Assert.AreEqual(new Color32(255, 0, 0, 0), pixels[0]);
            // 流路は、通り道と削れの大きい方
            Assert.AreEqual(102, pixels[1].g);
            Assert.AreEqual(new Color32(128, 0, 255, 64), pixels[2]);
            Assert.AreEqual(153, pixels[3].g);
            Assert.AreEqual(255, pixels[3].a);

            // 地図の無い窓は結び付けない（シェーダーは今の描き方のまま）
            Shader shader = Resources.Load<Shader>(RegionTerrainView.ShaderResource);
            Assert.IsNotNull(shader);
            var material = new Material(shader);
            try
            {
                Assert.IsFalse(RegionSedimentLayers.Bind(material, new RegionData(new RegionSpec { Resolution = 2, SizeKm = 0.002f }), 0f, 0f, 1f));
                Assert.IsTrue(RegionSedimentLayers.Bind(material, data, 10f, 20f, 1f));
                Assert.AreEqual(new Vector4(10f, 20f, 0.5f, 0.25f), material.GetVector(RegionSedimentLayers.RectProperty));
                Object.DestroyImmediate(material.GetTexture(RegionSedimentLayers.MapProperty));
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void ToSquare_RepeatsAWideImageVerticallyToKeepItsProportions()
        {
            // 4×2 の画像（行ごとに値が違う）は、縦に 2 回並べた 4×4 になる
            var pixels = new Color[8];
            for (int i = 0; i < 8; i++) pixels[i] = new Color(i / 8f, 0f, 0f);
            Color[] square = RegionSurfaceMaps.ToSquare(pixels, 4, 2, 4);
            Assert.AreEqual(16, square.Length);
            for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                Assert.AreEqual(pixels[(y % 2) * 4 + x].r, square[y * 4 + x].r, 1e-6f);
        }

        [Test]
        public void ToSquare_AveragesWhenShrinking()
        {
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = new Color((i % 4) / 3f, 0f, 0f);
            Color[] half = RegionSurfaceMaps.ToSquare(pixels, 4, 4, 2);
            Assert.AreEqual(0.5f * (0f + 1f / 3f), half[0].r, 1e-5f);
            Assert.AreEqual(0.5f * (2f / 3f + 1f), half[1].r, 1e-5f);
        }

        [Test]
        public void Slopes_OfARampMatchItsGradient()
        {
            // 右へ 1 画素ごとに 1 mm 上がる面（1 画素 = 1 cm）の傾きは 0.1、繰り返しの継ぎ目から離れた所で
            const int size = 64;
            var height = new float[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                height[y * size + x] = 0.001f * x;
            Vector2[] slopes = RegionSurfaceMaps.Slopes(height, size, 0.01f);
            Vector2 middle = slopes[32 * size + 32];
            Assert.AreEqual(0.1f, middle.x, 1e-4f);
            Assert.AreEqual(0f, middle.y, 1e-6f);
        }

        [Test]
        public void Normals_RoundTripThroughTheEncodedImage()
        {
            var slopes = new[] { new Vector2(0.3f, -0.2f), Vector2.zero };
            Color[] encoded = RegionSurfaceMaps.EncodeNormals(slopes);
            Vector2[] decoded = RegionSurfaceMaps.SlopesFromNormals(encoded, false);
            Assert.AreEqual(0.3f, decoded[0].x, 1e-4f);
            Assert.AreEqual(-0.2f, decoded[0].y, 1e-4f);
            Assert.AreEqual(0f, decoded[1].magnitude, 1e-5f);
            // DirectX 向きの画像は緑が逆なので、同じ画像を DX として読むと縦の傾きが反転する
            Assert.AreEqual(0.2f, RegionSurfaceMaps.SlopesFromNormals(encoded, true)[0].y, 1e-4f);
        }

        [Test]
        public void Detail_KeepsVariationAroundAHalfMean()
        {
            var color = new[] { new Color(0.2f, 0.4f, 0.1f), new Color(0.6f, 0.4f, 0.3f) };
            Color[] detail = RegionSurfaceMaps.Detail(color);
            Assert.AreEqual(0.25f, detail[0].r, 1e-5f);
            Assert.AreEqual(0.75f, detail[1].r, 1e-5f);
            Assert.AreEqual(0.5f, detail[0].g, 1e-5f);
            Assert.AreEqual(0.5f, 0.5f * (detail[0].b + detail[1].b), 1e-5f);
        }

        [Test]
        public void HeightFromColor_HasZeroMeanAndTheRequestedSpread()
        {
            const int size = 64;
            var color = new Color[size * size];
            var random = new System.Random(3);
            for (int i = 0; i < color.Length; i++)
            {
                float v = (float)random.NextDouble();
                color[i] = new Color(v, v, v);
            }
            float[] height = RegionSurfaceMaps.HeightFromColor(color, size, 0.02f, 0.04f);
            double sum = 0, squares = 0;
            foreach (float h in height) { sum += h; squares += h * h; }
            double mean = sum / height.Length;
            Assert.AreEqual(0.0, mean, 5e-4);
            Assert.AreEqual(0.01, System.Math.Sqrt(squares / height.Length), 0.002);
        }
    }
}
