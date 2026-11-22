using UnityEngine;
using UnityEngine.UI;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 輪を、線の太さを一定のまま、いくらでも大きく描く。画像を引き伸ばす輪は、拡大に比例して線も太くなり、
    /// 細部まで拡大した画面（輪の直径が画面の数倍）では太い帯になる。この輪は半径だけを RectTransform の大きさから読み、
    /// 太さと暗い縁は画面の単位で固定する。
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RegionRingGraphic : MaskableGraphic
    {
        private const int Segments = 192;

        /// <summary>白い線の太さ（Canvas の単位）。</summary>
        public float Thickness = 3f;

        /// <summary>白い線の外と内に足す暗い縁の太さ。</summary>
        public float Rim = 1.5f;

        public Color RimColor = new Color(0f, 0f, 0f, 0.75f);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            float outer = Mathf.Min(rect.width, rect.height) * 0.5f;
            if (outer <= Thickness + Rim) return;
            Vector2 center = rect.center;
            AddRing(vh, center, outer + Rim, outer - Thickness - Rim, RimColor);
            AddRing(vh, center, outer, outer - Thickness, color);
        }

        private static void AddRing(VertexHelper vh, Vector2 center, float outer, float inner, Color32 color)
        {
            int start = vh.currentVertCount;
            for (int s = 0; s < Segments; s++)
            {
                float angle = s / (float)Segments * Mathf.PI * 2f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                vh.AddVert(center + direction * outer, color, Vector2.zero);
                vh.AddVert(center + direction * inner, color, Vector2.zero);
            }
            for (int s = 0; s < Segments; s++)
            {
                int a = start + s * 2;
                int b = start + (s + 1) % Segments * 2;
                vh.AddTriangle(a, b, a + 1);
                vh.AddTriangle(b, b + 1, a + 1);
            }
        }
    }
}
