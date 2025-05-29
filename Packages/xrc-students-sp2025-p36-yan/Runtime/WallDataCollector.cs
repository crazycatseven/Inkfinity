using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using System;
using UnityEngine.Rendering;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Collects and visualizes wall data.
    /// </summary>
    public class WallDataCollector : MonoBehaviour
    {
        [Header("Visualization Settings")]
        // Material for walls - can be assigned in Inspector
        public Material wallMaterial;
        [Tooltip("Enable to visualize walls with material")]
        public bool enableVisualization = true;

        [Header("Collision Settings")]
        [Tooltip("Enable to add colliders to walls for interaction")]
        public bool enableColliders = true;
        [Tooltip("If true, wall colliders will be triggers (no physical collision)")]
        public bool useTriggersForColliders = false;

        [Header("Normal Direction")]
        [Tooltip("If true, normals will be flipped to point into the room")]
        public bool flipNormalsInward = true;
        [Tooltip("Debug visualization of wall normals")]
        public bool showNormalGizmos = false;
        [Tooltip("Length of normal debug lines")]
        public float normalGizmoLength = 0.5f;

        private Material _defaultWallMaterialInstance;

        // List of created wall visualizers
        private List<GameObject> _wallVisualizers = new List<GameObject>();

        /// <summary>
        /// Initialize and register callbacks.
        /// </summary>
        void Start()
        {
            if (MRUK.Instance == null)
            {
                Debug.LogError("MRUK.Instance not found. Please ensure MRUK prefab exists in scene.");
                return;
            }
            MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
        }

        /// <summary>
        /// Called when MR scene loaded.
        /// </summary>
        void OnSceneLoaded()
        {
            Debug.Log("MRUK scene loaded. Starting wall data visualization...");
            ClearWallVisualizers();

            MRUKRoom currentRoom = MRUK.Instance.GetCurrentRoom();

            if (currentRoom == null)
            {
                Debug.LogWarning("Current room not found. Cannot get wall data.");
                return;
            }

            Debug.Log($"Checking room: {currentRoom.gameObject.name} (UUID: {currentRoom.Anchor.Uuid})");

            List<MRUKAnchor> wallAnchors = new List<MRUKAnchor>();
            if (currentRoom.WallAnchors != null && currentRoom.WallAnchors.Count > 0)
            {
                Debug.Log($"Found {currentRoom.WallAnchors.Count} walls through currentRoom.WallAnchors.");
                wallAnchors.AddRange(currentRoom.WallAnchors);
            }
            else
            {
                Debug.Log("currentRoom.WallAnchors empty or uninitialized, trying to filter all anchors by label.");
                foreach (MRUKAnchor anchor in currentRoom.Anchors)
                {
                    if (anchor.Label.HasFlag(MRUKAnchor.SceneLabels.WALL_FACE))
                    {
                        if (!wallAnchors.Contains(anchor))
                        {
                            wallAnchors.Add(anchor);
                        }
                    }
                }
                Debug.Log($"Found {wallAnchors.Count} walls by filtering all anchors by label.");
            }

            if (wallAnchors.Count == 0)
            {
                Debug.LogWarning("No wall anchors found in this room.");
                return;
            }

            Debug.Log($"Found {wallAnchors.Count} wall anchors, creating visualizations...");

            // Prepare material if visualization is enabled
            if (enableVisualization && wallMaterial == null)
            {
                CreateDefaultMaterial();
            }

            int wallIndex = 0;
            foreach (MRUKAnchor wallAnchor in wallAnchors)
            {
                Debug.Log($"--- Processing wall {++wallIndex} (anchor: {wallAnchor.gameObject.name}) ---");

                if (!wallAnchor.PlaneRect.HasValue)
                {
                    Debug.LogWarning($"Wall anchor {wallAnchor.gameObject.name} has no PlaneRect data, cannot create Mesh.");
                    continue;
                }

                // Create GameObject for wall representation
                GameObject wallViz = new GameObject($"WallViz_{wallAnchor.Anchor.Uuid}");
                wallViz.transform.SetParent(this.transform);

                // Set transform (position and rotation aligned with anchor)
                wallViz.transform.position = wallAnchor.transform.position;
                wallViz.transform.rotation = wallAnchor.transform.rotation;
                wallViz.transform.localScale = Vector3.one;

                // Create mesh
                Mesh wallMesh = CreatePlaneMeshForWall(wallAnchor);
                if (wallMesh == null)
                {
                    Debug.LogWarning($"Failed to create Mesh for wall {wallAnchor.gameObject.name}.");
                    Destroy(wallViz);
                    continue;
                }

                // Add visualization components if enabled
                if (enableVisualization)
                {
                    MeshFilter meshFilter = wallViz.AddComponent<MeshFilter>();
                    MeshRenderer meshRenderer = wallViz.AddComponent<MeshRenderer>();
                    meshFilter.mesh = wallMesh;

                    // Apply material
                    if (wallMaterial != null)
                    {
                        meshRenderer.material = wallMaterial;
                    }
                    else if (_defaultWallMaterialInstance != null)
                    {
                        meshRenderer.material = _defaultWallMaterialInstance;
                    }
                    else
                    {
                        Debug.LogError("Wall material not set and couldn't create default material!");
                    }
                }

                // Add MeshCollider if enabled
                if (enableColliders)
                {
                    MeshCollider meshCollider = wallViz.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = wallMesh;
                    meshCollider.isTrigger = useTriggersForColliders;
                }

                // Add component to visualize normals if debug is enabled
                if (showNormalGizmos)
                {
                    WallNormalVisualizer normalViz = wallViz.AddComponent<WallNormalVisualizer>();
                    normalViz.normalLength = normalGizmoLength;
                }

                _wallVisualizers.Add(wallViz);
            }
        }

        /// <summary>
        /// Create default wall material.
        /// </summary>
        private void CreateDefaultMaterial()
        {
            Debug.Log("Creating default URP Unlit material for wall visualization.");
            // Check for URP
            if (GraphicsSettings.currentRenderPipeline != null &&
                GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("UniversalRenderPipelineAsset"))
            {
                Shader urpUnlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (urpUnlitShader != null)
                {
                    _defaultWallMaterialInstance = new Material(urpUnlitShader);
                }
                else
                {
                    Debug.LogWarning("Cannot find 'Universal Render Pipeline/Unlit' shader. Using vertex color material.");
                    _defaultWallMaterialInstance = CreateFallbackMaterial();
                }
            }
            else // If not URP or URP Unlit not found, use a simple one
            {
                Debug.LogWarning("Current render pipeline is not URP or cannot be determined. Using fallback material.");
                _defaultWallMaterialInstance = CreateFallbackMaterial();
            }
            wallMaterial = _defaultWallMaterialInstance;
        }

        /// <summary>
        /// Create fallback material.
        /// </summary>
        private Material CreateFallbackMaterial()
        {
            Material material = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse"));
            return material;
        }

        /// <summary>
        /// Create wall mesh.
        /// </summary>
        Mesh CreatePlaneMeshForWall(MRUKAnchor wallAnchor)
        {
            if (!wallAnchor.PlaneRect.HasValue) return null;

            Rect localRect = wallAnchor.PlaneRect.Value;
            float width = localRect.width;
            float height = localRect.height;
            Vector2 offset = localRect.center;

            Mesh mesh = new Mesh();

            // Vertices positioned according to PlaneRect in local coordinates
            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(localRect.xMin, localRect.yMin, 0), // Bottom-left
                new Vector3(localRect.xMax, localRect.yMin, 0), // Bottom-right
                new Vector3(localRect.xMin, localRect.yMax, 0), // Top-left
                new Vector3(localRect.xMax, localRect.yMax, 0)  // Top-right
            };
            mesh.vertices = vertices;

            // Triangle indices - order depends on whether we want normals pointing inward
            int[] tris;

            if (flipNormalsInward)
            {
                // Triangles ordered to make normals point along -Z (into the room)
                tris = new int[6]
                {
                    0, 1, 2, // First triangle
                    2, 1, 3  // Second triangle
                };
            }
            else
            {
                // Default triangles with normals along +Z (out of the room)
                tris = new int[6]
                {
                    0, 2, 1, // First triangle
                    1, 2, 3  // Second triangle
                };
            }

            mesh.triangles = tris;

            // UV coordinates (simple plane mapping)
            Vector2[] uv = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            mesh.uv = uv;

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Clear all wall visualizers.
        /// </summary>
        void ClearWallVisualizers()
        {
            foreach (GameObject viz in _wallVisualizers)
            {
                Destroy(viz);
            }
            _wallVisualizers.Clear();
        }

        /// <summary>
        /// Cleanup on destroy.
        /// </summary>
        void OnDestroy()
        {
            if (MRUK.Instance != null)
            {
                MRUK.Instance.SceneLoadedEvent.RemoveListener(OnSceneLoaded);
            }
            ClearWallVisualizers();
            // Clean up default material instance if created
            if (_defaultWallMaterialInstance != null)
            {
                Destroy(_defaultWallMaterialInstance);
            }
        }
    }

    /// <summary>
    /// Visualizes wall normals in editor.
    /// </summary>
    [ExecuteInEditMode]
    public class WallNormalVisualizer : MonoBehaviour
    {
        public float normalLength = 0.5f;
        private MeshFilter meshFilter;

        void OnEnable()
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        void OnDrawGizmos()
        {
            if (meshFilter == null || meshFilter.sharedMesh == null)
                return;

            Mesh mesh = meshFilter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;

            Gizmos.color = Color.blue;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldPos = transform.TransformPoint(vertices[i]);
                Vector3 worldNormal = transform.TransformDirection(normals[i]);
                Gizmos.DrawLine(worldPos, worldPos + worldNormal * normalLength);
            }

            // Draw center normal
            Vector3 center = mesh.bounds.center;
            Vector3 worldCenter = transform.TransformPoint(center);

            // Get normal direction at center - use first triangle's normal for simplicity
            if (normals.Length > 0)
            {
                Vector3 centerNormal = normals[0]; // Use first normal for demonstration
                Vector3 worldCenterNormal = transform.TransformDirection(centerNormal);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(worldCenter, worldCenter + worldCenterNormal * normalLength * 1.5f);
            }
        }
    }
}