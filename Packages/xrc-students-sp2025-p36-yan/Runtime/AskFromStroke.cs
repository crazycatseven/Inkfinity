using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using XRC.Students.Sp2025.P36.Yan;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Handles image recognition requests from strokes and manages sticky note feedback.
    /// </summary>
    public class AskFromStroke : MonoBehaviour
    {
        [SerializeField]
        private MxInkHandler mxInkHandler;
        [SerializeField]
        private Vector3 textOffset;
        [SerializeField]
        private MRImageViewerManager mrImageViewerManager;
        [SerializeField]
        private VRCameraCaptureTexture vrCameraCaptureTexture;

        [SerializeField]
        private bool overlayVRCamera = false;
        [SerializeField]
        private float defaultDelay = 0.2f;
        [SerializeField]
        private float promptCaptureDelay = 0.5f;
        [SerializeField]
        private Color resultNoteColor = new Color(1f, 0.92f, 0.7f, 1f);
        [SerializeField]
        private Color processingNoteColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField]
        private Color processingTextColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        [SerializeField]
        private Color resultTextColor = new Color(0f, 0f, 0f, 1f);
        [SerializeField]
        private bool saveDebugImages = false;

        [Header("Crop Settings")]
        [SerializeField, Tooltip("Minimum width for cropped regions (pixels)")]
        private int minCropWidth = 200;
        [SerializeField, Tooltip("Minimum height for cropped regions (pixels)")]
        private int minCropHeight = 200;

        [Header("Speech Recognition")]
        [SerializeField]
        private SpeechRecognizer speechRecognizer;

        [Header("Recognition Prompts")]
        [SerializeField, Tooltip("Default prompt for image recognition")]
        [TextArea(3, 10)]
        private string defaultRecognitionPrompt = "Identify the object in this image. Respond in English with 10 words or less.";

        [SerializeField, Tooltip("Prompt for multi-image recognition (format: {0} = first image, {1} = second image)")]
        [TextArea(3, 10)]
        private string multiImagePrompt = "The first image shows the target object. First recognize the handwritten content in the second image as a question, then answer this question about the object in the first image. Answer the question directly without describing or explaining what the handwritten content is.";

        private StrokeManager strokeManager;
        private StickyNoteManager stickyNoteManager;
        private StickyNote recognitionNote;
        private int targetStrokeIndex = -1;  // Index of the stroke used for target selection
        private bool isWaitingForPrompt = false;  // Flag to indicate if we're waiting for prompt strokes
        private List<Stroke> promptStrokes = new List<Stroke>();  // Strokes used for prompt

        /// <summary>
        /// Query state machine states
        /// </summary>
        private enum QueryState
        {
            Idle,               // Waiting for user input
            PromptWaiting,      // Waiting for prompt after target selection
            VoicePromptWaiting, // Waiting for voice prompt after target selection
            Processing          // Processing request
        }

        private QueryState currentState = QueryState.Idle;

        private void Start()
        {
            strokeManager = StrokeManager.Instance;
            stickyNoteManager = StickyNoteManager.Instance;

            // Auto-find SpeechRecognizer if not assigned
            if (speechRecognizer == null)
                speechRecognizer = FindFirstObjectByType<SpeechRecognizer>();

            // Register for speech recognition events
            if (speechRecognizer != null)
            {
                speechRecognizer.OnTranscriptionResult += HandleSpeechRecognitionResult;
                speechRecognizer.OnRecordingStatusChanged += HandleRecordingStatusChanged;
            }
        }

        private void OnDestroy()
        {
            // Unregister speech events
            if (speechRecognizer != null)
            {
                speechRecognizer.OnTranscriptionResult -= HandleSpeechRecognitionResult;
                speechRecognizer.OnRecordingStatusChanged -= HandleRecordingStatusChanged;
            }
        }

        /// <summary>
        /// Toggles between query states based on current context
        /// </summary>
        public void ToggleQueryState()
        {
            switch (currentState)
            {
                case QueryState.Idle:
                    if (strokeManager != null && strokeManager.GetLastStroke() != null)
                    {
                        StartPromptQuery();
                        currentState = QueryState.PromptWaiting;
                    }
                    else
                    {
                        StartImageRecognition();
                        currentState = QueryState.Processing;
                    }
                    break;

                case QueryState.PromptWaiting:
                    CompletePromptQuery();
                    currentState = QueryState.Processing;
                    break;

                case QueryState.VoicePromptWaiting:
                    // Toggle voice recording
                    if (speechRecognizer != null)
                    {
                        speechRecognizer.ToggleRecording();
                    }
                    break;

                case QueryState.Processing:
                    Debug.Log("Already processing a request, please wait...");
                    break;
            }
        }

        /// <summary>
        /// Starts voice-based query flow - called from Marking Menu
        /// </summary>
        public void StartVoiceQuery()
        {
            // Check if we have a stroke to target and a speech recognizer
            if (strokeManager == null || strokeManager.GetLastStroke() == null || speechRecognizer == null)
            {
                Debug.LogWarning("Cannot start voice query - missing stroke or speech recognizer");
                return;
            }

            // Setup the target stroke index
            targetStrokeIndex = strokeManager.GetCurrentStrokeIndex();

            // Create feedback sticky note
            Vector3 averagePosition = CalculateAveragePosition();
            recognitionNote = stickyNoteManager.CreateStickyNoteWithText("", averagePosition + textOffset, processingNoteColor);
            recognitionNote.OrientTowardsCamera();

            // Create microphone widget
            var microphoneWidget = SmartWidgetFactory.Instance.CreateMicrophoneWidget();

            if (microphoneWidget != null)
            {
                recognitionNote.AttachSmartWidget(microphoneWidget);
            }

            // Update state and start recording
            currentState = QueryState.VoicePromptWaiting;
            speechRecognizer.StartRecording();
        }

        private void HandleRecordingStatusChanged(bool isRecording)
        {
            if (currentState == QueryState.VoicePromptWaiting && isRecording)
            {
                if (recognitionNote != null)
                {
                    recognitionNote.GetComponentInChildren<MicrophoneWidget>().SetStatus("Listening...");
                }
            }

            if (currentState == QueryState.VoicePromptWaiting && !isRecording)
            {
                if (recognitionNote != null)
                {
                    recognitionNote.GetComponentInChildren<MicrophoneWidget>().SetStatus("Processing...");
                }
            }
        }

        private void HandleSpeechRecognitionResult(string result)
        {
            if (currentState != QueryState.VoicePromptWaiting)
                return;

            // Update UI to indicate processing
            if (recognitionNote != null)
            {
                recognitionNote.GetComponentInChildren<MicrophoneWidget>().SetStatus(result);
            }

            // Process the voice prompt
            StartCoroutine(ProcessVoicePromptQuery(result));
            currentState = QueryState.Processing;
        }

        /// <summary>
        /// Starts image recognition with specified delay
        /// </summary>
        public void StartImageRecognition(float delay = -1)
        {
            if (delay < 0)
            {
                delay = defaultDelay;
            }
            StartCoroutine(DelayedRecognizeImage(delay));
        }

        private IEnumerator DelayedRecognizeImage(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartCoroutine(RecognizeImage());
        }

        /// <summary>
        /// Starts the prompt-based query flow
        /// </summary>
        public void StartPromptQuery()
        {
            if (strokeManager == null || strokeManager.GetLastStroke() == null)
            {
                Debug.LogWarning("No stroke available for prompt query");
                return;
            }

            targetStrokeIndex = strokeManager.GetCurrentStrokeIndex();
            isWaitingForPrompt = true;
            promptStrokes.Clear();

            Vector3 averagePosition = CalculateAveragePosition();
            recognitionNote = stickyNoteManager.CreateStickyNoteWithText("Write your prompt...", averagePosition + textOffset, processingNoteColor);
            recognitionNote.OrientTowardsCamera();
            recognitionNote.SetTextColor(processingTextColor);
        }

        /// <summary>
        /// Completes prompt query and initiates processing
        /// </summary>
        public void CompletePromptQuery()
        {
            if (!isWaitingForPrompt || targetStrokeIndex < 0)
            {
                Debug.LogWarning("No prompt query in progress");
                return;
            }

            // Update UI to indicate processing
            recognitionNote.SetText("Processing request...");

            // Capture and process images
            StartCoroutine(ProcessPromptQuery());
        }

        /// <summary>
        /// Ensures crop rectangle meets minimum size requirements
        /// </summary>
        /// <param name="cropRect">Original crop rectangle</param>
        /// <param name="imageWidth">Full image width</param>
        /// <param name="imageHeight">Full image height</param>
        /// <returns>Adjusted crop rectangle with minimum size guaranteed</returns>
        private Rect EnsureMinimumCropSize(Rect cropRect, int imageWidth, int imageHeight)
        {
            float adjustedWidth = Mathf.Max(cropRect.width, minCropWidth);
            float adjustedHeight = Mathf.Max(cropRect.height, minCropHeight);

            // Calculate center of original rect
            Vector2 center = cropRect.center;

            // Create new rect centered on the same point
            Rect adjustedRect = new Rect(
                center.x - adjustedWidth / 2,
                center.y - adjustedHeight / 2,
                adjustedWidth,
                adjustedHeight
            );

            // Ensure the adjusted rect stays within image bounds
            if (adjustedRect.x < 0)
            {
                adjustedRect.x = 0;
            }
            if (adjustedRect.y < 0)
            {
                adjustedRect.y = 0;
            }
            if (adjustedRect.xMax > imageWidth)
            {
                adjustedRect.x = imageWidth - adjustedRect.width;
            }
            if (adjustedRect.yMax > imageHeight)
            {
                adjustedRect.y = imageHeight - adjustedRect.height;
            }

            // Final clamp to ensure we don't exceed image bounds
            adjustedRect.x = Mathf.Clamp(adjustedRect.x, 0, imageWidth - adjustedRect.width);
            adjustedRect.y = Mathf.Clamp(adjustedRect.y, 0, imageHeight - adjustedRect.height);
            adjustedRect.width = Mathf.Min(adjustedRect.width, imageWidth);
            adjustedRect.height = Mathf.Min(adjustedRect.height, imageHeight);

            return adjustedRect;
        }

        private IEnumerator ProcessPromptQuery()
        {
            // Save index to prevent issues after state reset
            int strokeIndexToRemove = targetStrokeIndex;
            bool imagesCaptured = false;
            Texture2D targetImage = null;
            Texture2D promptImage = null;

            // First capture all images while strokes still exist
            Texture2D fullImage = null;

            if (overlayVRCamera)
            {
                fullImage = mrImageViewerManager.GetCombinedTexture2D();
            }
            else
            {
                fullImage = mrImageViewerManager.GetWebCamTexture2D();
            }

            if (fullImage != null)
            {
                // Capture target image
                var targetStroke = strokeManager.GetStrokeAtIndex(strokeIndexToRemove);
                if (targetStroke != null && vrCameraCaptureTexture != null)
                {
                    Rect cropRect = StrokeGeometryUtility.CalculateCropRectFromStroke(targetStroke, fullImage.width, fullImage.height, vrCameraCaptureTexture.GetTargetCamera());
                    cropRect = EnsureMinimumCropSize(cropRect, fullImage.width, fullImage.height);
                    targetImage = ImageProcessingUtility.CropTexture(fullImage, cropRect);
                }

                // Capture prompt image
                promptStrokes = strokeManager.GetStrokesAfterIndex(strokeIndexToRemove);
                if (promptStrokes.Count > 0)
                {
                    Bounds combinedBounds = StrokeGeometryUtility.GetCombinedBounds(promptStrokes);
                    Camera cam = vrCameraCaptureTexture != null ? vrCameraCaptureTexture.GetTargetCamera() : Camera.main;
                    Rect promptCropRect = StrokeGeometryUtility.GetScreenRectFromBounds(combinedBounds, cam, fullImage.width, fullImage.height, 50f);
                    promptCropRect = EnsureMinimumCropSize(promptCropRect, fullImage.width, fullImage.height);
                    promptImage = ImageProcessingUtility.CropTexture(fullImage, promptCropRect);
                }

                imagesCaptured = (targetImage != null && promptImage != null);
            }

            // After capturing images, remove strokes
            if (strokeIndexToRemove >= 0)
            {
                strokeManager?.RemoveStrokesFromIndex(strokeIndexToRemove);
            }

            // Short delay to allow UI to update
            yield return new WaitForSeconds(promptCaptureDelay);

            // Process API request
            string result = "";
            bool isCompleted = false;
            bool hasError = false;
            string errorMsg = "";
            bool needWaitForResponse = false;

            try
            {
                if (!imagesCaptured)
                {
                    hasError = true;
                    errorMsg = "Error: Failed to capture images";
                }
                else
                {
                    // Create and send multi-image request
                    var promptSequence = new List<(string text, Texture2D image)>
                    {
                        (multiImagePrompt, targetImage),
                        ("", promptImage)
                    };
                    ChatGPTRequest.Instance.SendMultiImageRequest(
                        promptSequence,
                        (response) =>
                        {
                            result = response;
                            isCompleted = true;
                        }
                    );
                    needWaitForResponse = true;
                }
            }
            catch (System.Exception ex)
            {
                hasError = true;
                errorMsg = $"Error: {ex.Message}";
                Debug.LogError($"Error in ProcessPromptQuery: {ex.Message}");
            }
            finally
            {
                ResetQueryState();
            }

            if (hasError)
            {
                recognitionNote.SetText(errorMsg);
                yield break;
            }

            if (needWaitForResponse)
            {
                float timeoutDuration = 12f;
                float elapsedTime = 0f;
                while (!isCompleted && elapsedTime < timeoutDuration)
                {
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                if (!isCompleted)
                {
                    recognitionNote.SetText("Request timed out");
                    yield break;
                }
                recognitionNote.ChangeColor(resultNoteColor, true);
                recognitionNote.SetText(result, AnimationUtils.TextAnimationType.Fade, 0.6f);
                recognitionNote.SetTextColor(resultTextColor);
            }
        }

        private IEnumerator RecognizeImage()
        {
            Vector3 averagePosition = CalculateAveragePosition();
            recognitionNote = stickyNoteManager.CreateStickyNoteWithText("Recognizing...", averagePosition + textOffset, processingNoteColor);
            recognitionNote.OrientTowardsCamera();
            recognitionNote.SetTextColor(processingTextColor);

            Texture2D currentImage = null;
            bool isCompleted = false;
            string result = "";
            bool isRequestSent = false;
            bool hasError = false;
            string errorMsg = "";

            try
            {
                Texture2D fullImage;

                if (overlayVRCamera)
                {
                    fullImage = mrImageViewerManager.GetCombinedTexture2D();
                }
                else
                {
                    fullImage = mrImageViewerManager.GetWebCamTexture2D();
                }

                if (fullImage == null)
                {
                    hasError = true;
                    errorMsg = "Error: Failed to get image";
                }
                else
                {
                    var lastStroke = strokeManager.GetLastStroke();
                    if (lastStroke != null && vrCameraCaptureTexture != null)
                    {
                        Rect cropRect = StrokeGeometryUtility.CalculateCropRectFromStroke(lastStroke, fullImage.width, fullImage.height, vrCameraCaptureTexture.GetTargetCamera());
                        cropRect = EnsureMinimumCropSize(cropRect, fullImage.width, fullImage.height);
                        currentImage = ImageProcessingUtility.CropTexture(fullImage, cropRect);
                        if (saveDebugImages) ImageProcessingUtility.SaveTextureToFile(currentImage, "Cropped");
                    }
                    else
                    {
                        currentImage = fullImage;
                    }

                    ChatGPTRequest.Instance.SendRequest(
                        defaultRecognitionPrompt,
                        currentImage,
                        (response) =>
                        {
                            result = response;
                            isCompleted = true;
                        }
                    );
                    isRequestSent = true;
                    strokeManager?.RemoveLastStroke();
                }
            }
            catch (System.Exception ex)
            {
                hasError = true;
                errorMsg = $"Error: {ex.Message}";
                Debug.LogError($"Unexpected error in RecognizeImage: {ex.Message}");
            }
            finally
            {
                if (currentState == QueryState.Processing)
                {
                    ResetQueryState();
                }
            }

            if (hasError)
            {
                recognitionNote.SetText(errorMsg);
                yield break;
            }

            if (isRequestSent)
            {
                float timeoutDuration = 12f;
                float elapsedTime = 0f;
                while (!isCompleted && elapsedTime < timeoutDuration)
                {
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                if (!isCompleted)
                {
                    recognitionNote.SetText("Request timed out");
                    yield break;
                }
                recognitionNote.ChangeColor(resultNoteColor, true);
                recognitionNote.SetText(result, AnimationUtils.TextAnimationType.Fade, 0.6f);
                recognitionNote.SetTextColor(resultTextColor);
            }
        }

        /// <summary>
        /// Resets the query state
        /// </summary>
        public void ResetQueryState()
        {
            currentState = QueryState.Idle;
            isWaitingForPrompt = false;
            targetStrokeIndex = -1;
            promptStrokes.Clear();
        }

        private IEnumerator ProcessVoicePromptQuery(string voicePrompt)
        {
            // Save index to prevent issues after state reset
            int strokeIndexToRemove = targetStrokeIndex;
            bool imagesCaptured = false;
            Texture2D targetImage = null;

            // First capture image while stroke still exists
            Texture2D fullImage;

            if (overlayVRCamera)
            {
                fullImage = mrImageViewerManager.GetCombinedTexture2D();
            }
            else
            {
                fullImage = mrImageViewerManager.GetWebCamTexture2D();
            }

            if (fullImage != null)
            {
                // Capture target image
                var targetStroke = strokeManager.GetStrokeAtIndex(strokeIndexToRemove);
                if (targetStroke != null && vrCameraCaptureTexture != null)
                {
                    Rect cropRect = StrokeGeometryUtility.CalculateCropRectFromStroke(targetStroke, fullImage.width, fullImage.height, vrCameraCaptureTexture.GetTargetCamera());
                    cropRect = EnsureMinimumCropSize(cropRect, fullImage.width, fullImage.height);
                    targetImage = ImageProcessingUtility.CropTexture(fullImage, cropRect);
                    imagesCaptured = (targetImage != null);
                    if (saveDebugImages) ImageProcessingUtility.SaveTextureToFile(targetImage, "Target");
                }
            }

            // After capturing images, remove strokes
            if (strokeIndexToRemove >= 0)
            {
                strokeManager?.RemoveStrokesFromIndex(strokeIndexToRemove);
            }

            // Process API request
            string result = "";
            bool isCompleted = false;
            bool hasError = false;
            string errorMsg = "";

            try
            {
                if (!imagesCaptured)
                {
                    hasError = true;
                    errorMsg = "Error: Failed to capture image";
                }
                else
                {
                    // Create and send request with voice prompt
                    ChatGPTRequest.Instance.SendRequest(
                        voicePrompt,
                        targetImage,
                        (response) =>
                        {
                            result = response;
                            isCompleted = true;
                        }
                    );
                }
            }
            catch (System.Exception ex)
            {
                hasError = true;
                errorMsg = $"Error: {ex.Message}";
                Debug.LogError($"Error in ProcessVoicePromptQuery: {ex.Message}");
            }
            finally
            {
                ResetQueryState();
            }

            if (hasError)
            {
                recognitionNote.SetText(errorMsg);
                yield break;
            }

            // Wait for response
            float timeoutDuration = 12f;
            float elapsedTime = 0f;
            while (!isCompleted && elapsedTime < timeoutDuration)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (!isCompleted)
            {
                recognitionNote.SetText("Request timed out");
                yield break;
            }

            recognitionNote.HideSmartWidget(
                () =>
                {
                    recognitionNote.ChangeColor(resultNoteColor, true);
                    recognitionNote.SetText(result, AnimationUtils.TextAnimationType.Fade, 0.6f);
                    recognitionNote.SetTextColor(resultTextColor);
                }
            );
        }

        #region Geometry Utilities

        private Vector3 CalculateAveragePosition()
        {
            if (strokeManager == null)
            {
                return Vector3.zero;
            }

            var lastStroke = strokeManager.GetLastStroke();
            if (lastStroke == null)
            {
                return Vector3.zero;
            }

            return StrokeGeometryUtility.CalculateAveragePosition(lastStroke);
        }

        #endregion
    }
}

