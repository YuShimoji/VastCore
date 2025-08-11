using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System.Collections;

namespace NarrativeGen.UI
{
    public class TextClickHandler : MonoBehaviour, IPointerClickHandler
    {
        [Header("Text Variants")]
        public List<string> textVariants = new List<string>();
        
        [Header("Animation Settings")]
        public float fadeOutDuration = 0.3f;
        public float fadeInDuration = 0.5f;
        public Color highlightColor = Color.yellow;
        
        private TextMeshProUGUI textComponent;
        private int currentVariantIndex = 0;
        private Color originalColor;
        private bool isAnimating = false;

        private void Awake()
        {
            textComponent = GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                originalColor = textComponent.color;
                if (textVariants.Count == 0 && !string.IsNullOrEmpty(textComponent.text))
                {
                    textVariants.Add(textComponent.text);
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isAnimating || textVariants.Count <= 1) return;
            StartCoroutine(ChangeTextWithAnimation());
        }

        private IEnumerator ChangeTextWithAnimation()
        {
            isAnimating = true;

            // Highlight effect
            textComponent.color = highlightColor;
            yield return new WaitForSeconds(0.1f);

            // Fade out
            yield return StartCoroutine(FadeText(1f, 0f, fadeOutDuration));

            // Change text
            currentVariantIndex = (currentVariantIndex + 1) % textVariants.Count;
            textComponent.text = textVariants[currentVariantIndex];

            // Fade in
            yield return StartCoroutine(FadeText(0f, 1f, fadeInDuration));

            textComponent.color = originalColor;
            isAnimating = false;
        }

        private IEnumerator FadeText(float startAlpha, float endAlpha, float duration)
        {
            float elapsedTime = 0f;
            Color currentColor = textComponent.color;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
                currentColor.a = alpha;
                textComponent.color = currentColor;
                yield return null;
            }

            currentColor.a = endAlpha;
            textComponent.color = currentColor;
        }

        public void AddVariant(string variant)
        {
            if (!textVariants.Contains(variant))
            {
                textVariants.Add(variant);
            }
        }

        public void SetVariants(List<string> variants)
        {
            textVariants = new List<string>(variants);
            currentVariantIndex = 0;
            if (textComponent != null && textVariants.Count > 0)
            {
                textComponent.text = textVariants[0];
            }
        }
    }
}