using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Manages sticky note creation and operations.
    /// </summary>
    public class StickyNoteManager : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Prefab Settings")]
        /// <summary>
        /// Sticky note prefab.
        /// </summary>
        [SerializeField, Tooltip("Sticky note prefab")]
        private GameObject stickyNotePrefab;

        [Header("Appearance Settings")]
        /// <summary>
        /// Default sticky note color.
        /// </summary>
        [SerializeField, Tooltip("Default color for sticky notes")]
        private Color defaultColor = new Color(1f, 0.92f, 0.016f, 1f);

        [Header("Text Note Settings")]
        /// <summary>
        /// Text note width.
        /// </summary>
        [SerializeField, Tooltip("Width for text sticky notes (in world units)")]
        private float textNoteWidth = 0.15f;
        /// <summary>
        /// Text note height.
        /// </summary>
        [SerializeField, Tooltip("Height for text sticky notes (in world units)")]
        private float textNoteHeight = 0.1f;
        #endregion

        #region Private Fields
        /// <summary>
        /// Sticky note container.
        /// </summary>
        private Transform notesContainer;
        /// <summary>
        /// All created sticky notes.
        /// </summary>
        private readonly List<StickyNote> stickyNotes = new List<StickyNote>();
        #endregion

        #region Events
        /// <summary>
        /// Called when a sticky note is created.
        /// </summary>
        public event Action<StickyNote> StickyNoteCreated;
        /// <summary>
        /// Called when a sticky note is deleted.
        /// </summary>
        public event Action<StickyNote> StickyNoteDeleted;
        #endregion

        #region Singleton
        private static StickyNoteManager instance;
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static StickyNoteManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<StickyNoteManager>();
                    if (instance == null)
                    {
                        GameObject managerObject = new GameObject("StickyNoteManager");
                        instance = managerObject.AddComponent<StickyNoteManager>();
                    }
                }
                return instance;
            }
        }
        #endregion

        /// <summary>
        /// All sticky notes (read-only).
        /// </summary>
        public IReadOnlyList<StickyNote> StickyNotes => stickyNotes;

        /// <summary>
        /// Initialize manager.
        /// </summary>
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            notesContainer = new GameObject("StickyNotes").transform;
            notesContainer.SetParent(transform);
            if (stickyNotePrefab == null)
            {
                Debug.LogWarning("StickyNote prefab is not assigned to StickyNoteManager. StickyNotes cannot be created.");
            }
        }

        /// <summary>
        /// Create sticky note from corners.
        /// </summary>
        /// <param name="corners">Four corners.</param>
        /// <param name="attachedStrokes">Attached strokes.</param>
        /// <returns>Sticky note.</returns>
        public StickyNote CreateStickyNote(Vector3[] corners, List<Stroke> attachedStrokes)
        {
            if (stickyNotePrefab == null)
            {
                Debug.LogError("Cannot create StickyNote: prefab is not assigned");
                return null;
            }
            if (corners == null || corners.Length != 4)
            {
                Debug.LogError("Cannot create StickyNote: need exactly 4 corner points");
                return null;
            }
            Vector3 center = Vector3.zero;
            foreach (var corner in corners)
            {
                center += corner;
            }
            center /= 4f;
            GameObject stickyNoteObj = Instantiate(stickyNotePrefab, center, Quaternion.identity);
            stickyNoteObj.name = $"StickyNote_{stickyNotes.Count}";
            stickyNoteObj.transform.SetParent(notesContainer);
            StickyNote stickyNote = stickyNoteObj.GetComponent<StickyNote>() ?? stickyNoteObj.AddComponent<StickyNote>();
            if (!stickyNote.Initialize(corners, defaultColor, false))
            {
                Debug.LogError("Failed to initialize StickyNote");
                Destroy(stickyNoteObj);
                return null;
            }
            stickyNote.AddStrokes(attachedStrokes);
            stickyNotes.Add(stickyNote);
            StickyNoteCreated?.Invoke(stickyNote);
            Debug.Log($"Created StickyNote with size: {stickyNote.Width}x{stickyNote.Height}");
            return stickyNote;
        }

        /// <summary>
        /// Create text sticky note.
        /// </summary>
        /// <param name="text">Text.</param>
        /// <param name="position">Position.</param>
        /// <param name="color">Color.</param>
        /// <returns>Sticky note.</returns>
        public StickyNote CreateStickyNoteWithText(string text, Vector3 position, Color? color = null)
        {
            if (stickyNotePrefab == null)
            {
                Debug.LogError("Cannot create StickyNote: prefab is not assigned");
                return null;
            }
            Color noteColor = color ?? defaultColor;
            float width = textNoteWidth;
            float height = textNoteHeight;
            Vector3[] corners = new Vector3[4];
            corners[0] = position + new Vector3(-width / 2, 0, -height / 2);
            corners[1] = position + new Vector3(width / 2, 0, -height / 2);
            corners[2] = position + new Vector3(width / 2, 0, height / 2);
            corners[3] = position + new Vector3(-width / 2, 0, height / 2);
            GameObject stickyNoteObj = Instantiate(stickyNotePrefab, position, Quaternion.identity);
            stickyNoteObj.name = $"TextNote_{stickyNotes.Count}";
            stickyNoteObj.transform.SetParent(notesContainer);
            StickyNote stickyNote = stickyNoteObj.GetComponent<StickyNote>() ?? stickyNoteObj.AddComponent<StickyNote>();
            if (!stickyNote.Initialize(corners, noteColor))
            {
                Debug.LogError("Failed to initialize StickyNote");
                Destroy(stickyNoteObj);
                return null;
            }

            if (text != "")
            {
                stickyNote.SetText(text);
            }
            stickyNotes.Add(stickyNote);
            StickyNoteCreated?.Invoke(stickyNote);
            return stickyNote;
        }

        /// <summary>
        /// Delete sticky note.
        /// </summary>
        /// <param name="stickyNote">Sticky note.</param>
        /// <returns>True if deleted.</returns>
        public bool DeleteStickyNote(StickyNote stickyNote)
        {
            if (stickyNote == null || !stickyNotes.Contains(stickyNote))
                return false;
            stickyNotes.Remove(stickyNote);
            StickyNoteDeleted?.Invoke(stickyNote);
            Destroy(stickyNote.gameObject);
            return true;
        }

        /// <summary>
        /// Delete all sticky notes.
        /// </summary>
        public void DeleteAllStickyNotes()
        {
            foreach (var note in stickyNotes)
            {
                if (note != null)
                {
                    Destroy(note.gameObject);
                }
            }
            stickyNotes.Clear();
        }

        /// <summary>
        /// Change sticky note color.
        /// </summary>
        /// <param name="stickyNote">Sticky note.</param>
        /// <param name="color">Color.</param>
        public void ChangeStickyNoteColor(StickyNote stickyNote, Color color)
        {
            if (stickyNote == null || !stickyNotes.Contains(stickyNote))
                return;
            stickyNote.ChangeColor(color);
        }

        /// <summary>
        /// Get sticky notes in range.
        /// </summary>
        /// <param name="position">Center.</param>
        /// <param name="radius">Radius.</param>
        /// <returns>Sticky notes in range.</returns>
        public List<StickyNote> GetNearbyNotes(Vector3 position, float radius)
        {
            List<StickyNote> nearbyNotes = new List<StickyNote>();
            float sqrRadius = radius * radius;
            foreach (var note in stickyNotes)
            {
                if (note == null) continue;
                float sqrDistance = (note.transform.position - position).sqrMagnitude;
                if (sqrDistance <= sqrRadius)
                {
                    nearbyNotes.Add(note);
                }
            }
            return nearbyNotes;
        }
    }
}
