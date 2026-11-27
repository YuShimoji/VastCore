using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 砂岩の台地の場面で、役ごとに置かれた写真素材（取り込み時に作った細部・法線・高さ粗さAO の 3 枚）を、
    /// 場面を作る時に GPU で 3 本の配列（Texture2DArray）へ写して束ね、地形のシェーダーへ渡す（RegionSurfaceMaterials.hlsl）。
    /// 配列はファイルにしない（生成物を持たない。素材をフォルダから外すと、次に場面を作る時に今の手続きの絵へ戻る）。
    /// 置き場所と取り込みは RegionSurfaceMaps、設計は docs/02_design/REGION_SURFACE_MATERIALS_SPEC.md。
    /// </summary>
    public static class RegionSurfaceMaterials
    {
        /// <summary>false にすると、素材を読まず今の手続きの絵で描く。</summary>
        public static bool Enabled = true;

        /// <summary>
        /// 距離の段（m、カメラから）。NearStart〜NearEnd で高さ粗さAO を読まなくなり（近）、FarStart〜FarEnd で写真を読まなくなる（中）。
        /// その先（遠）は素材が無い時と同じ絵（設計 3.3 節。値は開発担当の仮の値で、計測で決める）。
        /// </summary>
        public static float NearStart = 40f, NearEnd = 60f, FarStart = 300f, FarEnd = 400f;

        /// <summary>鏡面の強さ（反射率 0.04 に掛ける）。乾いた砂岩なので弱める（設計 3.3 節の初期値 18%）。</summary>
        public static float Specular = 0.18f;

        /// <summary>高さによる層の混合を使うか（前後比較用）。境の深さ・幅は RegionSurfaceSettings。</summary>
        public static bool HeightBlend = true;

        /// <summary>負でなければ、設定の境の深さ（m）の代わりに使う（既定を撮って決める時用）。</summary>
        public static float BlendDepthOverride = -1f;

        /// <summary>
        /// 素材の候補を撮り比べる時用（Editor の RegionSurfaceCandidates）。null でなければ、役ごとの 3 枚（細部・法線・高さ粗さAO）を
        /// Resources の代わりにここから取る。null を返した役は素材なし。
        /// </summary>
        public static System.Func<int, Texture2D[]> SourceOverride;

        /// <summary>同上。役ごとの横の実寸（m）。0 以下を返した役は設定の値のまま。</summary>
        public static System.Func<int, float> TileOverride;

        private static Vector4 blendHeights, blendHeights2;
        private static float blendDepth, blendSpan = RegionSurfaceSettings.DefaultBlendSpan;

        private static Texture2DArray detailArray, normalArray, hraArray;
        private static readonly Vector4[] roles = new Vector4[RegionSurfaceMaps.Folders.Length];

        /// <summary>配列に入った役の数。</summary>
        public static int Count { get; private set; }

        /// <summary>直前の Bind の結果（役ごとに、使った・使えなかった理由）。Editor の撮影がログに書く。</summary>
        public static string Summary { get; private set; } = "未作成";

        /// <summary>置かれた素材を読んで配列を作り、シェーダーへ渡す。素材が無い時は切る。</summary>
        public static void Bind()
        {
            Release();
            var notes = new List<string>();
            var slices = new List<(int role, Texture2D detail, Texture2D normal, Texture2D hra)>();
            for (int i = 0; i < roles.Length; i++) roles[i] = new Vector4(-1f, 0f, 0f, 0f);
            bool canCopy = (SystemInfo.copyTextureSupport & CopyTextureSupport.DifferentTypes) != 0
                           && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
            if (Enabled && canCopy)
            {
                for (int role = 0; role < RegionSurfaceMaps.Folders.Length; role++)
                {
                    Texture2D detail, normal, hra;
                    if (SourceOverride != null)
                    {
                        Texture2D[] provided = SourceOverride(role);
                        detail = provided != null ? provided[0] : null;
                        normal = provided != null ? provided[1] : null;
                        hra = provided != null ? provided[2] : null;
                    }
                    else
                    {
                        string folder = RegionSurfaceMaps.Root + "/" + RegionSurfaceMaps.Folders[role] + "/";
                        detail = Resources.Load<Texture2D>(folder + RegionSurfaceMaps.DetailName);
                        normal = Resources.Load<Texture2D>(folder + RegionSurfaceMaps.NormalName);
                        hra = Resources.Load<Texture2D>(folder + RegionSurfaceMaps.HraName);
                    }
                    if (detail == null && normal == null && hra == null) continue;
                    string problem = Check(detail, normal, hra, slices.Count > 0 ? slices[0] : default);
                    if (problem != null)
                    {
                        notes.Add(RegionSurfaceMaps.Folders[role] + " 使えない（" + problem + "）");
                        continue;
                    }
                    slices.Add((role, detail, normal, hra));
                }
            }
            if (slices.Count > 0)
            {
                detailArray = Gather(slices, s => s.detail, "Region surfaces: detail");
                normalArray = Gather(slices, s => s.normal, "Region surfaces: normal");
                hraArray = Gather(slices, s => s.hra, "Region surfaces: height roughness occlusion");
                RegionSurfaceSettings.Entry[] settings = RegionSurfaceSettings.Load();
                for (int slice = 0; slice < slices.Count; slice++)
                {
                    int role = slices[slice].role;
                    RegionSurfaceSettings.Entry entry = settings[role];
                    float tile = TileOverride != null ? TileOverride(role) : 0f;
                    if (tile > 0f) entry.TileMetres = tile;
                    roles[role] = new Vector4(slice, 1f / Mathf.Max(0.05f, entry.TileMetres), entry.Strength, entry.Roughness);
                    notes.Add(RegionSurfaceMaps.Folders[role] + " " + entry.TileMetres.ToString("0.##") + " m");
                }
                (blendDepth, blendSpan) = RegionSurfaceSettings.LoadBlend();
                blendSpan = Mathf.Max(0.001f, blendSpan);
                blendHeights = new Vector4(settings[0].HeightMetres, settings[1].HeightMetres, settings[2].HeightMetres, settings[4].HeightMetres) / blendSpan;
                blendHeights2 = new Vector4(settings[(int)RegionSurfaceRole.Deposit].HeightMetres, settings[(int)RegionSurfaceRole.Wash].HeightMetres, 0f, 0f) / blendSpan;
                Shader.SetGlobalTexture("_RegionSurfaceDetail", detailArray);
                Shader.SetGlobalTexture("_RegionSurfaceNormal", normalArray);
                Shader.SetGlobalTexture("_RegionSurfaceHRA", hraArray);
            }
            Count = slices.Count;
            if (!Enabled) notes.Add("切");
            else if (!canCopy) notes.Add("GPU の画像の写しが使えない");
            Summary = notes.Count == 0 ? "なし（今の手続きの絵）" : string.Join("、", notes);
            Shader.SetGlobalVectorArray("_RegionSurfaceRoles", roles);
            SetActive(true);
        }

        /// <summary>配列を残したまま、素材を使うかを切り替える（同じ場面の前後比較と、重さの計測に使う）。</summary>
        public static void SetActive(bool active)
        {
            Shader.SetGlobalVector("_RegionSurfaceParams", new Vector4(active && Count > 0 ? 1f : 0f, Specular, 0f, 0f));
            Shader.SetGlobalVector("_RegionSurfaceTiers", new Vector4(NearStart, NearEnd, FarStart, FarEnd));
            float depth = BlendDepthOverride >= 0f ? BlendDepthOverride : blendDepth;
            bool blend = active && Count > 0 && HeightBlend && depth > 0f;
            Shader.SetGlobalVector("_RegionSurfaceBlend", new Vector4(blend ? depth / blendSpan : 0f, 0f, 0f, 0f));
            Shader.SetGlobalVector("_RegionSurfaceBlendHeights", blendHeights);
            Shader.SetGlobalVector("_RegionSurfaceBlendHeights2", blendHeights2);
        }

        public static void Release()
        {
            foreach (Texture2DArray array in new[] { detailArray, normalArray, hraArray })
                if (array != null) Object.DestroyImmediate(array);
            detailArray = normalArray = hraArray = null;
            Count = 0;
        }

        // 3 枚が揃い、正方形で、先に入った役と大きさ・形式・mip の数が同じか。問題があれば理由を返す
        private static string Check(Texture2D detail, Texture2D normal, Texture2D hra,
            (int role, Texture2D detail, Texture2D normal, Texture2D hra) first)
        {
            if (detail == null || normal == null || hra == null) return "3 枚が揃っていない。取り込み直す";
            foreach (Texture2D texture in new[] { detail, normal, hra })
                if (texture.width != texture.height || texture.width != detail.width || texture.mipmapCount != detail.mipmapCount)
                    return texture.name + " の大きさ " + texture.width + "x" + texture.height;
            if (first.detail == null) return null;
            if (detail.width != first.detail.width || detail.mipmapCount != first.detail.mipmapCount) return "大きさが他の役と違う";
            if (detail.graphicsFormat != first.detail.graphicsFormat || normal.graphicsFormat != first.normal.graphicsFormat
                || hra.graphicsFormat != first.hra.graphicsFormat) return "形式が他の役と違う";
            return null;
        }

        private static Texture2DArray Gather(List<(int role, Texture2D detail, Texture2D normal, Texture2D hra)> slices,
            System.Func<(int role, Texture2D detail, Texture2D normal, Texture2D hra), Texture2D> pick, string name)
        {
            Texture2D first = pick(slices[0]);
            var array = new Texture2DArray(first.width, first.height, slices.Count, first.graphicsFormat, TextureCreationFlags.MipChain, first.mipmapCount)
            {
                name = name, hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear, anisoLevel = 4,
            };
            for (int slice = 0; slice < slices.Count; slice++) Graphics.CopyTexture(pick(slices[slice]), 0, array, slice);
            return array;
        }
    }
}
