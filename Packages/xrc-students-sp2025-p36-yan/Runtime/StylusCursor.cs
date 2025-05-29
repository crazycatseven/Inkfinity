using UnityEngine;
using System.Collections.Generic;
using XRC.Students.Sp2025.P36.Yan;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Stylus surface interaction and cursor feedback.
    /// </summary>
    public class StylusCursor : MonoBehaviour
    {
        [Header("Cursor Settings")]
        [SerializeField] private GameObject cursorPrefab;
        [Tooltip("Pressure scale multiplier (1.0 = no change, 1.5 = 50% larger at max pressure)")]
        [SerializeField] private float pressureScaleMultiplier = 1.5f;
        [SerializeField] private float heightOffset = 0.001f;

        [Header("Cursor Colors")]
        [SerializeField] private Color baseColor = new Color(0.2f, 0.6f, 1f, 0.8f);
        [SerializeField] private Color pressureColor = new Color(0f, 0.8f, 0.2f, 0.8f);
        [SerializeField] private string rendererPath = "";

        [Header("Interaction Settings")]
        [SerializeField] private string interactableTag = "StylusInteractable";
        [SerializeField] private float maxInteractionDistance = 0.1f;
        [SerializeField] private float pressureThreshold = 0.1f;
        [SerializeField] private MxInkHandler stylusHandler;

        [Header("Debug")]
        [SerializeField] private bool showDebugRay = true;
        [SerializeField] private Color rayHitColor = Color.green;
        [SerializeField] private Color rayMissColor = Color.red;

        // Runtime references
        private GameObject cursorInstance;
        private Renderer cursorRenderer;
        private bool isCursorVisible = false;
        private RaycastHit lastHit;
        private bool isValidSurface = false;
        private Vector3 originalScale;

        private void Awake()
        {
            InitializeCursor();
        }

        private void Start()
        {
            if (stylusHandler == null)
            {
                stylusHandler = GetComponentInParent<MxInkHandler>();
                if (stylusHandler == null)
                {
                    Debug.LogError("StylusCursor: MxInkHandler reference not set");
                    enabled = false;
                    return;
                }
            }
        }

        private void Update()
        {
            UpdateCursor();
        }

        /// <summary>
        /// Initialize cursor instance.
        /// </summary>
        private void InitializeCursor()
        {
            if (cursorPrefab == null)
            {
                Debug.LogError("StylusCursor: Cursor prefab not assigned");
                enabled = false;
                return;
            }

            // Create cursor instance at scene root
            cursorInstance = Instantiate(cursorPrefab);
            cursorInstance.name = "StylusCursor";

            // Store original scale
            originalScale = cursorInstance.transform.localScale;

            // Find renderer component
            if (string.IsNullOrEmpty(rendererPath))
            {
                cursorRenderer = cursorInstance.GetComponentInChildren<Renderer>();
            }
            else
            {
                Transform rendererTransform = cursorInstance.transform.Find(rendererPath);
                if (rendererTransform != null)
                {
                    cursorRenderer = rendererTransform.GetComponent<Renderer>();
                }
                else
                {
                    cursorRenderer = cursorInstance.GetComponentInChildren<Renderer>();
                }
            }

            if (cursorRenderer == null)
            {
                Debug.LogWarning("StylusCursor: No renderer found in cursor prefab, color changes will not apply");
            }
            else
            {
                // Set initial color
                SetCursorColor(baseColor);
            }

            // Initially hide cursor
            cursorInstance.SetActive(false);

            Debug.Log("Cursor initialized using prefab");
        }

        /// <summary>
        /// Update cursor state.
        /// </summary>
        private void UpdateCursor()
        {
            if (stylusHandler == null || stylusHandler.TipTransform == null) return;

            Vector3 rayOrigin = stylusHandler.TipTransform.position;
            Vector3 rayDirection = -stylusHandler.TipTransform.up.normalized;

            bool hitSomething = Physics.Raycast(rayOrigin, rayDirection, out lastHit, maxInteractionDistance);

            if (showDebugRay)
            {
                Debug.DrawRay(rayOrigin, rayDirection * (hitSomething ? lastHit.distance : maxInteractionDistance),
                    hitSomething ? rayHitColor : rayMissColor);
            }

            if (hitSomething)
            {
                isValidSurface = IsValidInteractable(lastHit);

                if (isValidSurface)
                {
                    UpdateCursorTransform(lastHit.point, lastHit.normal);
                    UpdateCursorAppearance();
                    ShowCursor(true);
                }
                else
                {
                    ShowCursor(false);
                }
            }
            else
            {
                isValidSurface = false;
                ShowCursor(false);
            }
        }

        /// <summary>
        /// Check if hit is valid interactable.
        /// </summary>
        private bool IsValidInteractable(RaycastHit hit)
        {
            return hit.collider.CompareTag(interactableTag);
        }

        /// <summary>
        /// Update cursor transform.
        /// </summary>
        private void UpdateCursorTransform(Vector3 position, Vector3 normal)
        {
            // Position slightly above surface
            Vector3 adjustedPosition = position + normal * heightOffset;
            cursorInstance.transform.position = adjustedPosition;

            // Orient to surface with X rotation (prefab faces -Z)
            Quaternion normalRotation = Quaternion.FromToRotation(Vector3.up, normal);
            cursorInstance.transform.rotation = normalRotation * Quaternion.Euler(90, 0, 0);
        }

        /// <summary>
        /// Update cursor appearance.
        /// </summary>
        private void UpdateCursorAppearance()
        {
            if (cursorInstance == null) return;

            float pressure = stylusHandler.CurrentState.tip_value;

            // Calculate scale based on pressure only
            if (pressure > pressureThreshold)
            {
                float normalizedPressure = Mathf.InverseLerp(pressureThreshold, 1f, pressure);
                float scaleMultiplier = Mathf.Lerp(1.0f, pressureScaleMultiplier, normalizedPressure);
                cursorInstance.transform.localScale = originalScale * scaleMultiplier;

                // Lerp color based on pressure
                SetCursorColor(Color.Lerp(baseColor, pressureColor, normalizedPressure));
            }
            else
            {
                // Reset to original scale and color
                cursorInstance.transform.localScale = originalScale;
                SetCursorColor(baseColor);
            }
        }

        /// <summary>
        /// Set cursor color.
        /// </summary>
        private void SetCursorColor(Color color)
        {
            if (cursorRenderer != null)
            {
                // Handle different renderer types
                if (cursorRenderer is MeshRenderer meshRenderer)
                {
                    meshRenderer.material.color = color;
                }
                else if (cursorRenderer is SpriteRenderer spriteRenderer)
                {
                    spriteRenderer.color = color;
                }
            }
        }

        /// <summary>
        /// Show or hide cursor.
        /// </summary>
        private void ShowCursor(bool visible)
        {
            if (isCursorVisible != visible)
            {
                cursorInstance.SetActive(visible);
                isCursorVisible = visible;
            }
        }

        /// <summary>
        /// Set cursor colors.
        /// </summary>
        public void SetCursorColors(Color baseCol, Color pressureCol)
        {
            baseColor = baseCol;
            pressureColor = pressureCol;

            // Update current color if visible
            if (isCursorVisible)
            {
                UpdateCursorAppearance();
            }
        }

        /// <summary>
        /// Set pressure scale multiplier.
        /// </summary>
        public void SetPressureScaleMultiplier(float multiplier)
        {
            pressureScaleMultiplier = multiplier;

            // Update current scale if visible
            if (isCursorVisible)
            {
                UpdateCursorAppearance();
            }
        }

        /// <summary>
        /// Is hovering valid surface.
        /// </summary>
        public bool IsHoveringValidSurface()
        {
            return isValidSurface;
        }

        /// <summary>
        /// Get current hit.
        /// </summary>
        public RaycastHit GetCurrentHit()
        {
            return lastHit;
        }

        private void OnDestroy()
        {
            // Clean up cursor instance when component is destroyed
            if (cursorInstance != null)
            {
                Destroy(cursorInstance);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || stylusHandler == null || stylusHandler.TipTransform == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(stylusHandler.TipTransform.position, 0.005f);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(stylusHandler.TipTransform.position, stylusHandler.TipTransform.forward * 0.05f);
        }
    }
}