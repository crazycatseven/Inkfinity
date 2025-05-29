using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Manages sticky note collection, following and display in gallery mode.
    /// </summary>
    public class StickyNoteCollector : MonoBehaviour
    {
        [Header("Stylus Reference")]
        [SerializeField] private Transform stylusTransform;
        [SerializeField] private MxInkHandler stylusHandler;
        [Header("Collection Settings")]
        [SerializeField] private float longPressTime = 2.0f;
        [SerializeField] private float collectRadius = 0.5f;
        [Header("Follow Settings")]
        [SerializeField] private float followSpeed = 10f;
        [SerializeField] private float followDistance = 0.1f;
        [SerializeField] private float noteSpacing = 0.005f;
        [SerializeField] private float rotationSpeed = 5f;
        [Header("Gallery Settings")]
        [SerializeField] private float releaseDistance = 0.75f;
        [SerializeField] private float maxRowWidth = 1.2f;
        [SerializeField] private float notePadding = 0.05f;
        [SerializeField] private float rowVerticalPadding = 0.05f;
        [SerializeField] private float transitionDuration = 0.5f;
        [SerializeField] private float noteDelay = 0.05f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Header("Wall Attachment Settings")]
        [SerializeField] private float wallDetectionDistance = 5.0f;
        [SerializeField] private LayerMask wallLayerMask = -1;
        [SerializeField] private LayerMask ignoreLayerMask = 0;
        [SerializeField] private bool attachToWallsEnabled = true;
        [SerializeField] private float wallAttachmentOffset = 0.01f;
        public enum CollectorState { Idle, Following }
        private CollectorState currentState = CollectorState.Idle;
        private List<StickyNote> collectedNotes = new List<StickyNote>();
        private Dictionary<StickyNote, Coroutine> activeTransitions = new Dictionary<StickyNote, Coroutine>();
        private float pressStartTime;
        private bool backButtonWasPressed = false;
        private Vector3 releaseForward;
        private Vector3 releaseRight;
        private Vector3 releaseUp = Vector3.up;
        private Vector3 wallHitPoint;
        private Vector3 wallNormal;
        private WallDataCollector wallDataCollector;
        private void Start()
        {
            if (stylusTransform == null)
            {
                Debug.LogWarning("Stylus transform not found. Searching for MxInkHandler...");
                FindStylusReferences();
            }
            SetupStylusEvents();
            wallDataCollector = FindFirstObjectByType<WallDataCollector>();
            if (wallDataCollector == null)
            {
                Debug.LogWarning("WallDataCollector not found in scene. Wall attachment might not work properly.");
            }
        }
        private void FindStylusReferences()
        {
            if (stylusHandler == null)
            {
                stylusHandler = FindFirstObjectByType<MxInkHandler>();
                if (stylusHandler != null)
                {
                    Debug.Log("Found MxInkHandler automatically");
                    stylusTransform = stylusHandler.TipTransform;
                }
            }
            else if (stylusTransform == null)
            {
                stylusTransform = stylusHandler.TipTransform;
            }
            if (stylusHandler == null)
            {
                Debug.LogError("Could not find MxInkHandler. Some functionality will be limited.");
            }
        }
        private void SetupStylusEvents()
        {
            if (stylusHandler != null)
            {
                stylusHandler.buttonEvents.OnSideButtonPressed += OnStylusBackButtonPressed;
                stylusHandler.buttonEvents.OnSideButtonReleased += OnStylusBackButtonReleased;
            }
        }
        private void OnDestroy()
        {
            if (stylusHandler != null)
            {
                stylusHandler.buttonEvents.OnSideButtonPressed -= OnStylusBackButtonPressed;
                stylusHandler.buttonEvents.OnSideButtonReleased -= OnStylusBackButtonReleased;
            }
        }
        private void Update()
        {
            if (stylusHandler != null && stylusHandler.IsSideButtonPressed)
            {
                if (!backButtonWasPressed)
                {
                    OnButtonDown();
                    backButtonWasPressed = true;
                }
                if (currentState == CollectorState.Idle)
                {
                    CheckLongPress();
                }
            }
            else if (backButtonWasPressed)
            {
                OnButtonUp();
                backButtonWasPressed = false;
            }
            if (currentState == CollectorState.Following)
            {
                UpdateNotesPosition();
            }
        }
        private void OnStylusBackButtonPressed() => OnButtonDown();
        private void OnStylusBackButtonReleased() => OnButtonUp();
        private void OnButtonDown() { pressStartTime = Time.time; }
        private void OnButtonUp()
        {
            if (currentState == CollectorState.Following)
            {
                Debug.Log("Button released, checking for walls...");
                bool wallDetected = DetectWallAhead();
                Debug.Log($"Wall detected: {wallDetected}, Attach to walls enabled: {attachToWallsEnabled}");
                if (attachToWallsEnabled && wallDetected)
                {
                    SaveWallOrientationAndPosition();
                    ArrangeGalleryOnWall();
                }
                else
                {
                    SaveReleaseOrientation();
                    ArrangeGallery();
                }
                currentState = CollectorState.Idle;
            }
        }
        private bool DetectWallAhead()
        {
            Vector3 rayOrigin = GetCollectorPosition();
            Vector3 rayDirection = GetCollectorRotation() * Vector3.forward;
            List<Collider> disabledColliders = new List<Collider>();
            try
            {
                foreach (var note in collectedNotes)
                {
                    if (note == null) continue;
                    Collider[] noteColliders = note.GetComponentsInChildren<Collider>(true);
                    foreach (var collider in noteColliders)
                    {
                        if (collider.enabled)
                        {
                            disabledColliders.Add(collider);
                            collider.enabled = false;
                        }
                    }
                }
                RaycastHit hit;
                if (Physics.Raycast(rayOrigin, rayDirection, out hit, wallDetectionDistance, wallLayerMask))
                {
                    wallHitPoint = hit.point;
                    wallNormal = hit.normal;
                    return true;
                }
                if (wallDataCollector != null && Meta.XR.MRUtilityKit.MRUK.Instance != null)
                {
                    MRUKRoom currentRoom = Meta.XR.MRUtilityKit.MRUK.Instance.GetCurrentRoom();
                    if (currentRoom != null)
                    {
                        if (currentRoom.WallAnchors == null || currentRoom.WallAnchors.Count == 0)
                        {
                            return false;
                        }
                        foreach (var wallAnchor in currentRoom.WallAnchors)
                        {
                            Vector3 toWall = wallAnchor.transform.position - rayOrigin;
                            float dot = Vector3.Dot(toWall.normalized, rayDirection.normalized);
                            if (dot > 0.7f)
                            {
                                Vector3 planeNormal = wallAnchor.transform.forward;
                                float denominator = Vector3.Dot(rayDirection, planeNormal);
                                if (Mathf.Abs(denominator) < 0.0001f)
                                {
                                    continue;
                                }
                                float distance = Vector3.Dot(wallAnchor.transform.position - rayOrigin, planeNormal) / denominator;
                                if (distance > 0 && distance < wallDetectionDistance)
                                {
                                    Vector3 hitPoint = rayOrigin + rayDirection * distance;
                                    Vector3 localHitPoint = wallAnchor.transform.InverseTransformPoint(hitPoint);
                                    if (wallAnchor.PlaneRect.HasValue)
                                    {
                                        Rect rect = wallAnchor.PlaneRect.Value;
                                        float tolerance = 0.1f;
                                        if (localHitPoint.x >= rect.xMin - tolerance && localHitPoint.x <= rect.xMax + tolerance &&
                                            localHitPoint.y >= rect.yMin - tolerance && localHitPoint.y <= rect.yMax + tolerance)
                                        {
                                            wallHitPoint = hitPoint;
                                            wallNormal = -planeNormal;
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return false;
            }
            finally
            {
                foreach (var collider in disabledColliders)
                {
                    if (collider != null)
                    {
                        collider.enabled = true;
                    }
                }
            }
        }
        private void SaveWallOrientationAndPosition()
        {
            releaseForward = -wallNormal;
            releaseUp = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(releaseForward, releaseUp)) > 0.95f)
            {
                releaseUp = Vector3.forward;
            }
            releaseRight = Vector3.Cross(releaseUp, releaseForward).normalized;
            releaseUp = Vector3.Cross(releaseForward, releaseRight).normalized;
        }
        private void SaveReleaseOrientation()
        {
            Quaternion rotation = GetCollectorRotation();
            releaseForward = rotation * Vector3.forward;
            releaseForward.y = 0;
            if (releaseForward.magnitude < 0.01f)
            {
                releaseForward = Vector3.forward;
            }
            releaseForward.Normalize();
            releaseRight = Vector3.Cross(Vector3.up, releaseForward).normalized;
            releaseUp = Vector3.up;
        }
        private void CheckLongPress()
        {
            if (Time.time - pressStartTime >= longPressTime)
            {
                CollectNearbyNotes();
                StartFollowMode();
            }
        }
        private void CollectNearbyNotes()
        {
            if (StickyNoteManager.Instance == null) return;
            Vector3 collectPosition = GetCollectorPosition();
            List<StickyNote> nearbyNotes = StickyNoteManager.Instance.GetNearbyNotes(collectPosition, collectRadius);
            if (nearbyNotes.Count == 0)
            {
                Debug.Log("No sticky notes found within radius");
                return;
            }
            collectedNotes.Clear();
            foreach (var note in nearbyNotes)
            {
                collectedNotes.Add(note);
            }
            if (stylusHandler != null)
            {
                stylusHandler.TriggerHapticClick();
            }
            Debug.Log($"Collected {collectedNotes.Count} sticky notes");
        }
        private void StartFollowMode()
        {
            if (collectedNotes.Count == 0) return;
            currentState = CollectorState.Following;
            if (stylusHandler != null)
            {
                stylusHandler.TriggerHapticPulse(0.7f, 0.1f);
            }
        }
        private void UpdateNotesPosition()
        {
            if (collectedNotes.Count == 0) return;
            Vector3 position = GetCollectorPosition();
            Quaternion rotation = GetCollectorRotation();
            Vector3 forward = rotation * Vector3.forward;
            Vector3 targetPosition = position + forward * followDistance;
            for (int i = 0; i < collectedNotes.Count; i++)
            {
                StickyNote note = collectedNotes[i];
                if (note == null) continue;
                Vector3 noteTargetPosition = targetPosition + forward * (i * noteSpacing);
                note.transform.position = Vector3.Lerp(note.transform.position, noteTargetPosition, Time.deltaTime * followSpeed);
                note.transform.rotation = Quaternion.Slerp(note.transform.rotation, rotation, Time.deltaTime * rotationSpeed);
            }
        }
        private void ArrangeGalleryOnWall()
        {
            if (collectedNotes.Count == 0) return;
            foreach (var pair in activeTransitions)
            {
                if (pair.Value != null)
                {
                    StopCoroutine(pair.Value);
                }
            }
            activeTransitions.Clear();
            if (stylusHandler != null)
            {
                stylusHandler.TriggerHapticPulse(0.5f, 0.2f);
            }
            Vector3 galleryCenter = wallHitPoint + wallNormal * wallAttachmentOffset;
            CalculateGalleryLayout(galleryCenter);
        }
        private void ArrangeGallery()
        {
            if (collectedNotes.Count == 0) return;
            foreach (var pair in activeTransitions)
            {
                if (pair.Value != null)
                {
                    StopCoroutine(pair.Value);
                }
            }
            activeTransitions.Clear();
            if (stylusHandler != null)
            {
                stylusHandler.TriggerHapticPulse(0.5f, 0.2f);
            }
            Vector3 stylusPosition = GetCollectorPosition();
            Vector3 galleryCenter = stylusPosition + releaseForward * releaseDistance;
            CalculateGalleryLayout(galleryCenter);
        }
        private void CalculateGalleryLayout(Vector3 galleryCenter)
        {
            if (collectedNotes.Count == 0) return;
            Dictionary<StickyNote, Vector2> noteSizes = new Dictionary<StickyNote, Vector2>();
            foreach (var note in collectedNotes)
            {
                if (note == null) continue;
                float width = Mathf.Max(note.Width, 0.1f);
                float height = Mathf.Max(note.Height, 0.1f);
                noteSizes[note] = new Vector2(width, height);
            }
            List<List<StickyNote>> rows = new List<List<StickyNote>>();
            List<StickyNote> currentRow = new List<StickyNote>();
            float currentRowWidth = 0f;
            float maxRowHeight = 0f;
            List<StickyNote> sortedNotes = new List<StickyNote>(collectedNotes);
            foreach (var note in sortedNotes)
            {
                if (note == null) continue;
                Vector2 noteSize = noteSizes[note];
                if (currentRow.Count > 0 && currentRowWidth + noteSize.x + notePadding > maxRowWidth)
                {
                    rows.Add(currentRow);
                    currentRow = new List<StickyNote>();
                    currentRowWidth = 0f;
                    maxRowHeight = 0f;
                }
                currentRow.Add(note);
                currentRowWidth += noteSize.x;
                if (currentRow.Count > 1)
                {
                    currentRowWidth += notePadding;
                }
                maxRowHeight = Mathf.Max(maxRowHeight, noteSize.y);
            }
            if (currentRow.Count > 0)
            {
                rows.Add(currentRow);
            }
            float yOffset = 0f;
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                List<StickyNote> row = rows[rowIndex];
                float rowWidth = CalculateRowWidth(row, noteSizes);
                float rowHeight = CalculateRowHeight(row, noteSizes);
                float xOffset = -rowWidth / 2f;
                for (int i = 0; i < row.Count; i++)
                {
                    StickyNote note = row[i];
                    Vector2 noteSize = noteSizes[note];
                    float noteCenterX = xOffset + noteSize.x / 2f;
                    Vector3 targetPosition = galleryCenter +
                                           releaseRight * noteCenterX +
                                           releaseUp * yOffset;
                    Quaternion targetRotation = Quaternion.LookRotation(releaseForward, releaseUp);
                    float delay = (rowIndex * row.Count + i) * noteDelay;
                    activeTransitions[note] = StartCoroutine(TransitionToGallery(note, targetPosition, targetRotation, delay));
                    xOffset += noteSize.x + notePadding;
                }
                yOffset -= (rowHeight + rowVerticalPadding);
            }
        }
        private float CalculateRowWidth(List<StickyNote> row, Dictionary<StickyNote, Vector2> noteSizes)
        {
            float width = 0f;
            foreach (var note in row)
            {
                width += noteSizes[note].x;
            }
            width += (row.Count - 1) * notePadding;
            return width;
        }
        private float CalculateRowHeight(List<StickyNote> row, Dictionary<StickyNote, Vector2> noteSizes)
        {
            float height = 0f;
            foreach (var note in row)
            {
                height = Mathf.Max(height, noteSizes[note].y);
            }
            return height;
        }
        private IEnumerator TransitionToGallery(StickyNote note, Vector3 targetPosition, Quaternion targetRotation, float delay)
        {
            if (note == null) yield break;
            yield return new WaitForSeconds(delay);
            Vector3 startPosition = note.transform.position;
            Quaternion startRotation = note.transform.rotation;
            float startTime = Time.time;
            while (Time.time < startTime + transitionDuration)
            {
                float t = (Time.time - startTime) / transitionDuration;
                float curvedT = transitionCurve.Evaluate(t);
                note.transform.position = Vector3.Lerp(startPosition, targetPosition, curvedT);
                note.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, curvedT);
                yield return null;
            }
            note.transform.position = targetPosition;
            note.transform.rotation = targetRotation;
            activeTransitions.Remove(note);
        }
        public void Reset()
        {
            foreach (var pair in activeTransitions)
            {
                if (pair.Value != null)
                {
                    StopCoroutine(pair.Value);
                }
            }
            activeTransitions.Clear();
            collectedNotes.Clear();
            currentState = CollectorState.Idle;
        }
        private Vector3 GetCollectorPosition()
        {
            if (stylusHandler != null)
            {
                return stylusHandler.TipPosition;
            }
            else if (stylusTransform != null)
            {
                return stylusTransform.position;
            }
            return transform.position;
        }
        private Quaternion GetCollectorRotation()
        {
            if (stylusTransform != null)
            {
                return stylusTransform.rotation;
            }
            return transform.rotation;
        }
    }
}