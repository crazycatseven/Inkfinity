using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Utility class providing animation tools for UI elements
    /// </summary>
    public static class AnimationUtils
    {
        public enum TextAnimationType
        {
            Fade,
            TypeWriter,
            Zoom,
            Slide
        }

        /// <summary>
        /// Animates text transition for TextMeshPro components
        /// </summary>
        /// <param name="owner">MonoBehaviour that will own the coroutine</param>
        /// <param name="textComponent">Target TMP component</param>
        /// <param name="newText">Text to transition to</param>
        /// <param name="animationType">Animation style to use</param>
        /// <param name="duration">Duration of the animation in seconds</param>
        /// <param name="onComplete">Optional callback when animation completes</param>
        /// <returns>Coroutine handle</returns>
        public static Coroutine AnimateTextTransition(
            MonoBehaviour owner,
            TMP_Text textComponent,
            string newText,
            TextAnimationType animationType = TextAnimationType.Fade,
            float duration = 0.5f,
            UnityAction onComplete = null)
        {
            if (textComponent == null || owner == null) return null;

            switch (animationType)
            {
                case TextAnimationType.Fade:
                    return owner.StartCoroutine(FadeTextTransition(textComponent, newText, duration, onComplete));
                case TextAnimationType.TypeWriter:
                    return owner.StartCoroutine(TypeWriterTextTransition(textComponent, newText, duration, onComplete));
                case TextAnimationType.Zoom:
                    return owner.StartCoroutine(ZoomTextTransition(textComponent, newText, duration, onComplete));
                case TextAnimationType.Slide:
                    return owner.StartCoroutine(SlideTextTransition(textComponent, newText, duration, onComplete));
                default:
                    return owner.StartCoroutine(FadeTextTransition(textComponent, newText, duration, onComplete));
            }
        }

        /// <summary>
        /// Fade in/out text transition effect
        /// </summary>
        private static IEnumerator FadeTextTransition(
            TMP_Text textComponent,
            string newText,
            float duration,
            UnityAction onComplete = null)
        {
            Color originalColor = textComponent.color;

            // Fade out
            float halfDuration = duration / 2;
            float timer = 0;

            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(1, 0, timer / halfDuration);
                textComponent.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }

            textComponent.text = newText;

            // Fade in
            timer = 0;
            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(0, 1, timer / halfDuration);
                textComponent.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }

            textComponent.color = originalColor;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Typewriter text transition effect
        /// </summary>
        private static IEnumerator TypeWriterTextTransition(
            TMP_Text textComponent,
            string newText,
            float duration,
            UnityAction onComplete = null)
        {
            textComponent.text = "";
            float charTime = duration / newText.Length;

            for (int i = 0; i < newText.Length; i++)
            {
                textComponent.text = newText.Substring(0, i + 1);
                yield return new WaitForSeconds(charTime);
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// Scale text transition effect
        /// </summary>
        private static IEnumerator ZoomTextTransition(
            TMP_Text textComponent,
            string newText,
            float duration,
            UnityAction onComplete = null)
        {
            Vector3 originalScale = textComponent.transform.localScale;

            // Scale down
            float halfDuration = duration / 2;
            float timer = 0;

            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                float scale = Mathf.Lerp(1, 0.5f, timer / halfDuration);
                textComponent.transform.localScale = originalScale * scale;
                yield return null;
            }

            textComponent.text = newText;

            // Scale up
            timer = 0;
            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                float scale = Mathf.Lerp(0.5f, 1, timer / halfDuration);
                textComponent.transform.localScale = originalScale * scale;
                yield return null;
            }

            textComponent.transform.localScale = originalScale;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Slide text transition effect
        /// </summary>
        private static IEnumerator SlideTextTransition(
            TMP_Text textComponent,
            string newText,
            float duration,
            UnityAction onComplete = null)
        {
            Vector3 originalPosition = textComponent.rectTransform.anchoredPosition;
            Vector2 originalSize = textComponent.rectTransform.sizeDelta;

            // Slide out
            float halfDuration = duration / 2;
            float timer = 0;

            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                float offset = Mathf.Lerp(0, originalSize.x, timer / halfDuration);
                textComponent.rectTransform.anchoredPosition = originalPosition + new Vector3(offset, 0, 0);
                yield return null;
            }

            textComponent.text = newText;
            textComponent.rectTransform.anchoredPosition = originalPosition - new Vector3(originalSize.x, 0, 0);

            // Slide in
            timer = 0;
            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                float offset = Mathf.Lerp(-originalSize.x, 0, timer / halfDuration);
                textComponent.rectTransform.anchoredPosition = originalPosition + new Vector3(offset, 0, 0);
                yield return null;
            }

            textComponent.rectTransform.anchoredPosition = originalPosition;
            onComplete?.Invoke();
        }
    }
}