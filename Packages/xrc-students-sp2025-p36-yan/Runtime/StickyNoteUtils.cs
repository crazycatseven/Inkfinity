using UnityEngine;
using TMPro;
using System.Collections;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Utility class for StickyNote functionality
    /// </summary>
    public static class StickyNoteUtils
    {
        /// <summary>
        /// Settings for text auto-sizing
        /// </summary>
        [System.Serializable]
        public class TextAutoSizeSettings
        {
            public float fontSize = 24f;
            public int maxCharactersPerLine = 20;
            public float paddingHorizontal = 20f;
            public float paddingVertical = 15f;
            public float lineSpacing = 1.2f;
            public float minStickyNoteWidth = 100f;
            public float minStickyNoteHeight = 60f;
            public bool enableAutoSizing = true;
        }

        /// <summary>
        /// Calculate optimal size for text content
        /// </summary>
        /// <param name="text">Text to measure</param>
        /// <param name="textComponent">Text component to use for measurement</param>
        /// <param name="settings">Auto-size settings</param>
        /// <returns>Required size in pixels</returns>
        public static Vector2 CalculateOptimalTextSize(string text, TMP_Text textComponent, TextAutoSizeSettings settings)
        {
            if (string.IsNullOrEmpty(text) || textComponent == null)
            {
                return new Vector2(settings.minStickyNoteWidth, settings.minStickyNoteHeight);
            }

            // Temporarily set font size and text
            float originalFontSize = textComponent.fontSize;
            string originalText = textComponent.text;

            textComponent.fontSize = settings.fontSize;
            textComponent.text = text;

            // Force text component to update
            textComponent.ForceMeshUpdate();

            // Get the preferred values
            Vector2 preferredSize = textComponent.GetPreferredValues(
                text,
                settings.maxCharactersPerLine * settings.fontSize * 0.6f,
                0
            );

            // Apply line spacing
            preferredSize.y *= settings.lineSpacing;

            // Add padding
            preferredSize.x += settings.paddingHorizontal * 2;
            preferredSize.y += settings.paddingVertical * 2;

            // Ensure minimum size
            preferredSize.x = Mathf.Max(preferredSize.x, settings.minStickyNoteWidth);
            preferredSize.y = Mathf.Max(preferredSize.y, settings.minStickyNoteHeight);

            // Restore original values
            textComponent.fontSize = originalFontSize;
            textComponent.text = originalText;

            return preferredSize;
        }

        /// <summary>
        /// Setup text component for auto-sizing
        /// </summary>
        /// <param name="textComponent">Text component to setup</param>
        /// <param name="settings">Auto-size settings</param>
        public static void SetupTextComponent(TMP_Text textComponent, TextAutoSizeSettings settings)
        {
            if (textComponent == null) return;

            textComponent.fontSize = settings.fontSize;
            textComponent.textWrappingMode = TextWrappingModes.Normal;
            textComponent.overflowMode = TextOverflowModes.Overflow;

            // Setup text rect with padding
            RectTransform textRect = textComponent.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(settings.paddingHorizontal, settings.paddingVertical);
            textRect.offsetMax = new Vector2(-settings.paddingHorizontal, -settings.paddingVertical);
        }

        /// <summary>
        /// Animate sticky note size to target dimensions
        /// </summary>
        /// <param name="stickyNote">The sticky note to animate</param>
        /// <param name="canvasRectTransform">Canvas rect transform</param>
        /// <param name="targetSize">Target size in pixels</param>
        /// <param name="duration">Animation duration</param>
        /// <returns>Coroutine</returns>
        public static IEnumerator AnimateSizeChange(
            StickyNote stickyNote,
            RectTransform canvasRectTransform,
            Vector2 targetSize,
            float duration)
        {
            if (canvasRectTransform == null) yield break;

            Vector2 startSize = canvasRectTransform.sizeDelta;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                Vector2 currentSize = Vector2.Lerp(startSize, targetSize, smoothT);
                canvasRectTransform.sizeDelta = currentSize;

                // Update StickyNote dimensions and collider
                float newWidth = currentSize.x / 1000f;
                float newHeight = currentSize.y / 1000f;
                UpdateStickyNoteDimensions(stickyNote, newWidth, newHeight);
                stickyNote.UpdateColliderSize(newWidth, newHeight);

                yield return null;
            }

            // Ensure final values are set
            canvasRectTransform.sizeDelta = targetSize;
            UpdateStickyNoteDimensions(stickyNote, targetSize.x / 1000f, targetSize.y / 1000f);
        }

        /// <summary>
        /// Update sticky note dimensions (helper method)
        /// </summary>
        private static void UpdateStickyNoteDimensions(StickyNote stickyNote, float width, float height)
        {
            // Direct property assignment since Width and Height are now public set
            stickyNote.Width = width;
            stickyNote.Height = height;
        }
    }
}