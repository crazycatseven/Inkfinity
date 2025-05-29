using UnityEngine;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Non-blocking audio recorder optimized for Quest/Android.
    /// Uses a continuous microphone recording with ring buffer for smooth operation.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioRecorder : MonoBehaviour
    {
        /// <summary>
        /// Event triggered when a recording is successfully processed
        /// </summary>
        public event Action<AudioClip> OnRecordingReady;

        /// <summary>
        /// Event triggered when recording starts
        /// </summary>
        public event Action<AudioClip> OnRecordingStarted;

        /// <summary>
        /// Event triggered when recording stops
        /// </summary>
        public event Action OnRecordingStopped;

        [Header("Recording Settings")]
        [Tooltip("Ring buffer length in seconds")]
        [SerializeField] private int ringBufferSeconds = 60;

        [Tooltip("Sample rate in Hz")]
        [SerializeField] private int sampleRate = 32000;

        [Header("Playback Settings")]
        [Tooltip("Should automatically play recording after processing")]
        [SerializeField] private bool autoPlayRecording = true;

        [Tooltip("Delay before auto-playback in seconds")]
        [SerializeField] private float autoPlayDelay = 2f;

        // Recording state
        private AudioClip micClip;          // Ring buffer
        private string micDevice;           // Selected microphone
        private AudioSource audioSrc;       // Audio source component
        private bool isRecording = false;   // Currently recording
        private int startSample = -1;       // Recording start position
        private AudioClip recordedClip;     // Last recorded clip

        /// <summary>
        /// Get the current ring buffer clip (useful for real-time visualization)
        /// </summary>
        public AudioClip CurrentMicClip => micClip;

        private void Start()
        {
            audioSrc = GetComponent<AudioSource>();
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("No microphone devices detected");
                enabled = false;
                return;
            }
            micDevice = PickQuestMic();
            StartCoroutine(InitializeMicrophone());
        }

        private IEnumerator InitializeMicrophone()
        {
            // Start microphone in looping mode
            micClip = Microphone.Start(micDevice, true, ringBufferSeconds, sampleRate);

            // Wait for microphone to start
            while (Microphone.GetPosition(micDevice) <= 0)
                yield return null;

            // Set audio source to play the buffer
            audioSrc.clip = micClip;
            audioSrc.loop = true;
            audioSrc.mute = true;
            audioSrc.Play();
        }

        private void OnDisable()
        {
            if (Microphone.IsRecording(micDevice))
                Microphone.End(micDevice);
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
        /// Begin recording audio
        /// </summary>
        public void StartRecording()
        {
            if (isRecording || !Microphone.IsRecording(micDevice))
                return;

            isRecording = true;
            startSample = Microphone.GetPosition(micDevice);

            // Notify listeners that recording has started
            OnRecordingStarted?.Invoke(micClip);
        }

        /// <summary>
        /// Stop recording and process the audio
        /// </summary>
        public void StopRecording()
        {
            if (!isRecording)
                return;

            isRecording = false;

            // Notify listeners that recording has stopped
            OnRecordingStopped?.Invoke();

            // Calculate segment to extract from ring buffer
            int endSample = Microphone.GetPosition(micDevice);
            int totalSamples = micClip.samples;
            int length = (endSample >= startSample)
                       ? endSample - startSample
                       : totalSamples - startSample + endSample;

            // Extract audio data in main thread (Unity API restriction)
            float[] ring = new float[micClip.samples];
            micClip.GetData(ring, 0);

            // Process audio data in background thread
            Task.Run(() =>
            {
                try
                {
                    // Extract segment from ring buffer
                    float[] buffer = new float[length];
                    int firstPart = Math.Min(length, ring.Length - startSample);
                    Array.Copy(ring, startSample, buffer, 0, firstPart);
                    if (length > firstPart)
                        Array.Copy(ring, 0, buffer, firstPart, length - firstPart);

                    // Return to main thread to create AudioClip
                    UnityMainThreadDispatcher.Enqueue(() =>
                    {
                        // Create clip from processed data
                        var clip = AudioClip.Create("Recording", length, 1, sampleRate, false);
                        clip.SetData(buffer, 0);
                        recordedClip = clip;

                        // Auto-play if enabled
                        if (autoPlayRecording)
                            StartCoroutine(PlayRecordingAfterDelay(autoPlayDelay));

                        // Notify listeners
                        OnRecordingReady?.Invoke(recordedClip);
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Audio processing error: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Get the most recently recorded audio clip
        /// </summary>
        public AudioClip GetLastRecording() => recordedClip;

        /// <summary>
        /// Play the recorded audio clip
        /// </summary>
        public void PlayRecording()
        {
            if (recordedClip == null || audioSrc == null)
                return;

            audioSrc.clip = recordedClip;
            audioSrc.mute = false;
            audioSrc.loop = false;
            audioSrc.Play();
        }

        private IEnumerator PlayRecordingAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (recordedClip == null) yield break;

            PlayRecording();
        }

        // Find suitable microphone for Quest devices
        private static string PickQuestMic()
        {
            foreach (string dev in Microphone.devices)
            {
                string u = dev.ToUpperInvariant();
                if (u.Contains("OCULUS") || u.Contains("META") || u.Contains("ANDROID"))
                    return dev;
            }
            return Microphone.devices[0];
        }
    }
}
