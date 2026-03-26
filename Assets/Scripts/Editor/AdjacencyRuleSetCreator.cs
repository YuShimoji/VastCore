using UnityEditor;
using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Editor
{
    /// <summary>
    /// 初期AdjacencyRuleSet SOを生成するEditorユーティリティ。
    /// メニュー: Vastcore > Create Initial Adjacency Rules
    /// 建築タグ間の代表的な隣接親和度ルールを定義する。
    /// </summary>
    public static class AdjacencyRuleSetCreator
    {
        private const string RULES_PATH = "Assets/Resources/AdjacencyRules";

        [MenuItem("Vastcore/Create Initial Adjacency Rules")]
        public static void CreateInitialRules()
        {
            if (!AssetDatabase.IsValidFolder(RULES_PATH))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                AssetDatabase.CreateFolder("Assets/Resources", "AdjacencyRules");
            }

            string path = $"{RULES_PATH}/DefaultAdjacencyRules.asset";

            if (AssetDatabase.LoadAssetAtPath<AdjacencyRuleSet>(path) != null)
            {
                Debug.Log($"[AdjacencyRuleSetCreator] スキップ (既存): {path}");
                return;
            }

            var ruleSet = ScriptableObject.CreateInstance<AdjacencyRuleSet>();

            // --- 高親和度ペア (0.8-0.9): よく共存する建築要素 ---

            // 宗教建築の組み合わせ
            ruleSet.AddRule(StructureTagAdapter.TAG_SACRED, StructureTagAdapter.TAG_ORNATE, 0.9f);
            ruleSet.AddRule(StructureTagAdapter.TAG_SACRED, StructureTagAdapter.TAG_DOME, 0.85f);
            ruleSet.AddRule(StructureTagAdapter.TAG_SACRED, StructureTagAdapter.TAG_COLUMN, 0.85f);
            ruleSet.AddRule(StructureTagAdapter.TAG_SACRED, StructureTagAdapter.TAG_SPIRE, 0.8f);

            // 防御建築の組み合わせ
            ruleSet.AddRule(StructureTagAdapter.TAG_FORTIFIED, StructureTagAdapter.TAG_WALL, 0.9f);
            ruleSet.AddRule(StructureTagAdapter.TAG_FORTIFIED, StructureTagAdapter.TAG_TOWER, 0.9f);
            ruleSet.AddRule(StructureTagAdapter.TAG_FORTIFIED, StructureTagAdapter.TAG_MASSIVE, 0.85f);

            // インフラ系
            ruleSet.AddRule(StructureTagAdapter.TAG_FUNCTIONAL, StructureTagAdapter.TAG_BRIDGE, 0.85f);
            ruleSet.AddRule(StructureTagAdapter.TAG_FUNCTIONAL, StructureTagAdapter.TAG_ARCH, 0.8f);

            // 幾何学系
            ruleSet.AddRule(StructureTagAdapter.TAG_GEOMETRIC, StructureTagAdapter.TAG_CRYSTAL, 0.9f);
            ruleSet.AddRule(StructureTagAdapter.TAG_GEOMETRIC, StructureTagAdapter.TAG_ELEGANT, 0.8f);

            // --- 中親和度ペア (0.5-0.7): 自然に共存するが主要関係ではない ---

            ruleSet.AddRule(StructureTagAdapter.TAG_MASSIVE, StructureTagAdapter.TAG_ARCH, 0.7f);
            ruleSet.AddRule(StructureTagAdapter.TAG_MASSIVE, StructureTagAdapter.TAG_DOME, 0.65f);
            ruleSet.AddRule(StructureTagAdapter.TAG_WALL, StructureTagAdapter.TAG_TOWER, 0.7f);
            ruleSet.AddRule(StructureTagAdapter.TAG_COLUMN, StructureTagAdapter.TAG_ARCH, 0.7f);
            ruleSet.AddRule(StructureTagAdapter.TAG_ORNATE, StructureTagAdapter.TAG_ELEGANT, 0.6f);
            ruleSet.AddRule(StructureTagAdapter.TAG_PRIMITIVE, StructureTagAdapter.TAG_WEATHERED, 0.65f);
            ruleSet.AddRule(StructureTagAdapter.TAG_STEPPED, StructureTagAdapter.TAG_ENCLOSURE, 0.6f);

            // --- 低親和度ペア (0.1-0.3): 共存しにくい対立要素 ---

            ruleSet.AddRule(StructureTagAdapter.TAG_ORNATE, StructureTagAdapter.TAG_PRIMITIVE, 0.15f);
            ruleSet.AddRule(StructureTagAdapter.TAG_ELEGANT, StructureTagAdapter.TAG_PRIMITIVE, 0.1f);
            ruleSet.AddRule(StructureTagAdapter.TAG_SACRED, StructureTagAdapter.TAG_FUNCTIONAL, 0.2f);
            ruleSet.AddRule(StructureTagAdapter.TAG_CRYSTAL, StructureTagAdapter.TAG_WEATHERED, 0.15f);
            ruleSet.AddRule(StructureTagAdapter.TAG_FORTIFIED, StructureTagAdapter.TAG_ELEGANT, 0.25f);
            ruleSet.AddRule(StructureTagAdapter.TAG_ORGANIC, StructureTagAdapter.TAG_GEOMETRIC, 0.2f);

            AssetDatabase.CreateAsset(ruleSet, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[AdjacencyRuleSetCreator] {ruleSet.RuleCount}件のルールを {path} に作成しました");
        }
    }
}
