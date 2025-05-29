using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.IO;
using System.Text;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Handles communication with Whisper API for speech recognition.
    /// Converts AudioClip to WAV format and processes API responses.
    /// </summary>
    public class WhisperRequest : MonoBehaviour
    {
        /// <summary>
        /// Event triggered when transcription is completed successfully
        /// </summary>
        public event Action<string> OnTranscriptionSuccess;

        /// <summary>
        /// Event triggered when transcription fails
        /// </summary>
        public event Action<string> OnTranscriptionError;

        [Header("API Settings")]
        [Tooltip("Endpoint URL for the Whisper API")]
        [SerializeField] private string apiUrl = "http://localhost:8000/transcribe";

        [Tooltip("Language code for transcription (e.g. 'zh', 'en', or 'auto')")]
        [SerializeField] private string language = "zh";

        [Tooltip("Beam size for transcription model")]
        [SerializeField] private int beamSize = 5;

        [Tooltip("Apply voice activity detection filtering")]
        [SerializeField] private bool vadFilter = true;

        [Tooltip("Generate word-level timestamps")]
        [SerializeField] private bool wordTimestamps = true;

        /// <summary>
        /// Send an AudioClip to the Whisper API for transcription
        /// </summary>
        /// <param name="clip">AudioClip to transcribe</param>
        public void TranscribeAudio(AudioClip clip)
        {
            if (clip == null)
            {
                OnTranscriptionError?.Invoke("No audio clip provided");
                return;
            }

            StartCoroutine(ProcessAudioClip(clip));
        }

        private IEnumerator ProcessAudioClip(AudioClip clip)
        {
            // Convert AudioClip to WAV
            byte[] wavData = ConvertAudioClipToWAV(clip);
            if (wavData == null)
            {
                OnTranscriptionError?.Invoke("Failed to convert audio format");
                yield break;
            }

            // Create form with file data
            WWWForm form = new WWWForm();
            form.AddBinaryData("audio", wavData, "recording.wav", "audio/wav");
            form.AddField("language", language);
            form.AddField("beam_size", beamSize);
            form.AddField("vad_filter", vadFilter.ToString());
            form.AddField("word_timestamps", wordTimestamps.ToString());

            // Network request
            using (UnityWebRequest www = UnityWebRequest.Post(apiUrl, form))
            {
                // Send the request
                yield return www.SendWebRequest();

                // Process response
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"API request failed: {www.error}");
                    OnTranscriptionError?.Invoke($"API error: {www.error}");
                }
                else
                {
                    string json = www.downloadHandler.text;
                    ProcessApiResponse(json);
                }
            }
        }

        private void ProcessApiResponse(string json)
        {
            try
            {
                TranscriptionResponse response = JsonUtility.FromJson<TranscriptionResponse>(json);

                if (response != null && response.success)
                {
                    OnTranscriptionSuccess?.Invoke(response.text);
                }
                else
                {
                    OnTranscriptionError?.Invoke("Invalid API response format");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"JSON parse error: {ex.Message}");
                OnTranscriptionError?.Invoke("Failed to parse response");
            }
        }

        private byte[] ConvertAudioClipToWAV(AudioClip clip)
        {
            try
            {
                // Get audio data
                float[] samples = new float[clip.samples * clip.channels];
                clip.GetData(samples, 0);

                // Convert to 16-bit PCM
                byte[] pcmData = new byte[samples.Length * 2];
                int idx = 0;
                for (int i = 0; i < samples.Length; i++)
                {
                    short value = (short)(samples[i] * 32767);
                    byte[] bytes = BitConverter.GetBytes(value);
                    pcmData[idx++] = bytes[0];
                    pcmData[idx++] = bytes[1];
                }

                // Create WAV header
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] header = CreateWAVHeader(clip.samples, clip.channels, clip.frequency);
                    ms.Write(header, 0, header.Length);
                    ms.Write(pcmData, 0, pcmData.Length);
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error converting AudioClip to WAV: {ex.Message}");
                return null;
            }
        }

        private byte[] CreateWAVHeader(int sampleCount, int channels, int frequency)
        {
            int dataSize = sampleCount * channels * 2; // 16-bit samples
            int fileSize = 36 + dataSize;

            using (MemoryStream stream = new MemoryStream(44))
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII))
                {
                    // RIFF header
                    writer.Write(new char[] { 'R', 'I', 'F', 'F' });
                    writer.Write(fileSize);
                    writer.Write(new char[] { 'W', 'A', 'V', 'E' });

                    // Format chunk
                    writer.Write(new char[] { 'f', 'm', 't', ' ' });
                    writer.Write(16); // Chunk size
                    writer.Write((short)1); // Audio format (PCM)
                    writer.Write((short)channels);
                    writer.Write(frequency);
                    writer.Write(frequency * channels * 2); // Byte rate
                    writer.Write((short)(channels * 2)); // Block align
                    writer.Write((short)16); // Bits per sample

                    // Data chunk
                    writer.Write(new char[] { 'd', 'a', 't', 'a' });
                    writer.Write(dataSize);
                }
                return stream.ToArray();
            }
        }

        [Serializable]
        private class TranscriptionResponse
        {
            public bool success;
            public string text;
            public string language;
            public float language_probability;
            public float duration;
            public string model;
            public string device;
        }
    }
}