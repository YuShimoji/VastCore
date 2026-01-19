using UnityEngine;

namespace Vastcore.Generation.Map
{
    public enum TemplateBlendMode
    {
        Additive,
        Multiplicative,
    }

    /// <summary>
    /// 地形テンプレート用の簡易ブレンド設定（Editor から ScriptableObject として扱う）
    /// </summary>
    public class BlendSettings : ScriptableObject
    {
        public TemplateBlendMode blendMode = TemplateBlendMode.Additive;
        [Range(0f, 1f)] public float blendStrength = 1f;
        public float fadeDistance = 100f;
        public bool enableEdgeBlending = true;
        public float edgeBlendWidth = 10f;
    }
}
