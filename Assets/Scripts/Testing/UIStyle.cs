using UnityEngine;

namespace Vastcore.UI
{
    /// <summary>
    /// UIスタイル定義クラス
    /// テスト用に簡易的なスタブ実装
    /// </summary>
    public class UIStyle : ScriptableObject
    {
        // 基本プロパティ
        public Color primaryColor = Color.blue;
        public Color secondaryColor = Color.gray;
        public Color backgroundColor = Color.black;
        public float fontSize = 14f;
        public Font font;

        // デフォルトコンストラクタ
        public UIStyle() { }

        // ファクトリーメソッド
        public static UIStyle CreateDefault()
        {
            UIStyle style = CreateInstance<UIStyle>();
            style.primaryColor = Color.blue;
            style.secondaryColor = Color.gray;
            style.backgroundColor = Color.black;
            style.fontSize = 14f;
            return style;
        }
    }
}
