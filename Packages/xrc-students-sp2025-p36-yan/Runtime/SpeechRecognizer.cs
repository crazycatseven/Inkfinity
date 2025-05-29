using UnityEngine;
using System;
using System.Collections;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Coordinates audio recording and speech recognition.
    /// Acts as a high-level controller for the speech recognition pipeline.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SpeechRecognizer : MonoBehaviour
    {
        [Header("Components")]
        [Tooltip("Reference to AudioRecorder component")]
        [SerializeField] private AudioRecorder audioRecorder;

        [Tooltip("Reference to WhisperRequest component")]
        [SerializeField] private WhisperRequest whisperRequest;

        [Header("Settings")]
        [Tooltip("Automatically transcribe after recording stops")]
        [SerializeField] private bool autoTranscribeAfterRecording = true;

        /// <summary>
        /// Event triggered when the recording status changes
        /// </summary>
        public event Action<bool> OnRecordingStatusChanged;

        /// <summary>
        /// Event triggered when a transcription result is received
        /// </summary>
        public event Action<string> OnTranscriptionResult;

        private bool isRecording = false;
        private AudioSource audioSource;

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
            SetupComponents();
            SetupEventHandlers();
        }

        private void OnDestroy()
        {
            UnregisterEvents();
        }

        private void SetupComponents()
        {
            // Auto-create AudioRecorder if needed
            if (audioRecorder == null)
            {
                audioRecorder = GetComponent<AudioRecorder>();
                if (audioRecorder == null)
                    audioRecorder = gameObject.AddComponent<AudioRecorder>();
            }

            // Auto-create WhisperRequest if needed
            if (whisperRequest == null)
            {
                whisperRequest = GetComponent<WhisperRequest>();
                if (whisperRequest == null)
                    whisperRequest = gameObject.AddComponent<WhisperRequest>();
            }
        }

        private void SetupEventHandlers()
        {
            if (audioRecorder != null)
            {
                audioRecorder.OnRecordingReady += HandleRecordingReady;
            }
            else
            {
                Debug.LogError("SpeechRecognizer requires an AudioRecorder component");
            }

            if (whisperRequest != null)
            {
                whisperRequest.OnTranscriptionSuccess += HandleTranscriptionSuccess;
                whisperRequest.OnTranscriptionError += HandleTranscriptionError;
            }
            else
            {
                Debug.LogError("SpeechRecognizer requires a WhisperRequest component");
            }
        }

        private void UnregisterEvents()
        {
            if (audioRecorder != null)
                audioRecorder.OnRecordingReady -= HandleRecordingReady;

            if (whisperRequest != null)
            {
                whisperRequest.OnTranscriptionSuccess -= HandleTranscriptionSuccess;
                whisperRequest.OnTranscriptionError -= HandleTranscriptionError;
            }
        }

        /// <summary>
        /// Start recording audio
        /// </summary>
        public void StartRecording()
        {
            if (audioRecorder != null && !isRecording)
            {
                audioRecorder.StartRecording();
                isRecording = true;
                OnRecordingStatusChanged?.Invoke(true);
            }
        }

        /// <summary>
        /// Stop recording audio
        /// </summary>
        public void StopRecording()
        {
            if (audioRecorder != null && isRecording)
            {
                audioRecorder.StopRecording();
                isRecording = false;
                OnRecordingStatusChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// Toggle between recording and stopped states
        /// </summary>
        public void ToggleRecording()
        {
            if (isRecording)
                StopRecording();
            else
                StartRecording();
        }

        /// <summary>
        /// Manually send the last recording for transcription
        /// </summary>
        public void TranscribeLastRecording()
        {
            if (audioRecorder != null && whisperRequest != null)
            {
                AudioClip clip = audioRecorder.GetLastRecording();
                if (clip != null)
                {
                    whisperRequest.TranscribeAudio(clip);
                }
            }
        }

        private void HandleRecordingReady(AudioClip clip)
        {
            if (autoTranscribeAfterRecording && whisperRequest != null)
            {
                whisperRequest.TranscribeAudio(clip);
            }
        }

        private void HandleTranscriptionSuccess(string text)
        {
            OnTranscriptionResult?.Invoke(text);
        }

        private void HandleTranscriptionError(string errorMessage)
        {
            Debug.LogError($"Transcription error: {errorMessage}");
        }

        /// <summary>
        /// Play the last recorded audio
        /// </summary>
        public void PlayLastRecording()
        {
            if (audioRecorder != null)
                audioRecorder.PlayRecording();
        }

    }
}