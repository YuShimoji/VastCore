using UnityEngine;

namespace Vastcore.RegionMap
{
    /// <summary>描画結果（北西から行ごとの RGBA32）を Unity のテクスチャへ移す。</summary>
    public static class RegionMapTexture
    {
        /// <summary>
        /// テクスチャは下の行から並ぶので、上下を入れ替えて北が上になるようにする。
        /// </summary>
        public static Texture2D Create(byte[] rgba, int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
            texture.name = "RegionMap";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Apply(texture, rgba, size);
            return texture;
        }

        public static void Apply(Texture2D texture, byte[] rgba, int size)
        {
            int stride = size * 4;
            var flipped = new byte[rgba.Length];
            for (int y = 0; y < size; y++)
            {
                System.Buffer.BlockCopy(rgba, y * stride, flipped, (size - 1 - y) * stride, stride);
            }
            texture.LoadRawTextureData(flipped);
            texture.Apply(false, false);
        }

        /// <summary>輪の形の白い画像。カーソルの範囲や地点の印に色を付けて使う。</summary>
        public static Sprite CreateRingSprite(int size, float thickness, bool filled)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
            texture.name = filled ? "RegionMapDot" : "RegionMapRing";
            texture.wrapMode = TextureWrapMode.Clamp;
            var colors = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float outer = size * 0.5f - 1f;
            float inner = outer - thickness;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(outer - d + 0.5f);
                    if (!filled) alpha *= Mathf.Clamp01(d - inner + 0.5f);
                    colors[x + y * size] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            texture.SetPixels32(colors);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
