using System;
using System.Collections.Generic;
using UnityEngine;
using XRC.Students.Sp2025.P36.Yan;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Component for creating line drawings
    /// </summary>
    public class LineDrawing : MonoBehaviour
    {
        [Header("Input Device")]
        /// <summary>
        /// The stylus input device used for drawing
        /// </summary>
        [SerializeField, Tooltip("The stylus input device used for drawing")]
        private StylusHandler stylus;

        [Header("Line Appearance")]
        /// <summary>
        /// The maximum width of the line
        /// </summary>
        [SerializeField, Tooltip("The maximum width of the line")]
        private float lineWidth = 0.01f;

        /// <summary>
        /// The minimum width of the line
        /// </summary>
        [SerializeField, Tooltip("The minimum width of the line")]
        private float minLineWidth = 0.0005f;

        /// <summary>
        /// Material to use for the line
        /// </summary>
        [SerializeField, Tooltip("Material to use for the line")]
        private Material lineMaterial;

        [Header("Interaction Settings")]
        /// <summary>
        /// Minimum distance between adjacent line points
        /// </summary>
        [SerializeField, Tooltip("Minimum distance between adjacent line points")]
        private float minDistanceBetweenPoints = 0.0005f;

        /// <summary>
        /// Maximum segment length for line smoothing
        /// </summary>
        [SerializeField, Tooltip("Maximum segment length for line smoothing")]
        private float maxSegmentLength = 0.005f;

        [Header("Haptic Feedback")]
        /// <summary>
        /// Damping factor for haptic feedback
        /// </summary>
        [SerializeField, Range(0.1f, 1.0f), Tooltip("Damping factor for haptic feedback")]
        private float hapticDampingFactor = 0.6f;

        /// <summary>
        /// Duration of haptic feedback
        /// </summary>
        [SerializeField, Range(0.001f, 0.1f), Tooltip("Duration of haptic feedback")]
        private float hapticDuration = 0.01f;

        private StrokeManager strokeManager;

        // Private variables for drawing
        private GameObject currentMeshObj;
        private List<Vector3> currentLinePoints = new List<Vector3>();
        private List<float> currentLineWidths = new List<float>();
        private bool isDrawing = false;
        private bool doubleTapDetected = false;
        private Vector3 previousPoint;

        /// <summary>
        /// Event triggered when drawing starts
        /// </summary>
        public event Action OnDrawingStarted;

        /// <summary>
        /// Event triggered when drawing ends
        /// </summary>
        public event Action<GameObject, List<Vector3>, List<float>> OnDrawingEnded;

        /// <summary>
        /// Sets the stylus input device
        /// </summary>
        public StylusHandler Stylus
        {
            get { return stylus; }
            set { stylus = value; }
        }

        /// <summary>
        /// Awake is called when the script instance is being loaded
        /// </summary>
        private void Awake()
        {
            // Check if StrokeManager exists
            strokeManager = StrokeManager.Instance;
            if (strokeManager == null)
            {
                Debug.LogWarning("StrokeManager not found, creating one");
                strokeManager = new GameObject("StrokeManager").AddComponent<StrokeManager>();
            }
        }

        /// <summary>
        /// Checks if drawing is possible
        /// </summary>
        private bool CanDraw()
        {
            if (stylus == null) return false;

            float analogInput = Mathf.Max(stylus.CurrentState.tip_value, stylus.CurrentState.cluster_middle_value);
            return analogInput > 0 && stylus.CurrentState.isActive;
        }

        /// <summary>
        /// Starts drawing a new line
        /// </summary>
        private void StartNewLine()
        {
            currentLinePoints = new List<Vector3>();
            currentLineWidths = new List<float>();
            isDrawing = true;
            previousPoint = Vector3.zero;

            // Create a new mesh object for this line
            currentMeshObj = new GameObject("MeshLine");
            currentMeshObj.transform.SetParent(transform); // Temporarily parent to this object
            currentMeshObj.AddComponent<MeshFilter>();
            MeshRenderer renderer = currentMeshObj.AddComponent<MeshRenderer>();
            renderer.material = new Material(lineMaterial);

            OnDrawingStarted?.Invoke();
        }

        /// <summary>
        /// Triggers haptic feedback
        /// </summary>
        private void TriggerHaptics()
        {
            if (stylus is MxInkHandler mxStylus)
            {
                float pressure = stylus.CurrentState.cluster_middle_value * hapticDampingFactor;
                mxStylus.TriggerHapticPulse(pressure, hapticDuration);
            }
        }

        /// <summary>
        /// Adds a point to the current line
        /// </summary>
        /// <param name="position">The position to add</param>
        /// <param name="width">The width at this position</param>
        private void AddPoint(Vector3 position, float width)
        {
            if (currentLinePoints.Count == 0 || Vector3.Distance(position, previousPoint) > minDistanceBetweenPoints)
            {
                TriggerHaptics();

                // Add the point
                currentLinePoints.Add(position);
                currentLineWidths.Add(Mathf.Max(width * lineWidth, minLineWidth));
                previousPoint = position;

                // Generate or update mesh if we have enough points
                if (currentLinePoints.Count > 1)
                {
                    GenerateMeshLine();
                }
            }
        }

        /// <summary>
        /// Generates the mesh for the current line
        /// </summary>
        private void GenerateMeshLine()
        {
            if (currentMeshObj == null || currentLinePoints.Count < 2)
                return;

            // Create interpolated points for smoother lines
            List<Vector3> interpolatedPoints;
            List<float> interpolatedWidths;

            MeshLineUtility.InterpolatePoints(
                currentLinePoints,
                currentLineWidths,
                maxSegmentLength,
                out interpolatedPoints,
                out interpolatedWidths);

            // Generate mesh from the interpolated points
            Mesh mesh = MeshLineUtility.GenerateMeshFromLine(interpolatedPoints, interpolatedWidths);

            if (mesh != null)
            {
                MeshFilter meshFilter = currentMeshObj.GetComponent<MeshFilter>();
                meshFilter.mesh = mesh;
            }
        }

        /// <summary>
        /// Finishes the current drawing
        /// </summary>
        private void FinishDrawing()
        {
            isDrawing = false;

            if (currentLinePoints.Count > 1 && currentMeshObj != null)
            {
                // Notify that drawing has ended, so external systems can process it
                OnDrawingEnded?.Invoke(currentMeshObj, currentLinePoints, currentLineWidths);

                // Add the stroke to the manager
                strokeManager.CreateStroke(currentMeshObj, currentLinePoints, currentLineWidths);
            }
            else if (currentMeshObj != null)
            {
                // Too few points, destroy the mesh object
                Destroy(currentMeshObj);
            }

            // Reset state
            currentMeshObj = null;
            currentLinePoints.Clear();
            currentLineWidths.Clear();
        }

        /// <summary>
        /// Update is called once per frame
        /// </summary>
        private void Update()
        {
            if (stylus == null) return;

            HandleDrawing();
            HandleUndo();
        }

        /// <summary>
        /// Handles the line drawing process
        /// </summary>
        private void HandleDrawing()
        {
            float analogInput = Mathf.Max(stylus.CurrentState.tip_value, stylus.CurrentState.cluster_middle_value);

            if (analogInput > 0 && CanDraw())
            {
                if (!isDrawing)
                {
                    StartNewLine();
                }
                AddPoint(stylus.CurrentState.inkingPose.position, analogInput);
            }
            else if (isDrawing)
            {
                FinishDrawing();
            }
        }

        /// <summary>
        /// Handles undo operation for strokes
        /// </summary>
        private void HandleUndo()
        {
            // Undo functionality
            if (stylus.CurrentState.cluster_back_double_tap_value ||
                stylus.CurrentState.cluster_back_value)
            {
                if (!doubleTapDetected && strokeManager.Strokes.Count > 0)
                {
                    strokeManager.RemoveLastStroke();
                }
                doubleTapDetected = true;
            }
            else
            {
                doubleTapDetected = false;
            }
        }
    }
}