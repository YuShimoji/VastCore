using UnityEngine;
using Vastcore.Player;

namespace Vastcore.UI
{
    // ルート名前空間を定義するだけのプレースホルダー。
}

namespace Vastcore.UI.Menus
{
    /// <summary>
    /// VastcoreGameManager から参照されるタイトル画面管理のスタブ。
    /// 実際の UI 実装がなくてもコンパイルできるようにするための最小実装です。
    /// </summary>
    public class TitleScreenManager : MonoBehaviour
    {
        public void Setup(AdvancedPlayerController controller)
        {
            // スタブ: 何もしない
        }

        public void ShowTitle()
        {
            // スタブ: ログのみ
            Debug.Log("TitleScreenManager.ShowTitle (stub)");
        }
    }
}
