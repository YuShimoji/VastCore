using UnityEngine;
using UnityEngine.UI;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 雲（または雲の影）の層 1 枚。四角 1 枚ではなく升目のメッシュで描き、升目の角ごとに絵を読む位置をずらす。
    /// これで、場所によって向きと速さの違う流れになる（RawImage の uvRect では全体を一様にしか動かせない）。
    /// ずらす量の計算は RegionCloudFlow（UnityEngine 非依存）が持つ。絵は端で繰り返す設定のテクスチャを使う。
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RegionCloudLayer : MaskableGraphic
    {
        /// <summary>升目の数（縦横とも）。</summary>
        public const int Cells = 32;

        private const int Side = Cells + 1;

        private readonly Vector2[] _offset = new Vector2[Side * Side];
        private readonly float[] _fade = new float[Side * Side];
        private Texture _texture;
        private float _opacity = 1f;
        private bool _flowSet;

        public Texture Texture
        {
            get { return _texture; }
            set
            {
                if (_texture == value) return;
                _texture = value;
                SetMaterialDirty();
            }
        }

        public override Texture mainTexture
        {
            get { return _texture != null ? _texture : s_WhiteTexture; }
        }

        /// <summary>
        /// 今の時刻の流れを升目の角へ写す。layer は RegionCloudFlow.Low / High、shift は絵全体のずれ（雲の影を南東へ落とす分）、
        /// opacity は層全体の濃さ。
        /// </summary>
        public void SetFlow(int layer, float time, float crossSeconds, Vector2 shift, float opacity)
        {
            for (int gy = 0; gy < Side; gy++)
            {
                float v = gy / (float)Cells;
                for (int gx = 0; gx < Side; gx++)
                {
                    float du;
                    float dv;
                    float fade;
                    RegionCloudFlow.Sample(layer, gx / (float)Cells, v, time, crossSeconds, out du, out dv, out fade);
                    int g = gx + gy * Side;
                    _offset[g] = new Vector2(du + shift.x, dv + shift.y);
                    _fade[g] = fade;
                }
            }
            _opacity = opacity;
            _flowSet = true;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            Color tint = color;
            for (int gy = 0; gy < Side; gy++)
            {
                float v = gy / (float)Cells;
                for (int gx = 0; gx < Side; gx++)
                {
                    float u = gx / (float)Cells;
                    int g = gx + gy * Side;
                    Vector2 offset = _flowSet ? _offset[g] : Vector2.zero;
                    float fade = _flowSet ? _fade[g] : 1f;
                    var vertexColor = new Color(tint.r, tint.g, tint.b, tint.a * _opacity * fade);
                    vh.AddVert(
                        new Vector3(rect.xMin + rect.width * u, rect.yMin + rect.height * v, 0f),
                        vertexColor,
                        new Vector4(u - offset.x, v - offset.y, 0f, 0f));
                }
            }
            for (int gy = 0; gy < Cells; gy++)
            {
                for (int gx = 0; gx < Cells; gx++)
                {
                    int a = gx + gy * Side;
                    int b = a + 1;
                    int c = a + Side;
                    int d = c + 1;
                    vh.AddTriangle(a, c, d);
                    vh.AddTriangle(a, d, b);
                }
            }
        }
    }
}
