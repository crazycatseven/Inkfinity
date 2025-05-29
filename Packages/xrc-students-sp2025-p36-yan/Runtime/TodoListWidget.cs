using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Todo list widget for managing todo items
    /// </summary>
    public class TodoListWidget : SmartWidget
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private GameObject todoItemPrefab;
        [SerializeField] private Transform itemsContainer;

        [Header("Settings")]
        [SerializeField] private string defaultTitle = "To-do List";
        [SerializeField] private int maxItems = 10;
        [SerializeField] private float baseHeight = 130f;
        [SerializeField] private float itemHeight = 32f;

        private List<TodoItem> items = new List<TodoItem>();
        private string listTitle;
        private RectTransform rectTransform;

        /// <summary>
        /// Initialize components
        /// </summary>
        private void Awake()
        {
            listTitle = defaultTitle;
            rectTransform = GetComponent<RectTransform>();
            UpdateUI();
        }

        /// <summary>
        /// Initialize widget from recognized text
        /// </summary>
        public override void Initialize(string recognizedText)
        {
            // Format: TODO/ITEM1/ITEM2/ITEM3
            string[] parts = recognizedText.Split('/');
            if (parts.Length < 2) return;

            // Clear any existing items
            ClearItems();

            // Add items from the recognized text (skip the first "TODO" part)
            for (int i = 1; i < parts.Length; i++)
            {
                if (!string.IsNullOrEmpty(parts[i]))
                {
                    AddItem(parts[i].Trim());
                }
            }

            UpdateUI();
        }

        /// <summary>
        /// Add a new item to the list
        /// </summary>
        /// <param name="itemText">Text content of the item</param>
        /// <param name="isCompleted">Initial completion state</param>
        private void AddItem(string itemText, bool isCompleted = false)
        {
            if (string.IsNullOrEmpty(itemText) || items.Count >= maxItems)
                return;

            if (todoItemPrefab == null || itemsContainer == null)
            {
                Debug.LogError("Todo item prefab or container not assigned in TodoListWidget");
                return;
            }

            // Instantiate a new todo item
            GameObject itemObject = Instantiate(todoItemPrefab, itemsContainer);
            TodoItem todoItem = itemObject.GetComponent<TodoItem>();

            if (todoItem != null)
            {
                // Initialize the item
                todoItem.Initialize(itemText, isCompleted);

                // Register event handlers
                todoItem.CompletionChanged += OnItemCompletionChanged;
                todoItem.DeleteRequested += OnItemDeleteRequested;

                // Add to the list
                items.Add(todoItem);

                UpdateHeight();
            }
            else
            {
                Debug.LogError("Todo item prefab does not have TodoItem component");
                Destroy(itemObject);
            }
        }

        /// <summary>
        /// Handle item completion state change
        /// </summary>
        private void OnItemCompletionChanged(TodoItem item, bool isCompleted)
        {
            // PASS
        }

        /// <summary>
        /// Handle item delete request
        /// </summary>
        private void OnItemDeleteRequested(TodoItem item)
        {
            if (item != null)
            {
                // Unregister events
                item.CompletionChanged -= OnItemCompletionChanged;
                item.DeleteRequested -= OnItemDeleteRequested;

                // Remove from list
                items.Remove(item);

                // Destroy game object
                Destroy(item.gameObject);

                UpdateHeight();
            }
        }

        /// <summary>
        /// Clear all items from the list
        /// </summary>
        public void ClearItems()
        {
            foreach (TodoItem item in items)
            {
                if (item != null)
                {
                    // Unregister events
                    item.CompletionChanged -= OnItemCompletionChanged;
                    item.DeleteRequested -= OnItemDeleteRequested;

                    // Destroy game object
                    Destroy(item.gameObject);
                }
            }

            items.Clear();

            UpdateHeight();
        }

        /// <summary>
        /// Update UI elements
        /// </summary>
        private void UpdateUI()
        {
            if (titleText != null)
            {
                titleText.text = listTitle;

                if (titleText.transform.parent != null)
                {
                    titleText.transform.SetAsFirstSibling();
                }
            }

            UpdateHeight();
        }

        /// <summary>
        /// Update height based on item count
        /// </summary>
        private void UpdateHeight()
        {
            if (rectTransform != null)
            {
                // Calculate the required height
                // The base height can accommodate 2 items, and each additional item adds one _itemHeight
                int extraItems = Mathf.Max(0, items.Count - 2);
                float newHeight = baseHeight + (extraItems * itemHeight);

                // Apply the new height
                Vector2 sizeDelta = rectTransform.sizeDelta;
                sizeDelta.y = newHeight;
                rectTransform.sizeDelta = sizeDelta;
            }
        }
    }
}