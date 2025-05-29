using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Microphone widget for the sticky note
    /// </summary>
    public class MicrophoneWidget : SmartWidget
    {
        [Header("UI References")]
        [SerializeField] private Button microphoneButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Image microphoneIcon;
        [SerializeField] private Sprite defaultMicIcon;
        [SerializeField] private Sprite loadingIcon;

        [Header("Animation Settings")]
        [SerializeField] private float rotationSpeed = 100f;
        [SerializeField] private AnimationUtils.TextAnimationType textAnimationType = AnimationUtils.TextAnimationType.Fade;
        [SerializeField] private float textAnimationDuration = 0.17f;

        [Header("Audio")]
        [SerializeField] private AudioClip startRecordingSound;
        [SerializeField] private AudioClip stopRecordingSound;

        private SpeechRecognizer speechRecognizer;
        private bool isLoading = false;
        private Coroutine textAnimationCoroutine;

        private void Awake()
        {
            if (microphoneButton != null)
            {
                microphoneButton.onClick.AddListener(OnMicButtonClicked);
            }

            if (microphoneIcon != null && defaultMicIcon != null)
            {
                microphoneIcon.sprite = defaultMicIcon;
            }

            SetStatus("Listening...");
        }

        private void Start()
        {
            speechRecognizer = FindFirstObjectByType<SpeechRecognizer>();
            if (startRecordingSound != null)
            {
                AudioSource.PlayClipAtPoint(startRecordingSound, transform.position);
            }
        }

        private void Update()
        {
            if (isLoading && microphoneIcon != null && loadingIcon != null)
            {
                microphoneIcon.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
            }
        }

        public override void Initialize(string recognizedText)
        {

        }

        private void OnMicButtonClicked()
        {
            if (speechRecognizer != null)
            {
                speechRecognizer.StopRecording();

                if (stopRecordingSound != null)
                {
                    AudioSource.PlayClipAtPoint(stopRecordingSound, transform.position);
                }

                isLoading = true;
                SetLoadingState(true);
            }
        }

        private void SetLoadingState(bool loading)
        {
            if (microphoneIcon == null) return;

            isLoading = loading;

            if (loading && loadingIcon != null)
            {
                microphoneIcon.sprite = loadingIcon;
            }
            else if (!loading && defaultMicIcon != null)
            {
                microphoneIcon.sprite = defaultMicIcon;
                microphoneIcon.transform.rotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Sets status text with animation effect
        /// </summary>
        public void AnimateStatus(string text)
        {
            if (statusText == null) return;

            if (textAnimationCoroutine != null)
            {
                StopCoroutine(textAnimationCoroutine);
            }

            textAnimationCoroutine = AnimationUtils.AnimateTextTransition(
                this,
                statusText,
                text,
                textAnimationType,
                textAnimationDuration
            );
        }

        /// <summary>
        /// Sets status text directly without animation
        /// </summary>
        public void SetStatus(string text)
        {
            if (statusText != null)
                statusText.text = text;
        }
    }
}