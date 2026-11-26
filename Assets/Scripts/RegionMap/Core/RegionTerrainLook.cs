using System;
using System.Threading.Tasks;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 決まった視点 1 つ。位置は、場面の中心（RegionLookSpec.CenterMapX・Y）からの東・南への距離（m）と、その場所の地面からの高さ（m）。
    /// </summary>
    [Serializable]
    public sealed class RegionLookViewpoint
    {
        public string Name;
        public float EyeEast, EyeSouth, EyeAbove;
        public float TargetEast, TargetSouth, TargetAbove;
        public float FovDegrees = 40f;

        public RegionLookViewpoint()
        {
        }

        public RegionLookViewpoint(string name, float eyeEast, float eyeSouth, float eyeAbove, float targetEast, float targetSouth, float targetAbove, float fov)
        {
            Name = name;
            EyeEast = eyeEast; EyeSouth = eyeSouth; EyeAbove = eyeAbove;
            TargetEast = targetEast; TargetSouth = targetSouth; TargetAbove = targetAbove;
            FovDegrees = fov;
        }
    }

    /// <summary>
    /// 見え方の想定（docs/02_design/REGION_TERRAIN_LOOK_SPEC.md）と見比べる場面の指定。
    /// 既定は seed 9 の南の乾いた平原（X43.61 Y76.92）。1 km 四方を 1 m の格子で作り、そのまわり 6 km 四方を粗い格子で作る
    /// （遠景は 2〜5 km 先を見るので、1 km の窓だけでは足りない）。
    /// </summary>
    [Serializable]
    public sealed class RegionLookSpec
    {
        public float CenterMapX = 43.61f;
        public float CenterMapY = 76.92f;

        /// <summary>細かく作る窓の一辺（km）。実際の一辺は、外側の格子の線に合わせて少し変わる。</summary>
        public float InnerKm = 1f;

        /// <summary>外側の窓の一辺（km）。</summary>
        public float OuterKm = 6f;

        /// <summary>内側・外側それぞれの一辺のセル数。</summary>
        public int Resolution = 1024;

        public float Mesa = 1f;
        public bool Rocks = true;
        public bool Vegetation = true;

        // 3 つの視点は、既定の場所（seed 9 の X43.61 Y76.92）に合わせて決めた値。太陽は北西から差すので、
        // 西と北を向いた崖に光が斜めに当たる向きから見る。場所を変えたら、視点も決め直す。

        /// <summary>遠景：西の平原の地上 4 m から、2.5 km 先の台地・ビュート・尖塔の並びを見る。</summary>
        public RegionLookViewpoint Far = new RegionLookViewpoint("far", -2500f, 200f, 4f, 0f, 0f, 90f, 30f);

        /// <summary>中景：南西の平地の地上 2 m から、300〜400 m 先のビュート 3 つと、左の台地の崖を見る。</summary>
        public RegionLookViewpoint Mid = new RegionLookViewpoint("mid", -470f, 120f, 2f, -150f, -60f, 30f, 50f);

        /// <summary>近景：ビュートの南西の崖の足もと（岩肌から 15〜20 m）の岩屑の斜面に立ち、崖に沿って北を見る。</summary>
        public RegionLookViewpoint Near = new RegionLookViewpoint("near", -121f, -69f, 1.7f, -112f, -178f, 15f, 60f);

        public RegionLookViewpoint[] Viewpoints()
        {
            return new[] { Far, Mid, Near };
        }
    }

    /// <summary>
    /// 場面の部品 1 つ（外側の地形・内側の地形・岩）。座標は m で、X 東・Y 上・Z 南。外側の窓の北西の角が原点。
    /// UnityEngine に依存しない配列だけを持つ。
    /// </summary>
    public sealed class RegionLookPart
    {
        public string Name;

        /// <summary>地面の画像を描く元（真上から見た絵を Uv で貼る）。岩は null。</summary>
        public RegionData Data;

        public float[] Positions;
        public float[] Normals;

        /// <summary>頂点ごとの画像上の位置（0〜1）。岩は null（画像を貼らない）。</summary>
        public float[] Uv;

        public int[] Indices;

        /// <summary>頂点ごとの、日が当たる度合い（0〜1。地形と岩の影を先に計算したもの）。</summary>
        public float[] Sun;

        /// <summary>頂点ごとの、空の開け具合（0〜1。割れ目の中や崖の足もとで小さい）。</summary>
        public float[] Sky;

        /// <summary>頂点ごとの、岩肌の色にする度合い（0〜1。崖の面と岩は 1）。</summary>
        public float[] Rock;

        /// <summary>頂点ごとの岩肌の色のむら（0〜1。色に 0.66 + 0.34 × この値 を掛ける。地形は 0.5）。</summary>
        public float[] Tint;

        public int VertexCount
        {
            get { return Positions.Length / 3; }
        }

        public int TriangleCount
        {
            get { return Indices.Length / 3; }
        }
    }

    /// <summary>
    /// 見え方の想定と見比べる場面。外側の地形（内側の範囲は抜く）、内側の地形、岩の 3 つの部品と、決まった 3 つの視点を持つ。
    /// 内側の地形は、縁から 3 セル（外側のセル）ぶんの幅で外側の地形へなじませる（継ぎ目に段を作らない）。
    /// 日の当たり方は、Unity の影の届く距離（50 m）に頼らず、高さの格子から先に計算して頂点に持たせる。
    /// </summary>
    public sealed class RegionLookScene
    {
        /// <summary>地形の簡易描画と、砂岩の表面材質を適用しない場所で使う岩肌の色。</summary>
        public const float RockRed = 0.77f, RockGreen = 0.45f, RockBlue = 0.30f;

        /// <summary>岩肌に入れる、高さに沿った縞の強さ（仮の値）。</summary>
        public const float BandAmount = 0.06f;

        public RegionLookSpec Spec;
        public RegionDetail Outer;
        public RegionDetail Inner;
        public RegionRockField InnerRocks;
        public RegionRockField OuterRocks;
        public RegionLookPart OuterTerrain;
        public RegionLookPart InnerTerrain;
        public RegionLookPart Rocks;

        public float OuterCell, InnerCell;

        /// <summary>内側の地形の範囲（場面の座標）。</summary>
        public float InnerMinX, InnerMinZ, InnerMaxX, InnerMaxZ;

        /// <summary>場面の中心（RegionLookSpec.CenterMapX・Y）の、場面の座標。</summary>
        public float CenterX, CenterZ;

        /// <summary>
        /// 影を落とす高さの格子（描画時に GPU が太陽の方へたどる。RegionTerrainShadows）。
        /// 内側は 1.2 m 以上の岩を足した高さで、頂点 (i, j) は (InnerMinX + i * InnerCell, InnerMinZ + j * InnerCell)。
        /// 外側は頂点 (i, j) が ((i + 0.5) * OuterCell, (j + 0.5) * OuterCell)。頂点に持たせる日当たり（Sun）と同じ入力。
        /// </summary>
        public float[] ShadowInner, ShadowOuter;
        public int ShadowInnerSize, ShadowOuterSize;

        /// <summary>太陽の方を向く単位ベクトル（北西・高さ 38 度。鳥瞰図と同じ）。</summary>
        public static void SunDirection(out float x, out float y, out float z)
        {
            double altitude = RegionVolumePatch.SunAltitudeDegrees * Math.PI / 180.0;
            x = -(float)Math.Cos(altitude) * 0.70710678f;
            y = (float)Math.Sin(altitude);
            z = -(float)Math.Cos(altitude) * 0.70710678f;
        }

        public RegionLookPart[] Parts()
        {
            return Rocks != null ? new[] { OuterTerrain, InnerTerrain, Rocks } : new[] { OuterTerrain, InnerTerrain };
        }

        /// <summary>地面の高さ（m）。内側の範囲は内側の地形、その外は外側の地形から読む。海は 0。</summary>
        public float HeightAt(float x, float z)
        {
            float value;
            if (x >= InnerMinX && z >= InnerMinZ && x <= InnerMaxX && z <= InnerMaxZ)
                value = Sample(Inner.Data.Height, Inner.Data.Resolution, (x - InnerMinX) / InnerCell, (z - InnerMinZ) / InnerCell);
            else
                value = Sample(Outer.Data.Height, Outer.Data.Resolution, x / OuterCell - 0.5f, z / OuterCell - 0.5f);
            return value > 0f ? value : 0f;
        }

        /// <summary>視点を場面の座標に直す。eye・target は長さ 3（X・Y・Z）。</summary>
        public void Resolve(RegionLookViewpoint viewpoint, float[] eye, float[] target)
        {
            eye[0] = CenterX + viewpoint.EyeEast;
            eye[2] = CenterZ + viewpoint.EyeSouth;
            eye[1] = HeightAt(eye[0], eye[2]) + viewpoint.EyeAbove;
            target[0] = CenterX + viewpoint.TargetEast;
            target[2] = CenterZ + viewpoint.TargetSouth;
            target[1] = HeightAt(target[0], target[2]) + viewpoint.TargetAbove;
        }

        public static RegionLookScene Build(RegionData region, RegionLookSpec spec)
        {
            if (region == null) throw new ArgumentNullException("region");
            if (spec == null) spec = new RegionLookSpec();
            RegionDetail outer = RegionDetailBuilder.Build(region, new RegionDetailSpec
            {
                CenterMapX = spec.CenterMapX, CenterMapY = spec.CenterMapY, SizeKm = spec.OuterKm, Resolution = spec.Resolution, Mesa = spec.Mesa,
            });
            int n = outer.Data.Resolution;
            float outerCell = outer.Data.CellMeters;
            float metersPerMapUnit = region.SizeKm * 1000f / RegionData.MapUnits;
            float centerX = (spec.CenterMapX - outer.OriginMapX) * metersPerMapUnit;
            float centerZ = (spec.CenterMapY - outer.OriginMapY) * metersPerMapUnit;

            // 内側の窓は、外側のセル k 個ぶん。両端の頂点を、外側の頂点の線に重ねる
            int k = (int)Math.Round(spec.InnerKm * 1000f / outerCell);
            if (k < 8) k = 8; else if (k > n - 8) k = n - 8;
            int ka = Clamp((int)Math.Round(centerX / outerCell - 0.5f - k * 0.5f), 2, n - 3 - k);
            int la = Clamp((int)Math.Round(centerZ / outerCell - 0.5f - k * 0.5f), 2, n - 3 - k);
            int m = Math.Max(64, spec.Resolution);
            float span = k * outerCell;
            float innerCell = span / (m - 1);
            float minX = (ka + 0.5f) * outerCell;
            float minZ = (la + 0.5f) * outerCell;
            RegionDetail inner = RegionDetailBuilder.Build(region, new RegionDetailSpec
            {
                CenterMapX = outer.OriginMapX + (minX + (m * 0.5f - 0.5f) * innerCell) / metersPerMapUnit,
                CenterMapY = outer.OriginMapY + (minZ + (m * 0.5f - 0.5f) * innerCell) / metersPerMapUnit,
                SizeKm = m * innerCell / 1000f, Resolution = m, Mesa = spec.Mesa,
            });

            var scene = new RegionLookScene
            {
                Spec = spec, Outer = outer, Inner = inner, OuterCell = outerCell, InnerCell = innerCell,
                InnerMinX = minX, InnerMinZ = minZ, InnerMaxX = minX + span, InnerMaxZ = minZ + span,
                CenterX = centerX, CenterZ = centerZ,
            };

            // 内側の縁を、外側の地形へなじませる
            float[] outerHeight = outer.Data.Height;
            float[] innerHeight = inner.Data.Height;
            float band = 3f * outerCell;
            Parallel.For(0, m, y =>
            {
                for (int x = 0; x < m; x++)
                {
                    int edge = Math.Min(Math.Min(x, m - 1 - x), Math.Min(y, m - 1 - y));
                    float t = RegionNoise.Smooth(0f, band, edge * innerCell);
                    if (t >= 1f) continue;
                    float coarse = Sample(outerHeight, n, (minX + x * innerCell) / outerCell - 0.5f, (minZ + y * innerCell) / outerCell - 0.5f);
                    int i = x + y * m;
                    innerHeight[i] = coarse + (innerHeight[i] - coarse) * t;
                }
            });

            double worldInnerX = inner.OriginMapX * (double)metersPerMapUnit + 0.5 * innerCell;
            double worldInnerZ = inner.OriginMapY * (double)metersPerMapUnit + 0.5 * innerCell;
            double worldOuterX = outer.OriginMapX * (double)metersPerMapUnit + 0.5 * outerCell;
            double worldOuterZ = outer.OriginMapY * (double)metersPerMapUnit + 0.5 * outerCell;
            if (spec.Rocks)
            {
                scene.InnerRocks = RegionRockField.Build(inner.Data, worldInnerX, worldInnerZ, minX, minZ, 0f, null);
                // 外側は遠くからしか見ないので、大きい岩だけ置く
                scene.OuterRocks = RegionRockField.Build(outer.Data, worldOuterX, worldOuterZ, 0.5f * outerCell, 0.5f * outerCell, 4f,
                    new[] { scene.InnerMinX, scene.InnerMinZ, scene.InnerMaxX, scene.InnerMaxZ });
            }

            // 影を落とす高さ：内側は、1.2 m 以上の岩を地形に足した物（岩の影と、岩の足もとの暗さが地面に出る）
            float[] caster = innerHeight;
            if (scene.InnerRocks != null && scene.InnerRocks.Rocks.Length > 0)
            {
                caster = (float[])innerHeight.Clone();
                foreach (RegionRock rock in scene.InnerRocks.Rocks)
                {
                    if (rock.Size < 1.2f) continue;
                    float radius = 0.42f * rock.Size;
                    float lx = (rock.X - minX) / innerCell, lz = (rock.Z - minZ) / innerCell;
                    int reach = (int)Math.Ceiling(radius / innerCell);
                    for (int dz = -reach; dz <= reach; dz++)
                    {
                        for (int dx = -reach; dx <= reach; dx++)
                        {
                            int x = (int)(lx + 0.5f) + dx, z = (int)(lz + 0.5f) + dz;
                            if (x < 0 || z < 0 || x >= m || z >= m) continue;
                            float ex = (x - lx) * innerCell, ez = (z - lz) * innerCell;
                            if (ex * ex + ez * ez > radius * radius) continue;
                            float top = rock.Y + rock.Top * 0.85f;
                            if (top > caster[x + z * m]) caster[x + z * m] = top;
                        }
                    }
                }
            }

            scene.ShadowInner = caster;
            scene.ShadowInnerSize = m;
            scene.ShadowOuter = outerHeight;
            scene.ShadowOuterSize = n;

            float[] outerSun = BakeSun(outerHeight, outerHeight, n, outerCell, null, 0, 0f, 0f, 0f);
            float[] outerSky = BakeSky(outerHeight, outerHeight, n, outerCell, new[] { 6f, 12f, 24f, 48f, 96f });
            float[] innerRadii = { 1.5f, 3f, 6f, 12f, 24f, 48f };
            float[] innerSun = BakeSun(innerHeight, caster, m, innerCell, outerHeight, n, outerCell, minX, minZ);
            float[] innerSky = BakeSky(innerHeight, caster, m, innerCell, innerRadii);
            // 岩そのものの明るさは、岩を足す前の地形から読む（自分の影に入らないように）
            float[] plainSun = caster == innerHeight ? innerSun : BakeSun(innerHeight, innerHeight, m, innerCell, outerHeight, n, outerCell, minX, minZ);
            float[] plainSky = caster == innerHeight ? innerSky : BakeSky(innerHeight, innerHeight, m, innerCell, innerRadii);

            scene.OuterTerrain = TerrainPart("Outer Terrain", outer.Data, 0.5f * outerCell, 0.5f * outerCell, outerCell, outerSun, outerSky, ka, ka + k, la, la + k);
            scene.InnerTerrain = TerrainPart("Inner Terrain", inner.Data, minX, minZ, innerCell, innerSun, innerSky, -1, -1, -1, -1);
            if (spec.Rocks)
            {
                scene.Rocks = RockPart(scene.InnerRocks, plainSun, plainSky, m, minX, minZ, innerCell,
                    scene.OuterRocks, outerSun, outerSky, n, 0.5f * outerCell, 0.5f * outerCell, outerCell);
            }
            return scene;
        }

        /// <summary>
        /// 高さの格子を地形の部品にする。頂点 (i, j) は (minX + i * cell, 高さ, minZ + j * cell)。
        /// hole を渡すと、頂点の番号がその範囲に入る四角を抜く（内側の地形が入る所）。
        /// </summary>
        private static RegionLookPart TerrainPart(string name, RegionData data, float minX, float minZ, float cell, float[] sun, float[] sky,
            int holeX0, int holeX1, int holeZ0, int holeZ1)
        {
            int n = data.Resolution;
            float[] height = data.Height;
            var positions = new float[n * n * 3];
            var normals = new float[n * n * 3];
            var uv = new float[n * n * 2];
            var rock = new float[n * n];
            var tint = new float[n * n];
            float[] cliff = data.Cliff;
            float[] talus = data.Talus;
            Parallel.For(0, n, j =>
            {
                for (int i = 0; i < n; i++)
                {
                    int v = i + j * n;
                    positions[v * 3] = minX + i * cell;
                    positions[v * 3 + 1] = Ground(height[v]);
                    positions[v * 3 + 2] = minZ + j * cell;
                    int left = (i > 0 ? i - 1 : i) + j * n, right = (i < n - 1 ? i + 1 : i) + j * n;
                    int up = i + (j > 0 ? j - 1 : j) * n, down = i + (j < n - 1 ? j + 1 : j) * n;
                    float slopeX = (Ground(height[right]) - Ground(height[left])) / ((right - left) * cell);
                    float slopeZ = (Ground(height[down]) - Ground(height[up])) / ((down - up) / n * cell);
                    float length = (float)Math.Sqrt(slopeX * slopeX + 1f + slopeZ * slopeZ);
                    normals[v * 3] = -slopeX / length;
                    normals[v * 3 + 1] = 1f / length;
                    normals[v * 3 + 2] = -slopeZ / length;
                    uv[v * 2] = i / (float)(n - 1);
                    uv[v * 2 + 1] = j / (float)(n - 1);
                    // 崖の面は岩肌の色。岩屑の斜面も、同じ岩が砕けた物なので、半分ほど岩肌の色へ寄せる
                    float amount = cliff != null ? cliff[v] : 0f;
                    if (talus != null) amount = Math.Max(amount, 0.55f * Math.Min(1f, 1.3f * talus[v]));
                    rock[v] = amount;
                    tint[v] = 0.5f;
                }
            });

            bool hole = holeX0 >= 0;
            int quads = (n - 1) * (n - 1) - (hole ? (holeX1 - holeX0) * (holeZ1 - holeZ0) : 0);
            var indices = new int[quads * 6];
            int t = 0;
            for (int j = 0; j < n - 1; j++)
            {
                for (int i = 0; i < n - 1; i++)
                {
                    if (hole && i >= holeX0 && i < holeX1 && j >= holeZ0 && j < holeZ1) continue;
                    int a = i + j * n, b = a + 1, c = a + n, d = c + 1;
                    // 高さの差が小さい方の対角線で割る（RegionTerrainMesh と同じ）
                    if (Math.Abs(positions[a * 3 + 1] - positions[d * 3 + 1]) <= Math.Abs(positions[b * 3 + 1] - positions[c * 3 + 1]))
                    {
                        indices[t++] = a; indices[t++] = b; indices[t++] = d;
                        indices[t++] = a; indices[t++] = d; indices[t++] = c;
                    }
                    else
                    {
                        indices[t++] = a; indices[t++] = b; indices[t++] = c;
                        indices[t++] = b; indices[t++] = d; indices[t++] = c;
                    }
                }
            }
            return new RegionLookPart
            {
                Name = name, Data = data, Positions = positions, Normals = normals, Uv = uv, Indices = indices,
                Sun = sun, Sky = sky, Rock = rock, Tint = tint,
            };
        }

        private static RegionLookPart RockPart(RegionRockField inner, float[] innerSun, float[] innerSky, int innerN, float innerMinX, float innerMinZ, float innerCell,
            RegionRockField outer, float[] outerSun, float[] outerSky, int outerN, float outerMinX, float outerMinZ, float outerCell)
        {
            int innerCount = inner.VertexCount, outerCount = outer.VertexCount;
            int count = innerCount + outerCount;
            var part = new RegionLookPart
            {
                Name = "Rocks",
                Positions = new float[count * 3], Normals = new float[count * 3], Indices = new int[count],
                Sun = new float[count], Sky = new float[count], Rock = new float[count], Tint = new float[count],
            };
            Array.Copy(inner.Positions, 0, part.Positions, 0, innerCount * 3);
            Array.Copy(outer.Positions, 0, part.Positions, innerCount * 3, outerCount * 3);
            Array.Copy(inner.Normals, 0, part.Normals, 0, innerCount * 3);
            Array.Copy(outer.Normals, 0, part.Normals, innerCount * 3, outerCount * 3);
            Array.Copy(inner.Tint, 0, part.Tint, 0, innerCount);
            Array.Copy(outer.Tint, 0, part.Tint, innerCount, outerCount);
            for (int i = 0; i < count; i++)
            {
                part.Indices[i] = i;
                part.Rock[i] = 1f;
            }
            Light(part, 0, inner, innerSun, innerSky, innerN, innerMinX, innerMinZ, innerCell);
            Light(part, innerCount, outer, outerSun, outerSky, outerN, outerMinX, outerMinZ, outerCell);
            return part;
        }

        /// <summary>岩の頂点の明るさ：岩の置かれた地面の日当たりと空の開け具合を読み、地面に近い所ほど暗くする。</summary>
        private static void Light(RegionLookPart part, int first, RegionRockField field, float[] sun, float[] sky, int n, float minX, float minZ, float cell)
        {
            foreach (RegionRock rock in field.Rocks)
            {
                float lx = (rock.X - minX) / cell, lz = (rock.Z - minZ) / cell;
                float direct = Sample(sun, n, lx, lz);
                float open = Sample(sky, n, lx, lz);
                for (int v = 0; v < rock.VertexCount; v++)
                {
                    int index = rock.FirstVertex + v;
                    part.Sun[first + index] = direct;
                    part.Sky[first + index] = open * (0.5f + 0.5f * field.Lift[index]);
                }
            }
        }

        /// <summary>
        /// 日が当たる度合い（0〜1）を、セルごとに太陽の方へ進んで調べる。ground は光を受ける面、caster は影を落とす面の高さ。
        /// 格子の外へ出たら、far（外側の地形。頂点 k が (k + 0.5) * farCell）を渡していればそちらで続ける。
        /// 影の縁は、遮る物から離れるほどぼける。
        /// </summary>
        private static float[] BakeSun(float[] ground, float[] caster, int n, float cell, float[] far, int farN, float farCell, float minX, float minZ)
        {
            float sunX, sunY, sunZ;
            SunDirection(out sunX, out sunY, out sunZ);
            float flat = (float)Math.Sqrt(sunX * sunX + sunZ * sunZ);
            float hx = sunX / flat, hz = sunZ / flat, rise = sunY / flat;
            float top = float.MinValue, farTop = float.MinValue;
            for (int i = 0; i < caster.Length; i++) if (caster[i] > top) top = caster[i];
            if (far != null) for (int i = 0; i < far.Length; i++) if (far[i] > farTop) farTop = far[i];
            var result = new float[n * n];
            Parallel.For(0, n, y =>
            {
                for (int x = 0; x < n; x++)
                {
                    float start = Ground(ground[x + y * n]) + 0.35f;
                    float visible = 1f;
                    float s = 1.2f * cell;
                    bool inside = true;
                    while (visible > 0f)
                    {
                        float ray = start + s * rise;
                        if (inside)
                        {
                            float px = x + hx * s / cell, pz = y + hz * s / cell;
                            if (px < 0f || pz < 0f || px > n - 1 || pz > n - 1)
                            {
                                if (far == null) break;
                                inside = false;
                                continue;
                            }
                            if (ray > top && (far == null || ray > farTop)) break;
                            float clear = (ray - Sample(caster, n, px, pz)) / (0.03f * s + 0.3f);
                            if (clear < visible) visible = clear;
                            s += cell;
                        }
                        else
                        {
                            if (ray > farTop) break;
                            float fx = (minX + x * cell + hx * s) / farCell - 0.5f, fz = (minZ + y * cell + hz * s) / farCell - 0.5f;
                            if (fx < 0f || fz < 0f || fx > farN - 1 || fz > farN - 1) break;
                            float clear = (ray - Sample(far, farN, fx, fz)) / (0.03f * s + 0.3f);
                            if (clear < visible) visible = clear;
                            s += farCell;
                        }
                    }
                    result[x + y * n] = visible > 0f ? visible : 0f;
                }
            });
            return result;
        }

        /// <summary>
        /// 空の開け具合（0〜1）。8 方向で、radii（m）の距離にある面を見上げる角度を調べ、高く見上げるほど空が狭いとする。
        /// 割れ目の中、崖の足もと、岩の足もとが暗くなる。
        /// </summary>
        private static float[] BakeSky(float[] ground, float[] caster, int n, float cell, float[] radii)
        {
            float[] dx = { 1f, 0.7071f, 0f, -0.7071f, -1f, -0.7071f, 0f, 0.7071f };
            float[] dz = { 0f, 0.7071f, 1f, 0.7071f, 0f, -0.7071f, -1f, -0.7071f };
            var result = new float[n * n];
            Parallel.For(0, n, y =>
            {
                for (int x = 0; x < n; x++)
                {
                    float here = Ground(ground[x + y * n]);
                    float blocked = 0f;
                    for (int k = 0; k < 8; k++)
                    {
                        float steepest = 0f;
                        for (int r = 0; r < radii.Length; r++)
                        {
                            float px = x + dx[k] * radii[r] / cell, pz = y + dz[k] * radii[r] / cell;
                            if (px < 0f || pz < 0f || px > n - 1 || pz > n - 1) break;
                            float tangent = (Sample(caster, n, px, pz) - here) / radii[r];
                            if (tangent > steepest) steepest = tangent;
                        }
                        blocked += steepest / (float)Math.Sqrt(1f + steepest * steepest);
                    }
                    result[x + y * n] = 1f - 0.9f * blocked / 8f;
                }
            });
            return result;
        }

        private static float Ground(float height)
        {
            return height > 0f ? height : 0f;
        }

        private static float Sample(float[] values, int n, float x, float y)
        {
            int x0 = (int)x, y0 = (int)y;
            if (x0 < 0) x0 = 0; else if (x0 > n - 2) x0 = n - 2;
            if (y0 < 0) y0 = 0; else if (y0 > n - 2) y0 = n - 2;
            float fx = x - x0, fy = y - y0;
            if (fx < 0f) fx = 0f; else if (fx > 1f) fx = 1f;
            if (fy < 0f) fy = 0f; else if (fy > 1f) fy = 1f;
            int i = x0 + y0 * n;
            float top = values[i] + (values[i + 1] - values[i]) * fx;
            float bottom = values[i + n] + (values[i + n + 1] - values[i + n]) * fx;
            return top + (bottom - top) * fy;
        }

        private static int Clamp(int value, int low, int high)
        {
            return value < low ? low : (value > high ? high : value);
        }
    }
}
