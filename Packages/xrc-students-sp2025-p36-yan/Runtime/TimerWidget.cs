using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Timer widget for countdown.
    /// </summary>
    public class TimerWidget : SmartWidget
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Button toggleButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button completeButton;
        [SerializeField] private Image progressBar;

        [Header("Button Icons")]
        [SerializeField] private Sprite pauseIcon;
        [SerializeField] private Sprite continueIcon;

        [Header("Settings")]
        [SerializeField] private float duration = 300f;
        [SerializeField] private AudioClip timerEndSound;

        private float timeRemaining;
        private float initialDuration;
        private bool isRunning;
        private AudioSource audioSource;
        private string timerTitle = "Timer";

        /// <summary>
        /// Timer completed event.
        /// </summary>
        public event Action TimerCompleted;

        /// <summary>
        /// Initialize components.
        /// </summary>
        private void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            initialDuration = duration;
            timeRemaining = duration;

            // Setup button listeners
            if (toggleButton) toggleButton.onClick.AddListener(ToggleTimer);
            if (resetButton) resetButton.onClick.AddListener(ResetTimer);
            if (completeButton) completeButton.onClick.AddListener(CompleteTimer);

            // Configure progress bar
            if (progressBar)
            {
                progressBar.type = Image.Type.Filled;
                progressBar.fillMethod = Image.FillMethod.Radial360;
                progressBar.fillOrigin = (int)Image.Origin360.Top;
                progressBar.fillClockwise = true;
            }

            // Start after Initialize
            isRunning = false;
            UpdateUI();
        }

        /// <summary>
        /// Initialize widget from text.
        /// </summary>
        public override void Initialize(string recognizedText)
        {
            // Format: TIMER/DESCRIPTION|HH:MM:SS
            string[] parts = recognizedText.Split('/');
            if (parts.Length < 2) return;

            // Get data portion
            string data = parts[1];
            string[] dataParts = data.Split('|');

            // Parse title/description
            if (dataParts.Length > 0 && !string.IsNullOrEmpty(dataParts[0]))
            {
                timerTitle = dataParts[0].Trim();
                if (titleText != null)
                {
                    titleText.text = timerTitle;
                }
            }

            // Parse time
            if (dataParts.Length > 1)
            {
                string timeStr = dataParts[1].Trim();
                int seconds = ParseTimeString(timeStr);
                if (seconds > 0)
                {
                    SetDuration(seconds);
                }
            }

            // Auto-start timer
            StartTimer();
        }

        /// <summary>
        /// Parse time string.
        /// </summary>
        private int ParseTimeString(string timeStr)
        {
            try
            {
                string[] timeParts = timeStr.Split(':');
                if (timeParts.Length == 3)
                {
                    // HH:MM:SS
                    int hours = int.Parse(timeParts[0]);
                    int minutes = int.Parse(timeParts[1]);
                    int seconds = int.Parse(timeParts[2]);
                    return hours * 3600 + minutes * 60 + seconds;
                }
                else if (timeParts.Length == 2)
                {
                    // MM:SS (assuming this is minutes:seconds)
                    int minutes = int.Parse(timeParts[0]);
                    int seconds = int.Parse(timeParts[1]);
                    return minutes * 60 + seconds;
                }
                else if (timeParts.Length == 1)
                {
                    // Single number, assume minutes
                    int minutes = int.Parse(timeParts[0]);
                    return minutes * 60;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing time string: {e.Message}");
            }

            return 300; // Default to 5 minutes if parsing fails
        }

        /// <summary>
        /// Set timer duration.
        /// </summary>
        /// <param name="seconds">Duration in seconds.</param>
        public void SetDuration(int seconds)
        {
            initialDuration = seconds;
            duration = seconds;
            timeRemaining = seconds;
            UpdateUI();
        }

        /// <summary>
        /// Start timer.
        /// </summary>
        public void StartTimer()
        {
            if (timeRemaining > 0)
            {
                isRunning = true;
                UpdateUI();
            }
        }

        /// <summary>
        /// Toggle pause/continue.
        /// </summary>
        public void ToggleTimer()
        {
            if (timeRemaining > 0)
            {
                isRunning = !isRunning;
                UpdateUI();
            }
        }

        /// <summary>
        /// Reset timer.
        /// </summary>
        public void ResetTimer()
        {
            isRunning = false;
            timeRemaining = initialDuration;
            UpdateUI();
        }

        /// <summary>
        /// Complete timer immediately.
        /// </summary>
        public void CompleteTimer()
        {
            isRunning = false;
            timeRemaining = 0;
            UpdateUI();
            OnTimerCompleted();
        }

        /// <summary>
        /// Update timer logic.
        /// </summary>
        private void Update()
        {
            if (isRunning)
            {
                if (timeRemaining > 0)
                {
                    timeRemaining -= Time.deltaTime;
                    if (timeRemaining <= 0)
                    {
                        timeRemaining = 0;
                        isRunning = false;
                        OnTimerCompleted();
                    }
                    UpdateUI();
                }
            }
        }

        /// <summary>
        /// Update UI.
        /// </summary>
        private void UpdateUI()
        {
            // Update timer text
            if (timerText != null)
            {
                timerText.text = FormatTime(timeRemaining);
            }

            // Update progress bar
            if (progressBar != null)
            {
                float progress = timeRemaining / initialDuration;
                progressBar.fillAmount = progress;
            }

            // Update toggle button icon
            if (toggleButton)
            {
                Image buttonImage = toggleButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.sprite = isRunning ? pauseIcon : continueIcon;
                }
            }
        }

        /// <summary>
        /// Format seconds to time string.
        /// </summary>
        private string FormatTime(float timeInSeconds)
        {
            int hours = Mathf.FloorToInt(timeInSeconds / 3600);
            int minutes = Mathf.FloorToInt((timeInSeconds % 3600) / 60);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60);

            if (hours > 0)
                return string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
            else
                return string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        /// <summary>
        /// Handle timer complete.
        /// </summary>
        private void OnTimerCompleted()
        {
            TimerCompleted?.Invoke();

            // Play sound if available
            if (timerEndSound != null && audioSource != null)
            {
                audioSource.clip = timerEndSound;
                audioSource.Play();
            }

            // Flash timer text for visual feedback
            StartCoroutine(FlashOnComplete());
        }

        /// <summary>
        /// Flash effect on complete.
        /// </summary>
        private IEnumerator FlashOnComplete()
        {
            if (timerText == null) yield break;

            Color originalColor = timerText.color;

            for (int i = 0; i < 6; i++)
            {
                timerText.color = i % 2 == 0 ? Color.white : Color.red;
                yield return new WaitForSeconds(0.2f);
            }

            timerText.color = originalColor;
        }
    }
}