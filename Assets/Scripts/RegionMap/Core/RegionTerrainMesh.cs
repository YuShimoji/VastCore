using System;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 地域の標高を三角形メッシュにしたもの（3D 化の試作、段階 A）。UnityEngine に依存しない配列だけを持つので、
    /// Unity の Mesh へも確認用ツールの描画へもそのまま渡せる。
    /// 座標は m。X は東、Z は南（地図の Y と同じ向き）、Y は上。北西の角が原点。
    /// 海（標高 0 以下）は水面の高さ 0 に揃え、海底の起伏は出さない（水面を別に作るまでの仮の扱い）。
    /// </summary>
    public sealed class RegionTerrainMesh
    {
        /// <summary>頂点の数（横・縦）。</summary>
        public int Columns;
        public int Rows;

        /// <summary>頂点ごとの X・Y・Z（m）。長さは頂点数 × 3。</summary>
        public float[] Positions;

        /// <summary>頂点ごとの画像上の位置（0〜1）。長さは頂点数 × 2。真上から見た画像をそのまま貼れる。</summary>
        public float[] Uv;

        /// <summary>三角形ごとの頂点番号 3 つ。</summary>
        public int[] Indices;

        public int VertexCount
        {
            get { return Columns * Rows; }
        }

        public int TriangleCount
        {
            get { return Indices.Length / 3; }
        }

        /// <summary>
        /// 標高の格子から作る。step は何セルおきに頂点を取るか（1 で全セル）。
        /// exaggeration は高さの倍率で、広域（256 km）は幅に対して山が低いので 1 より大きくしないと平らに見える。
        /// </summary>
        public static RegionTerrainMesh Build(RegionData data, int step, float exaggeration)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (step < 1) step = 1;
            int n = data.Resolution;
            int cols = (n - 1) / step + 1;
            float cell = data.CellMeters;
            var mesh = new RegionTerrainMesh
            {
                Columns = cols,
                Rows = cols,
                Positions = new float[cols * cols * 3],
                Uv = new float[cols * cols * 2],
                Indices = new int[(cols - 1) * (cols - 1) * 6],
            };

            for (int j = 0; j < cols; j++)
            {
                int y = Math.Min(n - 1, j * step);
                for (int i = 0; i < cols; i++)
                {
                    int x = Math.Min(n - 1, i * step);
                    int v = i + j * cols;
                    float height = data.Height[x + y * n];
                    mesh.Positions[v * 3] = x * cell;
                    mesh.Positions[v * 3 + 1] = height > 0f ? height * exaggeration : 0f;
                    mesh.Positions[v * 3 + 2] = y * cell;
                    mesh.Uv[v * 2] = x / (float)(n - 1);
                    mesh.Uv[v * 2 + 1] = y / (float)(n - 1);
                }
            }

            // 四角を 2 枚に割る。高さの差が小さい方の対角線で割ると、尾根や海岸のぎざぎざが減る。
            int t = 0;
            for (int j = 0; j < cols - 1; j++)
            {
                for (int i = 0; i < cols - 1; i++)
                {
                    int a = i + j * cols;
                    int b = a + 1;
                    int c = a + cols;
                    int d = c + 1;
                    float diagonalAD = Math.Abs(mesh.Positions[a * 3 + 1] - mesh.Positions[d * 3 + 1]);
                    float diagonalBC = Math.Abs(mesh.Positions[b * 3 + 1] - mesh.Positions[c * 3 + 1]);
                    if (diagonalAD <= diagonalBC)
                    {
                        mesh.Indices[t++] = a; mesh.Indices[t++] = b; mesh.Indices[t++] = d;
                        mesh.Indices[t++] = a; mesh.Indices[t++] = d; mesh.Indices[t++] = c;
                    }
                    else
                    {
                        mesh.Indices[t++] = a; mesh.Indices[t++] = b; mesh.Indices[t++] = c;
                        mesh.Indices[t++] = b; mesh.Indices[t++] = d; mesh.Indices[t++] = c;
                    }
                }
            }
            return mesh;
        }
    }
}
