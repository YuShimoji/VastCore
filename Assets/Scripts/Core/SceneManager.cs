using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace NarrativeGen.Core
{
    /// <summary>
    /// シーン遷移を管理するマネージャー
    /// </summary>
    public class SceneManager : MonoBehaviour
    {
        #region Constants
        private const string c_MenuScene = "MenuScene";
        private const string c_GameScene = "GameScene";
        #endregion

        #region Singleton
        public static SceneManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// メニューシーンへ遷移
        /// </summary>
        public void LoadMenuScene()
        {
            StartCoroutine(LoadSceneAsync(c_MenuScene));
        }

        /// <summary>
        /// ゲームシーンへ遷移
        /// </summary>
        public void LoadGameScene()
        {
            StartCoroutine(LoadSceneAsync(c_GameScene));
        }

        /// <summary>
        /// アプリケーション終了
        /// </summary>
        public void QuitApplication()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 非同期シーン読み込み
        /// </summary>
        private IEnumerator LoadSceneAsync(string sceneName)
        {
            AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }
        #endregion
    }
}