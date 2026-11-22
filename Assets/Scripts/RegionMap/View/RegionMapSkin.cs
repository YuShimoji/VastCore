using UnityEngine;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 探索画面の十字・輪・点の絵を、コードを書き換えずに外から差し替える入口。
    /// <c>Assets/Resources/RegionMapSkin/</c> に、下の名前の画像（PNG など）を置くと、その絵が使われる。
    /// 置いていない部品は、コードで作る既定の絵のまま。名前の大文字小文字は区別される。
    /// 画像は、取り込み設定が Sprite でも Texture でも読める（Texture の時は、画像全体・中心の基準点・100 pixels per unit の Sprite にする）。
    /// 画像の寸法・向き・基準点の決まりは SP-021（docs/02_design/REGION_MAP_EXPLORATION_SPEC.md）の 4.1 節 G に書く。
    /// </summary>
    public static class RegionMapSkin
    {
        /// <summary>画像を置く Resources 下のフォルダ。</summary>
        public const string Folder = "RegionMapSkin";

        /// <summary>十字カーソル（座標を決める印）。</summary>
        public const string Crosshair = "Crosshair";

        /// <summary>サーチの範囲を示す輪。カーソルを中心に、発見の半径の大きさで出る。</summary>
        public const string SearchRing = "SearchRing";

        /// <summary>発見した地点の印の、外側の輪。</summary>
        public const string FoundRing = "FoundRing";

        /// <summary>発見した地点の印の、中心の点。</summary>
        public const string FoundDot = "FoundDot";

        /// <summary>差し替えられる画像の名前（拡張子なし）の一覧。</summary>
        public static readonly string[] Names = { Crosshair, SearchRing, FoundRing, FoundDot };

        /// <summary>
        /// 指定した名前の画像を読む。置いていなければ null（呼び出し側が既定の絵を使う）。
        /// </summary>
        public static Sprite Load(string name)
        {
            string path = Folder + "/" + name;
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null) return sprite;
            Texture2D texture = Resources.Load<Texture2D>(path);
            return texture != null ? FromTexture(texture) : null;
        }

        /// <summary>画像全体を、中心を基準点とする Sprite にする。</summary>
        public static Sprite FromTexture(Texture2D texture)
        {
            if (texture == null) return null;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
