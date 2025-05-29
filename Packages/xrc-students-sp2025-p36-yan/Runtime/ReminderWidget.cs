using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Reminder widget for event notifications
    /// </summary>
    public class ReminderWidget : SmartWidget
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI dateText;
        [SerializeField] private TextMeshProUGUI timeLeftText;
        [SerializeField] private Image dateBackground;
        [SerializeField] private Image progressBarFill;

        [Header("Settings")]
        [SerializeField] private float todayBackgroundWidth = 200f;
        [SerializeField] private float tomorrowBackgroundWidth = 250f;
        [SerializeField] private string defaultTitle = "Reminder";

        private DateTime eventTime;
        private DateTime reminderStartTime;
        private string eventTitle;
        private string dateType; // "Today" or "Tomorrow"
        private bool isActive = true;

        /// <summary>
        /// Initialize components
        /// </summary>
        private void Awake()
        {
            // Set default values
            eventTitle = defaultTitle;
            dateType = "Today";
            reminderStartTime = DateTime.Now;
            eventTime = DateTime.Now.AddHours(1);

            // Initialize UI
            UpdateUI();
        }

        /// <summary>
        /// Update logic
        /// </summary>
        private void Update()
        {
            if (isActive)
            {
                UpdateTimeLeft();
            }
        }

        /// <summary>
        /// Initialize widget from recognized text
        /// </summary>
        public override void Initialize(string recognizedText)
        {
            // Format: REMINDER/EVENT_TITLE|DATE|HH:MM
            string[] parts = recognizedText.Split('/');
            if (parts.Length < 2) return;

            string data = parts[1];
            string[] dataParts = data.Split('|');

            // Parse event title
            if (dataParts.Length > 0 && !string.IsNullOrEmpty(dataParts[0]))
            {
                eventTitle = dataParts[0].Trim();
            }

            // Parse date (Today/Tomorrow)
            if (dataParts.Length > 1)
            {
                string dateStr = dataParts[1].Trim();
                if (dateStr.Equals("Tomorrow", StringComparison.OrdinalIgnoreCase))
                {
                    dateType = "Tomorrow";
                }
                else
                {
                    dateType = "Today";
                }
            }

            // Parse time
            if (dataParts.Length > 2)
            {
                string timeStr = dataParts[2].Trim();
                DateTime currentDate = DateTime.Today;

                // If tomorrow, add one day
                if (dateType.Equals("Tomorrow", StringComparison.OrdinalIgnoreCase))
                {
                    currentDate = currentDate.AddDays(1);
                }

                try
                {
                    // Parse time in format HH:MM
                    string[] timeParts = timeStr.Split(':');
                    int hours = int.Parse(timeParts[0]);
                    int minutes = 0;

                    if (timeParts.Length > 1)
                    {
                        minutes = int.Parse(timeParts[1]);
                    }

                    // Set event time
                    eventTime = new DateTime(
                        currentDate.Year,
                        currentDate.Month,
                        currentDate.Day,
                        hours,
                        minutes,
                        0
                    );

                    // Set reminder start time to current time
                    reminderStartTime = DateTime.Now;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing time: {e.Message}");
                    // Set a default time 1 hour from now
                    eventTime = DateTime.Now.AddHours(1);
                }
            }

            UpdateUI();
        }

        /// <summary>
        /// Update UI elements
        /// </summary>
        private void UpdateUI()
        {
            if (titleText != null)
            {
                titleText.text = eventTitle;
            }

            if (timeText != null)
            {
                timeText.text = eventTime.ToString("HH:mm");
            }

            if (dateText != null)
            {
                dateText.text = dateType.ToUpper();
            }

            if (dateBackground != null)
            {
                RectTransform rectTransform = dateBackground.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    float width = dateType.Equals("Today", StringComparison.OrdinalIgnoreCase)
                        ? todayBackgroundWidth
                        : tomorrowBackgroundWidth;

                    Vector2 sizeDelta = rectTransform.sizeDelta;
                    sizeDelta.x = width;
                    rectTransform.sizeDelta = sizeDelta;
                }
            }

            UpdateTimeLeft();
        }

        /// <summary>
        /// Update time left and progress bar
        /// </summary>
        private void UpdateTimeLeft()
        {
            if (progressBarFill == null || timeLeftText == null)
                return;

            TimeSpan timeLeft = eventTime - DateTime.Now;

            if (timeLeft.TotalSeconds <= 0)
            {
                progressBarFill.fillAmount = 0;
                timeLeftText.text = "Time's up!";
                isActive = false;
                return;
            }

            TimeSpan totalDuration = eventTime - reminderStartTime;

            float progress = (float)(timeLeft.TotalSeconds / totalDuration.TotalSeconds);
            progress = Mathf.Clamp01(progress);

            progressBarFill.fillAmount = progress;

            string timeLeftStr = "";
            if (timeLeft.TotalHours >= 1)
            {
                int hours = (int)timeLeft.TotalHours;
                int minutes = timeLeft.Minutes;
                timeLeftStr = $"{hours}h {minutes} mins Left";
            }
            else if (timeLeft.TotalMinutes >= 1)
            {
                int minutes = (int)timeLeft.TotalMinutes;
                timeLeftStr = $"{minutes} mins Left";
            }
            else
            {
                int seconds = (int)timeLeft.TotalSeconds;
                timeLeftStr = $"{seconds} secs Left";
            }

            timeLeftText.text = timeLeftStr;
        }
    }
}