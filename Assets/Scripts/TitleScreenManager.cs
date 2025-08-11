using UnityEngine;
using System.Collections;
using TMPro;
using Vastcore.Player;

namespace Vastcore.UI
{
    /// <summary>
    /// 3Dタイトル「Vastcore」の表示とインタラクションを管理します。
    /// このコンポーネントはVastcoreGameManagerによって制御されます。
    /// </summary>
    public class TitleScreenManager : MonoBehaviour
    {
        [Header("Title Settings")]
        [SerializeField] private TextMeshPro m_TitleText;
        [Tooltip("プレイヤーカメラからのタイトルの表示距離")]
        [SerializeField] private float m_DisplayDistance = 30f;
        [Tooltip("フェードイン・アウトにかかる時間")]
        [SerializeField] private float m_FadeDuration = 2.0f;
        [Tooltip("この角度以上視線をそらすとタイトルがフェードアウトします")]
        [SerializeField] private float m_LookAwayAngle = 45f;

        private Camera m_PlayerCamera;
        private bool m_IsTitleActive = false;
        private Coroutine m_LookAwayCoroutine;

        /// <summary>
        /// TitleScreenManagerをセットアップします。
        /// VastcoreGameManagerから呼び出されることを想定しています。
        /// </summary>
        /// <param name="playerController">プレイヤーコントローラーのインスタンス</param>
        public void Setup(AdvancedPlayerController playerController)
        {
            if (playerController == null)
            {
                Debug.LogError("AdvancedPlayerController is null. Cannot setup TitleScreenManager.");
                return;
            }

            m_PlayerCamera = playerController.GetComponentInChildren<Camera>();

            if (m_PlayerCamera == null)
            {
                Debug.LogError("Player camera could not be found on the AdvancedPlayerController!");
            }

            if (m_TitleText == null)
            {
                Debug.LogError("Title Text (TextMeshPro) is not assigned in the inspector!");
                return;
            }
            
            // 初期状態では非表示にしておく
            m_TitleText.gameObject.SetActive(false);
            m_TitleText.color = new Color(m_TitleText.color.r, m_TitleText.color.g, m_TitleText.color.b, 0f);
        }

        /// <summary>
        /// タイトルをプレイヤーの視界内に表示します。
        /// </summary>
        public void ShowTitle()
        {
            if (m_PlayerCamera == null || m_TitleText == null)
            {
                Debug.LogWarning("Cannot show title because camera or text is not set up.");
                return;
            }

            // カメラの前方、少し上のビューポート座標(0.5, 0.6)をワールド座標に変換して位置を決める
            Vector3 targetPosition = m_PlayerCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.6f, m_DisplayDistance));
            m_TitleText.transform.position = targetPosition;

            // タイトルが常にカメラの方を向くように回転を調整する
            m_TitleText.transform.rotation = Quaternion.LookRotation(m_TitleText.transform.position - m_PlayerCamera.transform.position);
            
            m_TitleText.gameObject.SetActive(true);
            m_IsTitleActive = true;
            
            // フェードインを開始
            StartCoroutine(FadeTitle(true));

            // プレイヤーの視線がタイトルから外れていないかチェックを開始
            if (m_LookAwayCoroutine != null) StopCoroutine(m_LookAwayCoroutine);
            m_LookAwayCoroutine = StartCoroutine(CheckLookAway());
            
            Debug.Log("[TitleScreenManager] Title sequence started.");
        }

        /// <summary>
        /// プレイヤーの視線がタイトルから外れたかどうかを継続的にチェックします。
        /// </summary>
        private IEnumerator CheckLookAway()
        {
            while (m_IsTitleActive)
            {
                Vector3 directionToTitle = (m_TitleText.transform.position - m_PlayerCamera.transform.position).normalized;
                float angle = Vector3.Angle(m_PlayerCamera.transform.forward, directionToTitle);

                // 視線が指定した角度以上に外れたら、フェードアウト処理を開始
                if (angle > m_LookAwayAngle)
                {
                    StartCoroutine(FadeAndDeactivate());
                    yield break; // このコルーチンを終了
                }
                yield return null; // 次のフレームまで待機
            }
        }

        /// <summary>
        /// タイトルをフェードアウトし、非アクティブ化します。
        /// </summary>
        private IEnumerator FadeAndDeactivate()
        {
            m_IsTitleActive = false; // 視線チェックを停止
            yield return StartCoroutine(FadeTitle(false));
            m_TitleText.gameObject.SetActive(false);
            Debug.Log("[TitleScreenManager] Title faded out and deactivated due to look away.");
        }

        /// <summary>
        /// タイトルのフェードイン/フェードアウトを処理します。
        /// </summary>
        /// <param name="fadeIn">trueならフェードイン、falseならフェードアウト</param>
        private IEnumerator FadeTitle(bool fadeIn)
        {
            float targetAlpha = fadeIn ? 1f : 0f;
            float startAlpha = m_TitleText.color.a;

            float elapsedTime = 0f;
            while (elapsedTime < m_FadeDuration)
            {
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / m_FadeDuration);
                m_TitleText.color = new Color(m_TitleText.color.r, m_TitleText.color.g, m_TitleText.color.b, newAlpha);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // 最終的なアルファ値を設定
            m_TitleText.color = new Color(m_TitleText.color.r, m_TitleText.color.g, m_TitleText.color.b, targetAlpha);
        }
    }
}