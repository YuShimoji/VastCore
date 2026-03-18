using UnityEditor;
using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Editor
{
    /// <summary>
    /// 初期StructureTagPreset SOを一括生成するEditorユーティリティ。
    /// メニュー: Vastcore > Create Initial Structure Presets
    /// </summary>
    public static class StructureTagPresetCreator
    {
        private const string PRESET_PATH = "Assets/Resources/StructurePresets";

        [MenuItem("Vastcore/Create Initial Structure Presets")]
        public static void CreateInitialPresets()
        {
            if (!AssetDatabase.IsValidFolder(PRESET_PATH))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                AssetDatabase.CreateFolder("Assets/Resources", "StructurePresets");
            }

            CreatePreset("Cathedral", "大聖堂 — 巨大で装飾的な神聖建築",
                ("arch", 0.7f), ("dome", 0.5f), ("massive", 0.8f),
                ("ornate", 0.9f), ("sacred", 0.95f));

            CreatePreset("Fortress", "要塞 — 防御的で重厚な軍事建築",
                ("wall", 0.9f), ("tower", 0.7f), ("massive", 0.95f),
                ("fortified", 0.95f), ("functional", 0.6f));

            CreatePreset("Aqueduct", "水道橋 — アーチ構造の実用的インフラ",
                ("arch", 0.8f), ("bridge", 0.9f), ("massive", 0.6f),
                ("functional", 0.9f));

            CreatePreset("Ruins", "廃墟 — 風化した古代遺跡",
                ("wall", 0.4f), ("column", 0.5f), ("weathered", 0.95f),
                ("primitive", 0.6f), ("massive", 0.5f));

            CreatePreset("CrystalSpire", "結晶尖塔 — 幾何学的な結晶構造",
                ("spire", 0.8f), ("crystal", 0.95f), ("elegant", 0.7f),
                ("geometric", 0.9f));

            CreatePreset("Amphitheater", "円形劇場 — 大規模な囲い込み構造",
                ("enclosure", 0.9f), ("stepped", 0.6f), ("massive", 0.7f),
                ("ornate", 0.5f));

            CreatePreset("Monolith", "巨石碑 — 原始的で巨大な構造物",
                ("massive", 0.95f), ("primitive", 0.8f), ("geometric", 0.6f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[StructureTagPresetCreator] 7件のプリセットを {PRESET_PATH} に作成しました");
        }

        private static void CreatePreset(string _name, string _description,
            params (string tag, float weight)[] _tags)
        {
            string path = $"{PRESET_PATH}/Preset_{_name}.asset";

            // 既存アセットがあればスキップ
            if (AssetDatabase.LoadAssetAtPath<StructureTagPreset>(path) != null)
            {
                Debug.Log($"[StructureTagPresetCreator] スキップ (既存): {path}");
                return;
            }

            var preset = ScriptableObject.CreateInstance<StructureTagPreset>();

            // リフレクションで private フィールドを設定
            var type = typeof(StructureTagPreset);
            var displayNameField = type.GetField("m_DisplayName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var descriptionField = type.GetField("m_Description",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var profileField = type.GetField("m_Profile",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            displayNameField?.SetValue(preset, _name);
            descriptionField?.SetValue(preset, _description);

            var profile = new StructureTagProfile();
            foreach (var (tag, weight) in _tags)
            {
                profile.SetWeight(tag, weight);
            }
            profileField?.SetValue(preset, profile);

            AssetDatabase.CreateAsset(preset, path);
            Debug.Log($"[StructureTagPresetCreator] 作成: {path}");
        }
    }
}
