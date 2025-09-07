using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TMPro;
using System.Collections;
using NarrativeGen.Core;

namespace NarrativeGen.UI
{
    public class MenuManager : MonoBehaviour
    {
        [Header("Title Animation")]
        public TextMeshProUGUI titleText;
        public float dropDuration = 2.0f;
        public float dropHeight = 500f;
        
        [Header("UI Elements")]
        public Button startButton;
        public Button quitButton;
        public CanvasGroup buttonsGroup;

        private Vector3 originalTitlePosition;
        private bool animationComplete = false;

        private void Start()
        {
            InitializeMenu();
            StartTitleAnimation();
        }

        private void Update()
        {
            if (animationComplete && titleText != null)
            {
                float glowIntensity = Mathf.Sin(Time.time * 2f) * 0.3f + 0.7f;
                Color glowColor = Color.white * glowIntensity;
                titleText.color = glowColor;
            }
        }

        private void InitializeMenu()
        {
            if (titleText != null)
            {
                titleText.text = "NARRATIVE GEN";
                originalTitlePosition = titleText.rectTransform.anchoredPosition;
                Vector3 startPos = originalTitlePosition;
                startPos.y += dropHeight;
                titleText.rectTransform.anchoredPosition = startPos;
            }

            if (buttonsGroup != null)
            {
                buttonsGroup.alpha = 0f;
                buttonsGroup.interactable = false;
            }

            if (startButton != null)
                startButton.onClick.AddListener(OnStartButtonClicked);
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitButtonClicked);
        }

        private void StartTitleAnimation()
        {
            if (titleText != null)
                StartCoroutine(TitleDropAnimation());
        }

        private IEnumerator TitleDropAnimation()
        {
            float elapsedTime = 0f;
            Vector3 startPos = titleText.rectTransform.anchoredPosition;

            while (elapsedTime < dropDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / dropDuration;
                Vector3 currentPos = Vector3.Lerp(startPos, originalTitlePosition, progress);
                titleText.rectTransform.anchoredPosition = currentPos;
                yield return null;
            }

            titleText.rectTransform.anchoredPosition = originalTitlePosition;
            animationComplete = true;
            yield return StartCoroutine(FadeInButtons());
        }

        private IEnumerator FadeInButtons()
        {
            if (buttonsGroup == null) yield break;
            float duration = 1.0f;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                buttonsGroup.alpha = elapsedTime / duration;
                yield return null;
            }

            buttonsGroup.alpha = 1f;
            buttonsGroup.interactable = true;
        }

        private void OnStartButtonClicked()
        {
            buttonsGroup.interactable = false;
            if (SceneManager.Instance != null)
                SceneManager.Instance.LoadGameScene();
        }

        private void OnQuitButtonClicked()
        {
            if (SceneManager.Instance != null)
                SceneManager.Instance.QuitApplication();
        }
    }
}