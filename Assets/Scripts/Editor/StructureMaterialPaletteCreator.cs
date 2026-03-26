using UnityEditor;
using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Editor
{
    /// <summary>
    /// 初期StructureMaterialPalette SOを一括生成するEditorユーティリティ。
    /// メニュー: Vastcore > Create Initial Material Palettes
    /// </summary>
    public static class StructureMaterialPaletteCreator
    {
        private const string PALETTE_PATH = "Assets/Resources/MaterialPalettes";

        [MenuItem("Vastcore/Create Initial Material Palettes")]
        public static void CreateInitialPalettes()
        {
            if (!AssetDatabase.IsValidFolder(PALETTE_PATH))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                AssetDatabase.CreateFolder("Assets/Resources", "MaterialPalettes");
            }

            // 大聖堂: 装飾的で神聖。風化なし
            CreatePalette("Cathedral", 0.05f,
                ("arch", 0.7f), ("dome", 0.5f), ("massive", 0.8f),
                ("ornate", 0.9f), ("sacred", 0.95f));

            // 要塞: 重厚で機能的。軽度の風化
            CreatePalette("Fortress", 0.2f,
                ("wall", 0.9f), ("tower", 0.7f), ("massive", 0.95f),
                ("fortified", 0.95f), ("functional", 0.6f));

            // 水道橋: 実用的インフラ。中程度の風化
            CreatePalette("Aqueduct", 0.35f,
                ("arch", 0.8f), ("bridge", 0.9f), ("massive", 0.6f),
                ("functional", 0.9f));

            // 廃墟: 古代遺跡。高度に風化
            CreatePalette("Ruins", 0.85f,
                ("wall", 0.4f), ("column", 0.5f), ("weathered", 0.95f),
                ("primitive", 0.6f), ("massive", 0.5f));

            // 結晶尖塔: 幾何学的結晶。風化なし
            CreatePalette("CrystalSpire", 0f,
                ("spire", 0.8f), ("crystal", 0.95f), ("elegant", 0.7f),
                ("geometric", 0.9f));

            // 円形劇場: 大規模囲い込み。軽度の風化
            CreatePalette("Amphitheater", 0.25f,
                ("enclosure", 0.9f), ("stepped", 0.6f), ("massive", 0.7f),
                ("ornate", 0.5f));

            // 巨石碑: 原始的で巨大。高度に風化
            CreatePalette("Monolith", 0.7f,
                ("massive", 0.95f), ("primitive", 0.8f), ("geometric", 0.6f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[StructureMaterialPaletteCreator] 7件のパレットを {PALETTE_PATH} に作成しました");
        }

        private static void CreatePalette(string _name, float _weatheringLevel,
            params (string tag, float weight)[] _tags)
        {
            string path = $"{PALETTE_PATH}/Palette_{_name}.asset";

            // 既存アセットがあればスキップ
            if (AssetDatabase.LoadAssetAtPath<StructureMaterialPalette>(path) != null)
            {
                Debug.Log($"[StructureMaterialPaletteCreator] スキップ (既存): {path}");
                return;
            }

            var palette = ScriptableObject.CreateInstance<StructureMaterialPalette>();

            // リフレクションで private フィールドを設定
            var type = typeof(StructureMaterialPalette);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            type.GetField("m_DisplayName", flags)?.SetValue(palette, _name);
            type.GetField("m_WeatheringLevel", flags)?.SetValue(palette, _weatheringLevel);

            var profile = new StructureTagProfile();
            foreach (var (tag, weight) in _tags)
            {
                profile.SetWeight(tag, weight);
            }
            type.GetField("m_Affinity", flags)?.SetValue(palette, profile);

            AssetDatabase.CreateAsset(palette, path);
            Debug.Log($"[StructureMaterialPaletteCreator] 作成: {path}");
        }
    }
}
