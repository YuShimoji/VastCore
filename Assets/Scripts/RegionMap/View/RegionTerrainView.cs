using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.RegionMap
{
    /// <summary>3D 表示にする窓の指定（3D 化の試作）。既定は、崖と洞窟の試作を確かめた seed 9 の山の 0.5 km。</summary>
    [System.Serializable]
    public sealed class RegionTerrainViewSpec
    {
        public float CenterMapX = 38.8f;
        public float CenterMapY = 60.9f;
        public float SizeKm = 0.5f;
        public int Resolution = 1024;

        /// <summary>地層の段の強さ（RegionDetailSpec.Strata）。0 だと崖が無く、体積で作る場所も選ばれない。</summary>
        public float Strata = 1f;

        /// <summary>体積で作り直す部分の升目（m）。0 以下なら体積は作らない。</summary>
        public float VoxelMeters = 1f;
    }

    /// <summary>
    /// 拡大した地形と、体積で作り直した部分（崖下のえぐれと洞窟）を、Unity の Mesh にして並べる（3D 化の試作）。
    /// Core の座標（X 東・Y 上・Z 南、m）を、Unity の座標（X 東・Y 上・Z 北）へ、Z の符号を逆にして写す。
    /// </summary>
    public static class RegionTerrainView
    {
        public const string ShaderResource = "RegionMap3D/RegionTerrain";

        public sealed class Result
        {
            public GameObject Root;
            public RegionData Terrain;
            public RegionVolumeSpec Volume;
            public RegionVolumePatch Patch;

            /// <summary>見どころ（Unity の座標）。体積があれば崖の足もと、無ければ窓の中心の地面。</summary>
            public Vector3 Focus;

            /// <summary>見どころを正面から見る時にカメラを置く方位（度。0 で南から北を見る）。</summary>
            public float FacingYawDegrees;
        }

        public static Result Build(RegionData region, RegionTerrainViewSpec spec)
        {
            if (spec == null) spec = new RegionTerrainViewSpec();
            RegionData terrain = RegionDetailBuilder.Build(region, new RegionDetailSpec
            {
                CenterMapX = spec.CenterMapX,
                CenterMapY = spec.CenterMapY,
                SizeKm = spec.SizeKm,
                Resolution = spec.Resolution,
                Strata = spec.Strata,
            }).Data;
            RegionVolumeSpec volume = spec.VoxelMeters > 0f ? RegionVolumeFeatures.CliffShelterAndCave(terrain, spec.VoxelMeters) : null;
            RegionVolumePatch patch = volume != null ? RegionVolumePatch.Build(terrain, volume) : null;

            // 光は Unity が計算するので、貼る画像には陰影と落ち影を焼き込まない
            byte[] pixels = RegionMapRenderer.Render(terrain, new RegionRenderOptions { CloudCover = 0f, Exaggeration = 0f, CastShadow = 0f });
            Texture2D texture = RegionMapTexture.Create(pixels, terrain.Resolution);
            texture.name = "Region Terrain Ground";
            texture.hideFlags = HideFlags.DontSave;
            Material material = CreateMaterial(texture);

            var root = new GameObject("Region Terrain 3D");
            RegionTerrainMesh grid = RegionTerrainMesh.Build(terrain, 1, 1f);
            AddPart(root.transform, "Terrain", TerrainMesh(grid, patch), material);
            float span = (terrain.Resolution - 1) * terrain.CellMeters;
            if (patch != null) AddPart(root.transform, "Volume", PatchMesh(patch, span), material);

            var result = new Result { Root = root, Terrain = terrain, Volume = volume, Patch = patch };
            if (volume != null)
            {
                result.Focus = new Vector3(volume.FocusX, volume.FocusY, -volume.FocusZ);
                result.FacingYawDegrees = volume.FacingYawDegrees;
            }
            else
            {
                float center = span * 0.5f;
                result.Focus = new Vector3(center, RegionVolumePatch.SampleHeight(terrain, center, center), -center);
            }
            return result;
        }

        /// <summary>
        /// カメラを、見どころから distance（m）離れた所へ置いて、見どころへ向ける。
        /// yawOffsetDegrees は正面からのずれ、pitchDegrees は見下ろす角度（確認ツールの --view3d と同じ決め方）。
        /// </summary>
        public static void Place(Transform camera, Result result, float yawOffsetDegrees, float pitchDegrees, float distance)
        {
            float yaw = (result.FacingYawDegrees + yawOffsetDegrees) * Mathf.Deg2Rad;
            float pitch = pitchDegrees * Mathf.Deg2Rad;
            float flat = distance * Mathf.Cos(pitch);
            // Core では Z が南なので、Unity では符号が逆になる
            camera.position = result.Focus + new Vector3(flat * Mathf.Sin(yaw), distance * Mathf.Sin(pitch), -flat * Mathf.Cos(yaw));
            camera.rotation = Quaternion.LookRotation(result.Focus - camera.position, Vector3.up);
        }

        /// <summary>太陽の向き（光が進む向き）。北西・高さ 38 度から照らす（鳥瞰図と同じ）。</summary>
        public static Quaternion SunRotation()
        {
            float altitude = RegionVolumePatch.SunAltitudeDegrees * Mathf.Deg2Rad;
            var toSun = new Vector3(-Mathf.Cos(altitude) * 0.70710678f, Mathf.Sin(altitude), Mathf.Cos(altitude) * 0.70710678f);
            return Quaternion.LookRotation(-toSun, Vector3.up);
        }

        /// <summary>地形のメッシュ。hole を渡すと、その箱の範囲の三角形を抜く（縁は 1.5 升ぶん残して重ねる）。</summary>
        public static Mesh TerrainMesh(RegionTerrainMesh source, RegionVolumePatch hole)
        {
            int count = source.VertexCount;
            float[] p = source.Positions;
            var vertices = new Vector3[count];
            var uv = new Vector2[count];
            for (int v = 0; v < count; v++)
            {
                vertices[v] = new Vector3(p[v * 3], p[v * 3 + 1], -p[v * 3 + 2]);
                uv[v] = new Vector2(source.Uv[v * 2], 1f - source.Uv[v * 2 + 1]);
            }

            int[] triangles = source.Indices;
            if (hole != null)
            {
                float overlap = 1.5f * hole.Spec.VoxelMeters;
                float minX = hole.MinX + overlap, maxX = hole.MaxX - overlap;
                float minZ = hole.MinZ + overlap, maxZ = hole.MaxZ - overlap;
                var kept = new List<int>(triangles.Length);
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
                    if (Inside(p, a, minX, maxX, minZ, maxZ) && Inside(p, b, minX, maxX, minZ, maxZ) && Inside(p, c, minX, maxX, minZ, maxZ)) continue;
                    kept.Add(a);
                    kept.Add(b);
                    kept.Add(c);
                }
                triangles = kept.ToArray();
            }

            var mesh = new Mesh { name = "Region Terrain", indexFormat = IndexFormat.UInt32, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateNormals();
            return mesh;
        }

        /// <summary>
        /// 体積で作り直した部分のメッシュ。span は地形の一辺（m。貼る画像の位置を決める）。
        /// 頂点カラーの R に「真上が空か」、G に「日が当たるか」を入れる（開けた面は Unity の影に任せるので、G は真上が空なら 1）。
        /// </summary>
        public static Mesh PatchMesh(RegionVolumePatch patch, float span)
        {
            int count = patch.VertexCount;
            float[] p = patch.Positions;
            float[] n = patch.Normals;
            var vertices = new Vector3[count];
            var normals = new Vector3[count];
            var uv = new Vector2[count];
            var colors = new Color32[count];
            for (int v = 0; v < count; v++)
            {
                vertices[v] = new Vector3(p[v * 3], p[v * 3 + 1], -p[v * 3 + 2]);
                normals[v] = new Vector3(n[v * 3], n[v * 3 + 1], -n[v * 3 + 2]);
                uv[v] = new Vector2(p[v * 3] / span, 1f - p[v * 3 + 2] / span);
                colors[v] = new Color32((byte)(patch.Sky[v] * 255f), (byte)(Mathf.Max(patch.Sky[v], patch.Sun[v]) * 255f), 255, 255);
            }

            // Z を逆にすると回り方も逆になるので、面の向きを頂点の外向きに合わせ直す
            int[] triangles = (int[])patch.Indices.Clone();
            for (int t = 0; t < triangles.Length; t += 3)
            {
                int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
                Vector3 face = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                if (Vector3.Dot(face, normals[a] + normals[b] + normals[c]) >= 0f) continue;
                triangles[t + 1] = c;
                triangles[t + 2] = b;
            }

            var mesh = new Mesh { name = "Region Volume", indexFormat = IndexFormat.UInt32, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0, true);
            return mesh;
        }

        /// <summary>見え方の想定と見比べる場面（RegionLookScene）を Unity に並べた物。</summary>
        public sealed class LookResult
        {
            public GameObject Root;
            public RegionLookScene Scene;
        }

        /// <summary>
        /// 砂岩の台地の場面（外側の地形・内側の地形・岩）を作って並べる。地形の影は頂点に持たせてあるので、
        /// Unity の影は岩だけが落とす（影の届く距離の中でだけ出る）。
        /// </summary>
        public static LookResult BuildLook(RegionData region, RegionLookSpec spec)
        {
            RegionLookScene scene = RegionLookScene.Build(region, spec);
            var root = new GameObject("Region Terrain Look");
            var plain = new RegionRenderOptions { CloudCover = 0f, Exaggeration = 0f, CastShadow = 0f };
            foreach (RegionLookPart part in scene.Parts())
            {
                Texture2D texture;
                if (part.Data != null)
                {
                    texture = RegionMapTexture.Create(RegionMapRenderer.Render(part.Data, plain), part.Data.Resolution);
                }
                else
                {
                    var white = new Color32(255, 255, 255, 255);
                    texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    texture.SetPixels32(new[] { white, white, white, white });
                    texture.Apply();
                }
                texture.name = part.Name + " Ground";
                texture.hideFlags = HideFlags.DontSave;
                Material material = CreateMaterial(texture);
                material.name = part.Name;
                material.SetColor("_RockColor", new Color(RegionLookScene.RockRed, RegionLookScene.RockGreen, RegionLookScene.RockBlue, 1f));
                material.SetFloat("_BandAmount", RegionLookScene.BandAmount);
                if (part.Data?.MesaContactHeight != null || (part.Data == null && scene.Spec.Mesa > 0f))
                {
                    material.EnableKeyword("REGION_SANDSTONE");
                    material.SetFloat("_LooseRock", part.Data == null ? 1f : 0f);
                }
                MeshRenderer renderer = AddPart(root.transform, part.Name, LookMesh(part), material);
                // 地形の影は頂点に持たせてある。Unity の影を落とすのは岩だけにする
                if (part.Data != null) renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            var points = new GameObject("Viewpoints");
            points.transform.SetParent(root.transform, false);
            foreach (RegionLookViewpoint viewpoint in scene.Spec.Viewpoints())
            {
                var point = new GameObject(viewpoint.Name);
                point.transform.SetParent(points.transform, false);
                PlaceLook(point.transform, scene, viewpoint);
            }
            return new LookResult { Root = root, Scene = scene };
        }

        /// <summary>決まった視点の位置と向きを写す（画角は RegionLookViewpoint.FovDegrees を Camera.fieldOfView に入れる）。</summary>
        public static void PlaceLook(Transform target, RegionLookScene scene, RegionLookViewpoint viewpoint)
        {
            var eye = new float[3];
            var look = new float[3];
            scene.Resolve(viewpoint, eye, look);
            target.position = new Vector3(eye[0], eye[1], -eye[2]);
            target.rotation = Quaternion.LookRotation(new Vector3(look[0] - eye[0], look[1] - eye[1], -(look[2] - eye[2])), Vector3.up);
        }

        /// <summary>
        /// 場面の部品のメッシュ。頂点カラーは R = 空の開け具合、G = 日が当たる度合い、B = 1 − 岩肌の度合い、A = 色のむら。
        /// </summary>
        public static Mesh LookMesh(RegionLookPart part)
        {
            int count = part.VertexCount;
            float[] p = part.Positions;
            float[] n = part.Normals;
            var vertices = new Vector3[count];
            var normals = new Vector3[count];
            var uv = new Vector2[count];
            var colors = new Color32[count];
            for (int v = 0; v < count; v++)
            {
                vertices[v] = new Vector3(p[v * 3], p[v * 3 + 1], -p[v * 3 + 2]);
                normals[v] = new Vector3(n[v * 3], n[v * 3 + 1], -n[v * 3 + 2]);
                if (part.Uv != null) uv[v] = new Vector2(part.Uv[v * 2], 1f - part.Uv[v * 2 + 1]);
                colors[v] = new Color32((byte)(Mathf.Clamp01(part.Sky[v]) * 255f), (byte)(Mathf.Clamp01(part.Sun[v]) * 255f),
                    (byte)((1f - Mathf.Clamp01(part.Rock[v])) * 255f), (byte)(Mathf.Clamp01(part.Tint[v]) * 255f));
            }

            int[] triangles = part.Indices;
            if (part.Data == null)
            {
                // 岩は、面の向きから回り方を確かめる（Z を逆にしているので）
                triangles = (int[])triangles.Clone();
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
                    Vector3 face = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                    if (Vector3.Dot(face, normals[a]) >= 0f) continue;
                    triangles[t + 1] = c;
                    triangles[t + 2] = b;
                }
            }

            var mesh = new Mesh { name = part.Name, indexFormat = IndexFormat.UInt32, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            if (part.Uv != null) mesh.SetUVs(0, uv);
            if (part.Data?.MesaContactHeight != null)
            {
                RegionData data = part.Data;
                var surface = new Vector4[count];
                for (int v = 0; v < count; v++)
                {
                    float wet = data.WaterCoverage != null ? data.WaterCoverage[v] : 0f;
                    float dryLand = data.Height[v] <= 0f || data.LakeDepth[v] > 0f ? 0f
                        : (1f - Mathf.Clamp01(wet)) * RegionNoise.Smooth(0.42f, 0.30f, data.Moisture[v]);
                    float cover = Mathf.InverseLerp(0.10f, 0.32f, data.Moisture[v]);
                    surface[v] = new Vector4(data.Talus[v], vertices[v].y - data.MesaContactHeight[v], cover, dryLand);
                }
                mesh.SetUVs(1, surface);
            }
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0, true);
            return mesh;
        }

        private static bool Inside(float[] positions, int vertex, float minX, float maxX, float minZ, float maxZ)
        {
            float x = positions[vertex * 3];
            float z = positions[vertex * 3 + 2];
            return x > minX && x < maxX && z > minZ && z < maxZ;
        }

        private static Material CreateMaterial(Texture2D texture)
        {
            Shader shader = Resources.Load<Shader>(ShaderResource);
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader) { name = "Region Terrain", hideFlags = HideFlags.DontSave };
            material.SetTexture("_BaseMap", texture);
            return material;
        }

        private static MeshRenderer AddPart(Transform parent, string name, Mesh mesh, Material material)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return renderer;
        }
    }
}
