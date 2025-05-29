using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Single todo item.
    /// </summary>
    public class TodoItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI itemText;
        [SerializeField] private Toggle completionToggle;
        /// <summary>
        /// Completion state changed event.
        /// </summary>
        public event Action<TodoItem, bool> CompletionChanged;
        /// <summary>
        /// Delete request event.
        /// </summary>
        public event Action<TodoItem> DeleteRequested;
        /// <summary>
        /// Is completed.
        /// </summary>
        public bool IsCompleted => completionToggle ? completionToggle.isOn : false;
        /// <summary>
        /// Item text.
        /// </summary>
        public string Text => itemText ? itemText.text : string.Empty;
        private void Awake()
        {
            if (completionToggle)
            {
                completionToggle.onValueChanged.AddListener(OnToggleValueChanged);
            }
        }
        /// <summary>
        /// Initialize item.
        /// </summary>
        /// <param name="text">Text.</param>
        /// <param name="isCompleted">Initial state.</param>
        public void Initialize(string text, bool isCompleted = false)
        {
            if (itemText)
            {
                itemText.text = text;
            }
            if (completionToggle)
            {
                completionToggle.isOn = isCompleted;
                UpdateCompletionVisuals(isCompleted);
            }
        }
        /// <summary>
        /// Handle toggle change.
        /// </summary>
        private void OnToggleValueChanged(bool isOn)
        {
            UpdateCompletionVisuals(isOn);
            CompletionChanged?.Invoke(this, isOn);
        }
        /// <summary>
        /// Update visuals for completion.
        /// </summary>
        private void UpdateCompletionVisuals(bool isCompleted)
        {
            if (itemText)
            {
                itemText.fontStyle = isCompleted ? FontStyles.Strikethrough : FontStyles.Normal;
                itemText.color = isCompleted
                    ? new Color(itemText.color.r, itemText.color.g, itemText.color.b, 0.5f)
                    : new Color(itemText.color.r, itemText.color.g, itemText.color.b, 1f);
            }
        }
        /// <summary>
        /// Handle delete click.
        /// </summary>
        private void OnDeleteClicked()
        {
            DeleteRequested?.Invoke(this);
        }
    }
}