using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine.UI;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Represents a sticky note in the scene
    /// </summary>
    public class StickyNote : MonoBehaviour
    {
        // Properties
        public Vector3[] Corners { get; private set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public Color Color { get; private set; }
        public string Text { get; private set; }

        [SerializeField, Tooltip("Height offset of the strokes")]
        private float strokeHeightOffset = 0.003f;

        [SerializeField, Tooltip("Fade in duration (seconds)")]
        private float fadeInDuration = 0.5f;

        [SerializeField, Tooltip("Scale in duration (seconds)")]
        private float scaleInDuration = 0.5f;

        [SerializeField, Tooltip("Animation curve for scaling effect")]
        private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [SerializeField, Tooltip("Widget background color")]
        private Color widgetBackgroundColor = new Color(0.9f, 0.9f, 0.95f, 1.0f);

        [SerializeField, Tooltip("Canvas for displaying text")]
        private Canvas canvas;

        [SerializeField, Tooltip("Background image for the sticky note")]
        private Image backgroundImage;

        [SerializeField, Tooltip("Text component for displaying text")]
        private TMP_Text textComponent;



        [Header("Auto-sizing Settings")]
        [SerializeField, Tooltip("Text auto-sizing settings")] private StickyNoteUtils.TextAutoSizeSettings autoSizeSettings = new StickyNoteUtils.TextAutoSizeSettings();

        // List of strokes attached to this sticky note
        private List<Stroke> attachedStrokes = new List<Stroke>();

        // Smart widget attached to this sticky note
        private GameObject attachedWidget;

        // Canvas rect transform
        private RectTransform canvasRectTransform;

        // Coroutine for text animation
        private Coroutine textAnimationCoroutine;

        // Coroutine for size animation
        private Coroutine sizeAnimationCoroutine;

        /// <summary>
        /// Initialize when the component is created
        /// </summary>
        private void Awake()
        {

            if (canvas == null)
            {
                canvas = GetComponentInChildren<Canvas>(true);
            }

            if (canvas != null)
            {
                canvasRectTransform = canvas.GetComponent<RectTransform>();
            }

            if (textComponent == null && canvas != null)
            {
                textComponent = canvas.GetComponentInChildren<TMP_Text>(true);
            }

            if (backgroundImage == null)
            {
                backgroundImage = GetComponentInChildren<Image>(true);
            }

            if (textComponent != null)
            {
                textComponent.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Initialize the sticky note with corners
        /// </summary>
        /// <param name="corners">The four corners of the sticky note</param>
        /// <param name="defaultColor">The default color to apply</param>
        /// <param name="withScaleAnimation">Whether to use scale-in animation</param>
        /// <returns>Whether initialization was successful</returns>
        public bool Initialize(Vector3[] corners, Color defaultColor, bool withScaleAnimation = true)
        {
            if (corners == null || corners.Length != 4)
            {
                Debug.LogError("Cannot initialize StickyNote: need exactly 4 corner points");
                return false;
            }

            // Store corners
            Corners = corners;

            // Calculate center
            Vector3 center = Vector3.zero;
            foreach (var corner in corners)
            {
                center += corner;
            }
            center /= 4f;

            // Calculate dimensions
            Width = Vector3.Distance(corners[0], corners[1]);
            Height = Vector3.Distance(corners[1], corners[2]);

            if (canvasRectTransform != null)
            {
                canvasRectTransform.sizeDelta = new Vector2(Width * 1000f, Height * 1000f);
            }

            // Position at center
            transform.position = center;

            // Calculate rotation
            Vector3 forward = Vector3.Cross((corners[1] - corners[0]), Vector3.up).normalized;
            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
            transform.rotation = rotation;

            // Rotate 90 degrees on X axis for proper orientation
            transform.Rotate(Vector3.right, 90f);

            if (withScaleAnimation)
            {
                transform.localScale = Vector3.zero;
            }
            else
            {
                transform.localScale = Vector3.one;
            }

            // Set color and size, initially with zero alpha
            Color = defaultColor;
            if (backgroundImage != null)
            {
                // Set initial color to transparent
                Color transparentColor = Color;
                transparentColor.a = 0f;
                backgroundImage.color = transparentColor;

                // Start the fade in animation
                StartCoroutine(FadeIn());

                // Start the scale in animation if enabled
                if (withScaleAnimation)
                {
                    StartCoroutine(ScaleIn());
                }
            }
            else
            {
                Debug.LogWarning("StickyNote doesn't have a background image");
                return false;
            }

            // Get and adjust the size of the BoxCollider
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.size = new Vector3(Width, Height, 0.01f);
                boxCollider.center = new Vector3(0, 0, 0.0075f);
            }
            else
            {
                Debug.LogWarning("StickyNote doesn't have a BoxCollider component");
            }

            return true;
        }

        /// <summary>
        /// Set text content with optional animation and auto-sizing
        /// </summary>
        /// <param name="text">Text to display</param>
        /// <param name="animationType">Animation type (None if duration is 0)</param>
        /// <param name="duration">Animation duration in seconds (0 = instant)</param>
        /// <param name="activateCanvas">Whether to activate text canvas</param>
        public void SetText(string text, AnimationUtils.TextAnimationType animationType = AnimationUtils.TextAnimationType.Fade, float duration = 0f, bool activateCanvas = true)
        {
            if (textComponent == null)
            {
                Debug.LogWarning("Text component not found on StickyNote");
                return;
            }

            if (activateCanvas)
                textComponent.gameObject.SetActive(true);

            // Stop ongoing animations
            if (textAnimationCoroutine != null)
                StopCoroutine(textAnimationCoroutine);
            if (sizeAnimationCoroutine != null)
                StopCoroutine(sizeAnimationCoroutine);

            // Instant text update (no animation)
            if (duration <= 0f)
            {
                ApplyTextInstantly(text);
                return;
            }

            // Animated text update
            ApplyTextWithAnimation(text, animationType, duration);
        }

        /// <summary>
        /// Set text color
        /// </summary>
        public void SetTextColor(Color color)
        {
            if (textComponent != null)
            {
                textComponent.color = color;
            }
            else
            {
                Debug.LogWarning("Text component not found on StickyNote");
            }
        }

        /// <summary>
        /// Show or hide text canvas
        /// </summary>
        public void ShowText(bool show)
        {
            if (textComponent != null)
            {
                textComponent.gameObject.SetActive(show);
            }
        }

        /// <summary>
        /// Fade in the sticky note
        /// </summary>
        private IEnumerator FadeIn()
        {
            float fadeInTime = 0f;
            Color startColor = backgroundImage.color;
            Color targetColor = Color;

            while (fadeInTime < fadeInDuration)
            {
                fadeInTime += Time.deltaTime;
                float t = Mathf.Clamp01(fadeInTime / fadeInDuration);

                // Use smooth transition
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // Change only the alpha value
                Color newColor = Color;
                newColor.a = Mathf.Lerp(startColor.a, targetColor.a, smoothT);
                backgroundImage.color = newColor;

                yield return null;
            }

            backgroundImage.color = targetColor;
        }

        /// <summary>
        /// Scale in the sticky note
        /// </summary>
        private IEnumerator ScaleIn()
        {
            float scaleInTime = 0f;
            Vector3 targetScale = Vector3.one;

            while (scaleInTime < scaleInDuration)
            {
                scaleInTime += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(scaleInTime / scaleInDuration);

                // Use the animation curve for smooth transition
                float curveValue = scaleCurve.Evaluate(normalizedTime);

                // Apply scale
                transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, curveValue);

                yield return null;
            }

            transform.localScale = targetScale;
        }

        /// <summary>
        /// Get the bounds of the sticky note
        /// </summary>
        public Bounds GetBounds()
        {
            Bounds bounds = new Bounds();
            bounds.center = transform.position;
            bounds.size = new Vector3(Width, 0.01f, Height);
            return bounds;
        }

        /// <summary>
        /// Change the color of the sticky note
        /// </summary>
        /// <param name="newColor">The new color to apply</param>
        /// <param name="withTransition">Whether to use transition animation</param>
        /// <param name="transitionDuration">Duration of the transition in seconds</param>
        public void ChangeColor(Color newColor, bool withTransition = false, float transitionDuration = 0.3f)
        {
            if (!withTransition || backgroundImage == null)
            {
                // Direct color change without transition
                Color = newColor;
                if (backgroundImage != null)
                {
                    backgroundImage.color = newColor;
                }
            }
            else
            {
                // Use transition
                StartCoroutine(TransitionColor(newColor, transitionDuration));
            }
        }

        /// <summary>
        /// Transition the color of the sticky note
        /// </summary>
        /// <param name="targetColor">Target color</param>
        /// <param name="duration">Transition duration in seconds</param>
        /// <returns>Coroutine</returns>
        private IEnumerator TransitionColor(Color targetColor, float duration)
        {
            if (backgroundImage == null)
            {
                Color = targetColor;
                yield break;
            }

            Color startColor = backgroundImage.color;
            float transitionTime = 0f;

            while (transitionTime < duration)
            {
                transitionTime += Time.deltaTime;
                float t = Mathf.Clamp01(transitionTime / duration);

                // Use smooth transition
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // Interpolate color
                Color newColor = Color.Lerp(startColor, targetColor, smoothT);

                // Apply new color
                backgroundImage.color = newColor;

                yield return null;
            }

            // Ensure final color is set exactly
            backgroundImage.color = targetColor;
            Color = targetColor;
        }

        /// <summary>
        /// Add strokes to this sticky note
        /// </summary>
        /// <param name="strokes">List of strokes to add</param>
        public void AddStrokes(List<Stroke> strokes)
        {
            if (strokes == null || strokes.Count == 0)
                return;

            // Get the height of the sticky note
            float height = Corners[0].y;

            attachedStrokes.AddRange(strokes);

            // Flatten and set the strokes parent to the sticky note
            foreach (var stroke in strokes)
            {
                stroke.Flatten(height + strokeHeightOffset);
                stroke.MeshObject.transform.SetParent(transform);
            }

            StartCoroutine(TextRecognitionCoroutine());
        }

        /// <summary>
        /// Generate the image of the strokes on the sticky note
        /// </summary>
        /// <param name="maxResolution">The maximum resolution (pixels)</param>
        /// <param name="saveToFile">Whether to save to file</param>
        /// <returns>The generated texture (Texture2D)</returns>
        public Texture2D GenerateImage(int maxResolution = 1024, bool saveToFile = true)
        {
            // Get the size of the sticky note
            Bounds bounds = GetBounds();
            float width = bounds.size.x;
            float height = bounds.size.z;

            float aspectRatio = width / height;
            int textureWidth, textureHeight;

            if (aspectRatio > 1)
            {
                textureWidth = maxResolution;
                textureHeight = Mathf.RoundToInt(maxResolution / aspectRatio);
            }
            else
            {
                textureHeight = maxResolution;
                textureWidth = Mathf.RoundToInt(maxResolution * aspectRatio);
            }

            textureWidth = Mathf.Max(textureWidth, 256);
            textureHeight = Mathf.Max(textureHeight, 256);

            Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            Color[] colors = new Color[textureWidth * textureHeight];

            // Fill the background with white
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.white;
            }
            texture.SetPixels(colors);


            int penThickness = Mathf.Max(2, Mathf.FloorToInt(textureWidth / 200));

            // Iterate through all strokes
            foreach (var stroke in attachedStrokes)
            {
                if (stroke.Points == null || stroke.Points.Count < 2)
                {
                    Debug.LogWarning("Ignoring invalid stroke: not enough points");
                    continue;
                }

                // Iterate through each point in the stroke
                for (int i = 0; i < stroke.Points.Count - 1; i++)
                {
                    Vector3 point1 = stroke.Points[i];
                    Vector3 point2 = stroke.Points[i + 1];

                    // Convert world coordinates to local coordinates
                    Vector3 localPoint1 = transform.InverseTransformPoint(point1);
                    Vector3 localPoint2 = transform.InverseTransformPoint(point2);

                    // Normalize the local coordinates to the [0,1] range
                    float normalizedX1 = (localPoint1.x + Width / 2) / Width;
                    float normalizedY1 = (localPoint1.y + Height / 2) / Height;
                    float normalizedX2 = (localPoint2.x + Width / 2) / Width;
                    float normalizedY2 = (localPoint2.y + Height / 2) / Height;

                    // Convert to pixel coordinates
                    int x1 = Mathf.FloorToInt(normalizedX1 * textureWidth);
                    int y1 = Mathf.FloorToInt(normalizedY1 * textureHeight);
                    int x2 = Mathf.FloorToInt(normalizedX2 * textureWidth);
                    int y2 = Mathf.FloorToInt(normalizedY2 * textureHeight);

                    // Ensure the coordinates are within the valid range
                    x1 = Mathf.Clamp(x1, 0, textureWidth - 1);
                    y1 = Mathf.Clamp(y1, 0, textureHeight - 1);
                    x2 = Mathf.Clamp(x2, 0, textureWidth - 1);
                    y2 = Mathf.Clamp(y2, 0, textureHeight - 1);

                    // Use the Bresenham algorithm to draw the line
                    ImageProcessingUtility.DrawLine(texture, x1, y1, x2, y2, Color.black, penThickness);
                }
            }

            // Apply the changes
            texture.Apply();

            if (saveToFile)
            {
                SaveTextureToFile(texture);
            }

            return texture;
        }

        /// <summary>
        /// Save the texture to a file
        /// </summary>
        /// <param name="texture">The texture to save</param>
        private void SaveTextureToFile(Texture2D texture)
        {
            ImageProcessingUtility.SaveTextureToFile(texture, "StickyNote", "StickyNoteImages");
        }

        /// <summary>
        /// Perform text recognition and create smart widget if needed
        /// </summary>
        private IEnumerator TextRecognitionCoroutine()
        {
            Texture2D stickyNoteImage = GenerateImage(512, false);

            SmartWidgetFactory factory = SmartWidgetFactory.Instance;
            if (factory == null)
            {
                Debug.LogError("SmartWidgetFactory instance not found");
                yield break;
            }

            string prompt = factory.GetRecognitionPrompt();

            bool isCompleted = false;
            string recognitionResult = "";

            ChatGPTRequest.Instance.SendRequest(
                prompt,
                stickyNoteImage,
                (response) =>
                {
                    recognitionResult = response;
                    isCompleted = true;
                }
            );

            yield return new WaitUntil(() => isCompleted);

            GameObject widgetObject = factory.CreateFromText(recognitionResult);
            if (widgetObject != null)
            {
                AttachSmartWidget(widgetObject);
            }
        }

        /// <summary>
        /// Attach smart widget to this sticky note
        /// </summary>
        /// <param name="widgetObject">Widget GameObject to attach</param>
        public void AttachSmartWidget(GameObject widgetObject)
        {
            // Remove any existing widget
            if (attachedWidget != null)
            {
                Destroy(attachedWidget);
            }

            // Set up new widget
            attachedWidget = widgetObject;
            widgetObject.transform.SetParent(transform, false);

            widgetObject.transform.localPosition = new Vector3(0, 0, -0.001f);

            RectTransform rectTransform = widgetObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localRotation = Quaternion.identity;
            }

            // Set parent reference
            SmartWidget smartWidget = widgetObject.GetComponent<SmartWidget>();
            if (smartWidget != null)
            {
                smartWidget.SetParentStickyNote(this);
            }

            // Start the widget attachment process with animations
            StartCoroutine(AnimateWidgetAttachment(widgetObject));
        }

        /// <summary>
        /// Animate the widget attachment process
        /// </summary>
        /// <param name="widgetObject">The widget to attach</param>
        /// <returns>Coroutine</returns>
        private IEnumerator AnimateWidgetAttachment(GameObject widgetObject)
        {
            // Initially hide the widget
            CanvasGroup canvasGroup = widgetObject.GetComponentInChildren<CanvasGroup>(true);
            canvasGroup.alpha = 0f;

            // Step 1: Hide all strokes
            yield return StartCoroutine(HideStrokes(0.3f));

            // Step 2: Resize the sticky note
            RectTransform widgetRectTransform = widgetObject.GetComponent<RectTransform>();
            if (widgetRectTransform != null && backgroundImage != null)
            {
                // Get the widget dimensions
                float targetWidth = widgetRectTransform.rect.width;
                float targetHeight = widgetRectTransform.rect.height;

                Vector2 currentSize = canvasRectTransform.sizeDelta;

                // Start the color transition in parallel
                StartCoroutine(TransitionColor(widgetBackgroundColor, 0.3f));

                // Animate the size change
                float resizeDuration = 0.3f;
                float resizeTime = 0f;

                while (resizeTime < resizeDuration)
                {
                    resizeTime += Time.deltaTime;
                    float t = Mathf.Clamp01(resizeTime / resizeDuration);

                    // Use smooth transition
                    float smoothT = Mathf.SmoothStep(0f, 1f, t);

                    // Interpolate the size
                    Vector2 newSize = new Vector2(
                        Mathf.Lerp(currentSize.x, targetWidth, smoothT),
                        Mathf.Lerp(currentSize.y, targetHeight, smoothT)
                    );

                    // Apply the new size
                    canvasRectTransform.sizeDelta = newSize;

                    // Update the collider size as well
                    UpdateColliderSize(targetWidth / 1000f, targetHeight / 1000f);

                    yield return null;
                }

                canvasRectTransform.sizeDelta = new Vector2(targetWidth, targetHeight);

                Width = targetWidth / 1000f;
                Height = targetHeight / 1000f;
            }

            // Set background image's parent to the widget and the index to 0
            backgroundImage.transform.SetParent(widgetObject.transform, false);
            backgroundImage.transform.SetSiblingIndex(0);


            // Step 3: Fade in the widget
            float fadeInDuration = 0.3f;
            float fadeTime = 0f;

            while (fadeTime < fadeInDuration)
            {
                fadeTime += Time.deltaTime;
                float t = Mathf.Clamp01(fadeTime / fadeInDuration);

                // Use smooth transition
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // Update canvas group alpha
                canvasGroup.alpha = smoothT;

                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Get all strokes attached to this sticky note
        /// </summary>
        public IReadOnlyList<Stroke> AttachedStrokes => attachedStrokes;

        /// <summary>
        /// Hide all strokes with a fade out animation
        /// </summary>
        /// <param name="fadeOutDuration">Duration of the fade out animation in seconds</param>
        /// <returns>Coroutine</returns>
        public IEnumerator HideStrokes(float fadeOutDuration = 0.5f)
        {
            if (attachedStrokes == null || attachedStrokes.Count == 0)
                yield break;

            List<Stroke> strokesToFade = new List<Stroke>(attachedStrokes);
            List<Material> strokeMaterials = new List<Material>();
            List<Color> originalColors = new List<Color>();

            // Get all stroke materials and their original colors
            foreach (var stroke in strokesToFade)
            {
                if (stroke.MeshObject != null)
                {
                    Renderer renderer = stroke.MeshObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Material mat = renderer.material;
                        strokeMaterials.Add(mat);
                        originalColors.Add(mat.color);
                    }
                }
            }


            float fadeOutTime = 0f;
            while (fadeOutTime < fadeOutDuration)
            {
                fadeOutTime += Time.deltaTime;
                float t = Mathf.Clamp01(fadeOutTime / fadeOutDuration);

                // Use smooth transition
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // Fade out stroke colors
                for (int i = 0; i < strokeMaterials.Count; i++)
                {
                    Color newColor = originalColors[i];
                    newColor.a = Mathf.Lerp(originalColors[i].a, 0f, smoothT);
                    strokeMaterials[i].color = newColor;
                }

                yield return null;
            }

            for (int i = 0; i < strokeMaterials.Count; i++)
            {
                Color newColor = originalColors[i];
                newColor.a = 0f;
                strokeMaterials[i].color = newColor;
            }

            foreach (var stroke in strokesToFade)
            {
                if (stroke.MeshObject != null)
                {
                    stroke.MeshObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Orient the sticky note towards a camera
        /// </summary>
        /// <param name="cam">Camera to face (defaults to main camera)</param>
        public void OrientTowardsCamera(Camera cam = null)
        {
            if (cam == null)
            {
                cam = Camera.main;
            }

            Vector3 camPos = cam.transform.position;
            Vector3 objPos = transform.position;
            Vector3 dirToCamera = camPos - objPos;

            if (dirToCamera != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(-dirToCamera);
                lookRotation = Quaternion.Euler(lookRotation.eulerAngles.x, lookRotation.eulerAngles.y, 0);
                transform.rotation = lookRotation;
            }
        }

        public void HideSmartWidget(System.Action onComplete = null)
        {
            if (attachedWidget == null)
            {
                onComplete?.Invoke();
                return;
            }
            StartCoroutine(HideWidgetCoroutine(attachedWidget, onComplete));
        }

        private IEnumerator HideWidgetCoroutine(GameObject widgetObject, System.Action onComplete)
        {

            // 1. Put backgroundImage back to canvas
            if (backgroundImage != null && canvas != null)
            {
                backgroundImage.transform.SetParent(canvas.transform, false);
                backgroundImage.transform.SetAsFirstSibling();
            }


            // 2. Get CanvasGroup
            CanvasGroup canvasGroup = widgetObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = widgetObject.AddComponent<CanvasGroup>();

            // 3. Fade out
            float fadeDuration = 0.3f;
            float fadeTime = 0f;
            float startAlpha = canvasGroup.alpha;
            while (fadeTime < fadeDuration)
            {
                fadeTime += Time.deltaTime;
                float t = Mathf.Clamp01(fadeTime / fadeDuration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }
            canvasGroup.alpha = 0f;

            // 4. Hide widget
            widgetObject.SetActive(false);

            // 5. Callback
            onComplete?.Invoke();
        }

        /// <summary>
        /// Apply text instantly without animation
        /// </summary>
        private void ApplyTextInstantly(string text)
        {
            StickyNoteUtils.SetupTextComponent(textComponent, autoSizeSettings);

            if (autoSizeSettings.enableAutoSizing)
            {
                Vector2 targetSize = StickyNoteUtils.CalculateOptimalTextSize(text, textComponent, autoSizeSettings);

                // Update size instantly
                canvasRectTransform.sizeDelta = targetSize;
                Width = targetSize.x / 1000f;
                Height = targetSize.y / 1000f;

                // Update collider
                UpdateColliderSize();
            }

            textComponent.text = text;
            Text = text;
        }

        /// <summary>
        /// Apply text with animation
        /// </summary>
        private void ApplyTextWithAnimation(string text, AnimationUtils.TextAnimationType animationType, float duration)
        {
            StickyNoteUtils.SetupTextComponent(textComponent, autoSizeSettings);

            if (autoSizeSettings.enableAutoSizing)
            {
                Vector2 targetSize = StickyNoteUtils.CalculateOptimalTextSize(text, textComponent, autoSizeSettings);

                // Start size animation
                sizeAnimationCoroutine = StartCoroutine(StickyNoteUtils.AnimateSizeChange(this, canvasRectTransform, targetSize, duration * 0.8f));

                // Start text animation with slight delay to sync with size change
                StartCoroutine(DelayedTextAnimation(text, animationType, duration, duration * 0.1f));
            }
            else
            {
                // Just animate text without size change
                textAnimationCoroutine = AnimationUtils.AnimateTextTransition(
                    this,
                    textComponent,
                    text,
                    animationType,
                    duration
                );
            }

            Text = text;
        }

        /// <summary>
        /// Update collider size to match dimensions
        /// </summary>
        /// <param name="width">Width in world units (optional, uses current Width if not provided)</param>
        /// <param name="height">Height in world units (optional, uses current Height if not provided)</param>
        public void UpdateColliderSize(float? width = null, float? height = null)
        {
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                float actualWidth = width ?? Width;
                float actualHeight = height ?? Height;
                boxCollider.size = new Vector3(actualWidth, actualHeight, 0.01f);
                boxCollider.center = new Vector3(0, 0, 0.0075f);
            }
        }

        /// <summary>
        /// Start text animation with a delay
        /// </summary>
        private IEnumerator DelayedTextAnimation(string text, AnimationUtils.TextAnimationType animationType, float duration, float delay)
        {
            yield return new WaitForSeconds(delay);

            textAnimationCoroutine = AnimationUtils.AnimateTextTransition(
                this,
                textComponent,
                text,
                animationType,
                duration
            );
        }
    }
}

