using System;
using System.Collections.Generic;
using UnityEngine;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Manages strokes.
    /// </summary>
    public class StrokeManager : MonoBehaviour
    {
        [Header("Stroke Settings")]
        /// <summary>
        /// Parent for strokes.
        /// </summary>
        [SerializeField, Tooltip("Parent transform for organizing strokes, if not assigned, will use the parent of the StrokeManager game object")]
        private Transform strokesContainer;
        /// <summary>
        /// All strokes.
        /// </summary>
        private List<Stroke> strokes = new List<Stroke>();
        /// <summary>
        /// Called when stroke added.
        /// </summary>
        public event Action<Stroke> StrokeAdded;
        /// <summary>
        /// Called when stroke removed.
        /// </summary>
        public event Action<Stroke> StrokeRemoved;
        /// <summary>
        /// Called when all strokes cleared.
        /// </summary>
        public event Action AllStrokesCleared;
        private static StrokeManager instance;
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static StrokeManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<StrokeManager>();
                    if (instance == null)
                    {
                        GameObject managerObject = new GameObject("StrokeManager");
                        instance = managerObject.AddComponent<StrokeManager>();
                    }
                }
                return instance;
            }
        }
        /// <summary>
        /// All strokes (read-only).
        /// </summary>
        public IReadOnlyList<Stroke> Strokes => strokes;
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
            if (strokesContainer == null)
            {
                strokesContainer = new GameObject("Strokes").transform;
                strokesContainer.SetParent(transform);
            }
        }
        /// <summary>
        /// Initialize events.
        /// </summary>
        private void Start()
        {
            StrokeAdded += (stroke) =>
            {
                TryCreateStickyNoteFromStroke(stroke);
            };
        }
        /// <summary>
        /// Create stroke.
        /// </summary>
        /// <param name="meshObject">Mesh object.</param>
        /// <param name="points">Points.</param>
        /// <param name="widths">Widths.</param>
        /// <returns>Stroke.</returns>
        public Stroke CreateStroke(GameObject meshObject, List<Vector3> points, List<float> widths)
        {
            if (meshObject == null)
            {
                Debug.LogError("Cannot create stroke with null mesh object");
                return null;
            }
            meshObject.transform.SetParent(strokesContainer);
            Stroke newStroke = new Stroke(meshObject, points, widths);
            strokes.Add(newStroke);
            StrokeAdded?.Invoke(newStroke);
            return newStroke;
        }
        /// <summary>
        /// Remove last stroke.
        /// </summary>
        /// <returns>True if removed.</returns>
        public bool RemoveLastStroke()
        {
            if (strokes.Count == 0)
                return false;
            Stroke lastStroke = strokes[strokes.Count - 1];
            strokes.RemoveAt(strokes.Count - 1);
            StrokeRemoved?.Invoke(lastStroke);
            Destroy(lastStroke.MeshObject);
            return true;
        }
        /// <summary>
        /// Remove stroke.
        /// </summary>
        /// <param name="stroke">Stroke.</param>
        /// <returns>True if removed.</returns>
        public bool RemoveStroke(Stroke stroke)
        {
            if (stroke == null || !strokes.Contains(stroke))
                return false;
            strokes.Remove(stroke);
            StrokeRemoved?.Invoke(stroke);
            Destroy(stroke.MeshObject);
            return true;
        }
        /// <summary>
        /// Clear all strokes.
        /// </summary>
        public void ClearAllStrokes()
        {
            foreach (var stroke in strokes)
            {
                Destroy(stroke.MeshObject);
            }
            strokes.Clear();
            AllStrokesCleared?.Invoke();
        }
        /// <summary>
        /// Get last stroke.
        /// </summary>
        /// <returns>Last stroke or null.</returns>
        public Stroke GetLastStroke()
        {
            if (strokes.Count == 0)
                return null;
            return strokes[strokes.Count - 1];
        }
        /// <summary>
        /// Get current stroke index.
        /// </summary>
        /// <returns>Current stroke index.</returns>
        public int GetCurrentStrokeIndex()
        {
            return strokes.Count - 1;
        }
        /// <summary>
        /// Get stroke at specified index.
        /// </summary>
        /// <param name="index">Index.</param>
        /// <returns>Stroke or null.</returns>
        public Stroke GetStrokeAtIndex(int index)
        {
            if (index < 0 || index >= strokes.Count)
                return null;
            return strokes[index];
        }
        /// <summary>
        /// Get all strokes after specified index.
        /// </summary>
        /// <param name="index">Start index.</param>
        /// <returns>List of strokes.</returns>
        public List<Stroke> GetStrokesAfterIndex(int index)
        {
            if (index < 0 || index >= strokes.Count)
                return new List<Stroke>();

            List<Stroke> result = new List<Stroke>();
            for (int i = index + 1; i < strokes.Count; i++)
            {
                result.Add(strokes[i]);
            }
            return result;
        }
        /// <summary>
        /// Remove all strokes from specified index.
        /// </summary>
        /// <param name="startIndex">Start index.</param>
        /// <returns>True if removed.</returns>
        public bool RemoveStrokesFromIndex(int startIndex)
        {
            if (startIndex < 0 || startIndex >= strokes.Count)
                return false;

            List<Stroke> strokesToRemove = new List<Stroke>();
            for (int i = strokes.Count - 1; i >= startIndex; i--)
            {
                strokesToRemove.Add(strokes[i]);
            }

            foreach (var stroke in strokesToRemove)
            {
                RemoveStroke(stroke);
            }

            return true;
        }
        /// <summary>
        /// Find strokes in area.
        /// </summary>
        /// <param name="corners">Area corners.</param>
        /// <param name="strict">Strict mode.</param>
        /// <returns>Strokes in area or null.</returns>
        public List<Stroke> FindStrokesInArea(Vector3[] corners, bool strict = false)
        {
            if (corners == null || corners.Length != 4 || strokes == null || strokes.Count == 0)
                return null;
            float rectHeight = 0;
            foreach (var corner in corners)
            {
                rectHeight += corner.y;
            }
            rectHeight /= 4f;
            float yThreshold = 0.1f;
            List<Stroke> strokesInArea = new List<Stroke>();
            foreach (Stroke stroke in strokes)
            {
                if (stroke.Points.Count < 3)
                    continue;
                float distanceY = Mathf.Abs(stroke.Center.y - rectHeight);
                if (distanceY > yThreshold)
                {
                    continue;
                }
                bool isInside = strict ?
                    IsStrokeInStickyNoteXZ(stroke, corners) :
                    IsStrokeOverlappingStickyNote(stroke, corners);
                if (isInside)
                {
                    strokesInArea.Add(stroke);
                }
            }
            return strokesInArea.Count > 0 ? strokesInArea : null;
        }
        /// <summary>
        /// Check if stroke overlaps polygon.
        /// </summary>
        private bool IsStrokeOverlappingStickyNote(Stroke stroke, Vector3[] corners)
        {
            Vector3[] boundingBoxCorners = new Vector3[]
            {
                new Vector3(stroke.MinX, 0, stroke.MinZ),
                new Vector3(stroke.MaxX, 0, stroke.MinZ),
                new Vector3(stroke.MinX, 0, stroke.MaxZ),
                new Vector3(stroke.MaxX, 0, stroke.MaxZ)
            };
            foreach (Vector3 corner in boundingBoxCorners)
            {
                if (IsPointInPolygonXZ(corner, corners))
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Check if stroke is inside polygon.
        /// </summary>
        private bool IsStrokeInStickyNoteXZ(Stroke stroke, Vector3[] corners)
        {
            Vector3[] boundingBoxCorners = new Vector3[]
            {
                new Vector3(stroke.MinX, 0, stroke.MinZ),
                new Vector3(stroke.MaxX, 0, stroke.MinZ),
                new Vector3(stroke.MinX, 0, stroke.MaxZ),
                new Vector3(stroke.MaxX, 0, stroke.MaxZ)
            };
            foreach (Vector3 corner in boundingBoxCorners)
            {
                if (!IsPointInPolygonXZ(corner, corners))
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// Check if point is in polygon (XZ).
        /// </summary>
        private bool IsPointInPolygonXZ(Vector3 point, Vector3[] polygonVertices)
        {
            if (polygonVertices.Length < 3)
                return false;
            int intersections = 0;
            for (int i = 0; i < polygonVertices.Length; i++)
            {
                Vector3 vert1 = polygonVertices[i];
                Vector3 vert2 = polygonVertices[(i + 1) % polygonVertices.Length];
                if (((vert1.z <= point.z && point.z < vert2.z) ||
                     (vert2.z <= point.z && point.z < vert1.z)) &&
                    (point.x < (vert2.x - vert1.x) * (point.z - vert1.z) / (vert2.z - vert1.z) + vert1.x))
                {
                    intersections++;
                }
            }
            return (intersections % 2) == 1;
        }
        /// <summary>
        /// Try create sticky note from stroke.
        /// </summary>
        /// <param name="stroke">Stroke.</param>
        /// <returns>Sticky note or null.</returns>
        public StickyNote TryCreateStickyNoteFromStroke(Stroke stroke)
        {
            RectangleDetectionResult rectangleDetectionResult = RectangleDetection.DetectRectangle(stroke.Points);
            if (rectangleDetectionResult == null || rectangleDetectionResult.FinalConfidence < 0.75f)
            {
                return null;
            }
            List<Stroke> strokesInArea = FindStrokesInArea(rectangleDetectionResult.Corners);
            if (strokesInArea == null || strokesInArea.Count == 0)
            {
                return null;
            }
            StickyNoteManager stickyNoteManager = StickyNoteManager.Instance;
            if (stickyNoteManager == null)
            {
                Debug.LogError("Cannot create StickyNote: StickyNoteManager not found");
                return null;
            }
            StickyNote stickyNote = stickyNoteManager.CreateStickyNote(rectangleDetectionResult.Corners, strokesInArea);
            if (stickyNote != null)
            {
                RemoveStroke(stroke);
                foreach (var s in strokesInArea)
                {
                    strokes.Remove(s);
                }
            }
            return stickyNote;
        }
    }
}