using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Vastcore.Player; // AdvancedPlayerControllerの名前空間を追加

namespace Vastcore.Cinematic
{
    /// <summary>
    /// レターボックス付きシネマティックカメラコントローラー
    /// ゲーム開始時に、地形全体を見渡す映画的なカメラワークを提供する
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CinematicCameraController : MonoBehaviour
    {
        [Header("Cinematic Settings")]
        [SerializeField] private float m_TransitionDuration = 8.0f;
        [SerializeField] private float m_StartHeightOffset = 150f;
        [SerializeField] private float m_StartDistanceOffset = -200f;
        [SerializeField] private Vector3 m_EndPositionOffset = new Vector3(0, 3, -8);
        [SerializeField] private AnimationCurve m_MovementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Letterbox Settings")]
        [SerializeField] private Image m_TopLetterbox;
        [SerializeField] private Image m_BottomLetterbox;
        [SerializeField] private float m_LetterboxFadeDuration = 1.0f;

        private AdvancedPlayerController m_PlayerController;
        private Transform m_PlayerTransform;
        private Camera m_PlayerCamera;
        private Camera m_CinematicCamera;
        private Transform m_TerrainTransform;

        public void Setup(AdvancedPlayerController playerController, GameObject terrain)
        {
            m_PlayerController = playerController;
            m_PlayerTransform = playerController.transform;
            m_PlayerCamera = m_PlayerTransform.GetComponentInChildren<Camera>();
            m_CinematicCamera = GetComponent<Camera>();

            if (terrain != null)
            {
                m_TerrainTransform = terrain.transform;
            }

            if (m_TopLetterbox == null || m_BottomLetterbox == null)
            {
                Debug.LogError("Letterbox images are not assigned in the inspector!");
            }
        }

        public IEnumerator PlayInitialCinematicSequence()
        {
            if (m_PlayerController == null || m_PlayerCamera == null || m_TerrainTransform == null)
            {
                Debug.LogError("CinematicCameraController is not properly set up. Aborting cinematic.");
                yield break;
            }

            // プレイヤーの操作を制限し、プレイヤーカメラを無効化
            m_PlayerController.EnableLookOnly();
            m_PlayerCamera.enabled = false;
            m_CinematicCamera.enabled = true;

            yield return StartCoroutine(FadeLetterbox(true));

            // カメラワークの開始・終了位置と回転を計算
            Vector3 startPosition = m_PlayerTransform.position + new Vector3(0, m_StartHeightOffset, m_StartDistanceOffset);
            Quaternion startRotation = Quaternion.LookRotation(m_PlayerTransform.position - startPosition);

            Transform playerCameraTransform = m_PlayerCamera.transform;
            Vector3 endPosition = playerCameraTransform.position; // 最終的にはプレイヤーカメラの位置に
            Quaternion endRotation = playerCameraTransform.rotation;

            float elapsedTime = 0f;
            while (elapsedTime < m_TransitionDuration)
            {
                float t = m_MovementCurve.Evaluate(elapsedTime / m_TransitionDuration);
                
                transform.position = Vector3.Lerp(startPosition, endPosition, t);
                transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            transform.position = endPosition;
            transform.rotation = endRotation;

            yield return StartCoroutine(FadeLetterbox(false));

            // プレイヤーカメラを再度有効化し、シネマティックカメラを無効化
            m_PlayerCamera.enabled = true;
            m_CinematicCamera.enabled = false;
            
            // プレイヤーの操作を完全に許可
            m_PlayerController.EnablePlayerControl();
            
            Debug.Log("[CinematicCameraController] Initial cinematic completed.");
        }

        private IEnumerator FadeLetterbox(bool fadeIn)
        {
            if (m_TopLetterbox == null || m_BottomLetterbox == null) yield break;

            float targetAlpha = fadeIn ? 1f : 0f;
            float startAlpha = m_TopLetterbox.color.a;

            if(fadeIn) 
            {
                m_TopLetterbox.gameObject.SetActive(true);
                m_BottomLetterbox.gameObject.SetActive(true);
            }

            float elapsedTime = 0f;
            while (elapsedTime < m_LetterboxFadeDuration)
            {
                float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / m_LetterboxFadeDuration);
                SetLetterboxAlpha(alpha);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            SetLetterboxAlpha(targetAlpha);

            if (!fadeIn)
            {
                m_TopLetterbox.gameObject.SetActive(false);
                m_BottomLetterbox.gameObject.SetActive(false);
            }
        }

        private void SetLetterboxAlpha(float alpha)
        {
            Color topColor = m_TopLetterbox.color;
            topColor.a = alpha;
            m_TopLetterbox.color = topColor;

            Color bottomColor = m_BottomLetterbox.color;
            bottomColor.a = alpha;
            m_BottomLetterbox.color = bottomColor;
        }
    }
}