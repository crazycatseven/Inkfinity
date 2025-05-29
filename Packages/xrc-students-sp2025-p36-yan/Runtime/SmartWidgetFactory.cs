using System.Text.RegularExpressions;
using UnityEngine;
using System;
using System.Text;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Factory for creating smart widgets based on recognized text
    /// </summary>
    public class SmartWidgetFactory : MonoBehaviour
    {
        private static SmartWidgetFactory instance;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static SmartWidgetFactory Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<SmartWidgetFactory>();
                    if (instance == null)
                    {
                        GameObject factoryObject = new GameObject("SmartWidgetFactory");
                        instance = factoryObject.AddComponent<SmartWidgetFactory>();
                        DontDestroyOnLoad(factoryObject);
                    }
                }
                return instance;
            }
        }

        [Header("Widget Prefabs")]
        [SerializeField, Tooltip("Timer widget prefab")]
        private GameObject timerPrefab;

        [SerializeField, Tooltip("Todo list widget prefab")]
        private GameObject todoListPrefab;

        [SerializeField, Tooltip("Reminder widget prefab")]
        private GameObject reminderPrefab;

        [SerializeField, Tooltip("Microphone widget prefab")]
        private GameObject microphoneWidgetPrefab;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Create appropriate widget based on recognized text
        /// </summary>
        /// <param name="recognizedText">Text from OCR recognition</param>
        /// <returns>Widget GameObject or null if no match</returns>
        public GameObject CreateFromText(string recognizedText)
        {
            if (string.IsNullOrEmpty(recognizedText))
                return null;

            // Check if the text starts with one of our format prefixes
            if (recognizedText.StartsWith("TIMER/"))
            {
                return CreateTimerWidget(recognizedText);
            }
            else if (recognizedText.StartsWith("TODO/"))
            {
                return CreateTodoListWidget(recognizedText);
            }
            else if (recognizedText.StartsWith("REMINDER/"))
            {
                return CreateReminderWidget(recognizedText);
            }
            else
            {
                // Plain text without a format prefix - no widget needed
                Debug.Log($"Recognized text does not match any widget pattern: {recognizedText}");
                return null;
            }
        }

        /// <summary>
        /// Get the prompt for OCR and widget recognition
        /// </summary>
        /// <returns>Prompt string to send to LLM</returns>
        public string GetRecognitionPrompt()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("You are an OCR assistant. Recognize the text within the image.\n");
            sb.Append("Your task is to classify the recognized text and then output it according to the following rules:\n\n");

            sb.Append("If the text matches one of the patterns below (List, Timer, Reminder), format your output *strictly* as described for that pattern. Do not include any other explanatory text or deviate from the specified format.\n\n");

            sb.Append("1. If the image content appears to be a list (e.g., to-do items, shopping list, numbered or bulleted items like 1. xxx, 2. xxx, - xxx):\n");
            sb.Append("   Return in the format: TODO/ITEM1/ITEM2/ITEM3\n");
            sb.Append("   - `TODO` signifies that the content is a list.\n");
            sb.Append("   - `ITEM1`, `ITEM2`, `ITEM3`, etc., are the individual list items.\n");
            sb.Append("   - Items are separated by a forward slash (`/`).\n");
            sb.Append("   Example: If the image shows \"1. Buy milk 2. Call John\", return: TODO/Buy milk/Call John\n\n");

            sb.Append("2. If the image content appears to be a timer or countdown, typically containing time durations (e.g., 10:00, 05:30, 5 mins, 2 hours 30 minutes):\n");
            sb.Append("   Return in the format: TIMER/DESCRIPTION|HH:MM:SS\n");
            sb.Append("   - `TIMER` signifies a timer or countdown.\n");
            sb.Append("   - `DESCRIPTION` is the textual description or title associated with the timer (if any).\n");
            sb.Append("   - `HH:MM:SS` is the time in hours, minutes, and seconds. If hours or minutes are not explicitly present, represent them as `00`.\n");
            sb.Append("   Example: If the image shows 'Read 25:00', return: TIMER/Read|00:25:00\n");
            sb.Append("   Example: If the image shows 'Break 5 min', return: TIMER/Break|00:05:00\n\n");

            sb.Append("3. If the image content appears to be a schedule, appointment, or reminder, containing a specific time of day (e.g., 15:00, 3 PM, Tomorrow 10:00 AM):\n");
            sb.Append("   Return in the format: REMINDER/EVENT_TITLE|DATE|HH:MM\n");
            sb.Append("   - `REMINDER` signifies a scheduled event or reminder.\n");
            sb.Append("   - `EVENT_TITLE` is the title or description of the event.\n");
            sb.Append("   - `DATE` must be either 'Today' or 'Tomorrow'. If the date is ambiguous, not specified, or refers to the current day, default to 'Today'.\n");
            sb.Append("   - `HH:MM` is the time of the event in 24-hour format.\n");
            sb.Append("   Example: Image content 'Meeting at 3pm' -> REMINDER/Meeting|Today|15:00\n");
            sb.Append("   Example: Image content 'Customer Reception Tomorrow 10:00' -> REMINDER/Customer Reception|Tomorrow|10:00\n");
            sb.Append("   Example: Image content 'Gym session 6:30 AM' -> REMINDER/Gym session|Today|06:30\n\n");

            sb.Append("4. If the recognized text does *not* clearly match any of the specific patterns above (List, Timer, or Reminder):\n");
            sb.Append("   Return *only the raw recognized text* itself, without any prefix or special formatting.\n");
            sb.Append("   Example: If the image shows 'Hello World', return: Hello World\n");
            sb.Append("   Example: If the image shows 'Important Company Memo', return: Important Company Memo\n\n");

            sb.Append("To summarize: If a pattern (List, Timer, Reminder) is matched, output *only* the string in the specified format for that pattern. If no pattern is matched, output *only* the raw recognized text.");
            return sb.ToString();
        }

        /// <summary>
        /// Create timer widget
        /// </summary>
        private GameObject CreateTimerWidget(string recognizedText)
        {
            if (timerPrefab == null)
            {
                Debug.LogError("Timer prefab not assigned in SmartWidgetFactory");
                return null;
            }

            GameObject timerObject = Instantiate(timerPrefab);
            TimerWidget timerWidget = timerObject.GetComponent<TimerWidget>();

            if (timerWidget != null)
            {
                timerWidget.Initialize(recognizedText);
            }
            else
            {
                Debug.LogError("Timer prefab does not have TimerWidget component");
                Destroy(timerObject);
                return null;
            }

            return timerObject;
        }

        /// <summary>
        /// Create todo list widget
        /// </summary>
        private GameObject CreateTodoListWidget(string recognizedText)
        {
            if (todoListPrefab == null)
            {
                Debug.LogError("Todo list prefab not assigned in SmartWidgetFactory");
                return null;
            }

            // Instantiate the todo list prefab
            GameObject todoListObject = Instantiate(todoListPrefab);
            TodoListWidget todoListWidget = todoListObject.GetComponent<TodoListWidget>();

            if (todoListWidget != null)
            {
                todoListWidget.Initialize(recognizedText);
            }
            else
            {
                Debug.LogError("Todo list prefab does not have TodoListWidget component");
                Destroy(todoListObject);
                return null;
            }

            return todoListObject;
        }

        /// <summary>
        /// Create reminder widget
        /// </summary>
        private GameObject CreateReminderWidget(string recognizedText)
        {
            if (reminderPrefab == null)
            {
                Debug.LogError("Reminder prefab not assigned in SmartWidgetFactory");
                return null;
            }

            // Instantiate the reminder prefab
            GameObject reminderObject = Instantiate(reminderPrefab);
            ReminderWidget reminderWidget = reminderObject.GetComponent<ReminderWidget>();

            if (reminderWidget != null)
            {
                reminderWidget.Initialize(recognizedText);
            }
            else
            {
                Debug.LogError("Reminder prefab does not have ReminderWidget component");
                Destroy(reminderObject);
                return null;
            }

            return reminderObject;
        }

        /// <summary>
        /// Create microphone widget
        /// </summary>
        public GameObject CreateMicrophoneWidget()
        {
            return Instantiate(microphoneWidgetPrefab);
        }
    }
}