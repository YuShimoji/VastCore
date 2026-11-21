using UnityEngine;

namespace Vastcore.RegionMap
{
    /// <summary>描画結果（北西から行ごとの RGBA32）を Unity のテクスチャへ移す。</summary>
    public static class RegionMapTexture
    {
        /// <summary>
        /// テクスチャは下の行から並ぶので、上下を入れ替えて北が上になるようにする。
        /// </summary>
        public static Texture2D Create(byte[] rgba, int size, bool repeat = false)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
            texture.name = "RegionMap";
            // 雲の層のように、ずらしながら繰り返して使う画像は repeat にする
            texture.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
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

        /// <summary>
        /// 十字カーソルの画像。4 本の腕は中心を空けて置くので、狙う 1 点が隠れない。白い線の周りに暗い縁を持つので、
        /// 雲・雪・淡い地色の上でも形が読める。縦と横を別々の部品で重ねると、縁が交点で片方の線を横切る。
        /// 1 枚の画像にして、交点も縁も 1 つの形として描く。寸法は画像のピクセル数。
        /// </summary>
        public static Sprite CreateCrosshairSprite(int size, float gap, float thickness, float rim)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
            texture.name = "RegionMapCrosshair";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var colors = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float reach = size * 0.5f - rim - 1.5f;
            float armHalfLength = (reach - gap) * 0.5f;
            float armCenter = gap + armHalfLength;
            float half = thickness * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 4 本の腕は上下左右で同じ形なので、右上の 1/4 へ折り返して測る
                    float ax = Mathf.Abs(x - center);
                    float ay = Mathf.Abs(y - center);
                    float distance = Mathf.Min(
                        BoxDistance(ax - armCenter, ay, armHalfLength, half),
                        BoxDistance(ax, ay - armCenter, half, armHalfLength));
                    // 中心の小さな点（狙っている位置そのもの）
                    distance = Mathf.Min(distance, Mathf.Sqrt(ax * ax + ay * ay) - half);

                    float white = Mathf.Clamp01(0.5f - distance);
                    float dark = Mathf.Clamp01(0.5f - (distance - rim)) * 0.8f;
                    float alpha = white + dark * (1f - white);
                    byte tone = alpha > 0f ? (byte)(white / alpha * 255f) : (byte)255;
                    colors[x + y * size] = new Color32(tone, tone, tone, (byte)(alpha * 255f));
                }
            }
            texture.SetPixels32(colors);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>中心が原点の長方形までの距離（中は負）。px, py は中心からのずれ。</summary>
        private static float BoxDistance(float px, float py, float halfWidth, float halfHeight)
        {
            float qx = Mathf.Abs(px) - halfWidth;
            float qy = Mathf.Abs(py) - halfHeight;
            float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f);
        }
    }
}
