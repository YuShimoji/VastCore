using UnityEngine;

namespace Vastcore.Core
{
    public interface ISceneNavigator
    {
        void LoadMenuScene();
        void LoadGameScene();
        void QuitApplication();
    }

    /// <summary>
    /// デフォルトのシーンナビゲータ。既存の NarrativeGen.Core.SceneManager があればそれを使用し、
    /// なければ UnityEngine.SceneManagement をフォールバックとして利用します。
    /// </summary>
    public class DefaultSceneNavigator : ISceneNavigator
    {
        public void LoadMenuScene()
        {
            TrySceneManager(sm => sm.LoadMenuScene(), () => UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene"));
        }

        public void LoadGameScene()
        {
            TrySceneManager(sm => sm.LoadGameScene(), () => UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene"));
        }

        public void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void TrySceneManager(System.Action<Vastcore.Core.SceneManager> viaSM, System.Action fallback)
        {
            var sm = Vastcore.Core.SceneManager.Instance;
            if (sm != null)
            {
                viaSM?.Invoke(sm);
            }
            else
            {
                fallback?.Invoke();
            }
        }
    }

    /// <summary>
    /// シーンナビゲータへのアクセス入口。差し替え可能。
    /// </summary>
    public static class SceneNavigator
    {
        private static ISceneNavigator _current;
        public static ISceneNavigator Current
        {
            get
            {
                if (_current == null)
                {
                    _current = new DefaultSceneNavigator();
                }
                return _current;
            }
            set { _current = value; }
        }
    }
}
