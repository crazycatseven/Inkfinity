// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using System;
using PassthroughCameraSamples;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Captures camera frames from VR camera.
    /// </summary>
    public class VRCameraCaptureTexture : MonoBehaviour
    {
        [SerializeField, Tooltip("Width of the captured texture")]
        private int captureWidth = 1024;

        [SerializeField, Tooltip("Height of the captured texture")]
        private int captureHeight = 1024;

        [SerializeField, Range(0, 30), Tooltip("Capture rate (0 = capture on demand only)")]
        private int framesPerSecond = 0;

        [SerializeField, Tooltip("Which passthrough eye to use for capture")]
        private PassthroughCameraEye passthroughEye = PassthroughCameraEye.Left;

        private Camera targetCamera;
        private float captureTimer = 0f;
        private float captureInterval;
        private RenderTexture renderTexture;
        private Texture2D captureTexture;
        private bool isEditorMode = false;

        /// <summary>
        /// Initialize camera and textures
        /// </summary>
        void Start()
        {
            // Check if running in editor mode
#if UNITY_EDITOR
            isEditorMode = true;
#endif

            if (targetCamera == null)
            {
                if (isEditorMode && Camera.main != null)
                {
                    targetCamera = Camera.main;
                    Debug.Log("Editor mode: using main camera");
                }
                else
                {
                    GameObject camObj = new GameObject("VRCaptureCamera");
                    camObj.transform.parent = this.transform;
                    targetCamera = camObj.AddComponent<Camera>();
                    targetCamera.enabled = false;
                    targetCamera.clearFlags = CameraClearFlags.SolidColor;
                    targetCamera.backgroundColor = new Color(0, 0, 0, 0);
                }
            }

            if (!isEditorMode)
            {
                // Get real camera world pose
                var pose = PassthroughCameraUtils.GetCameraPoseInWorld(passthroughEye);
                targetCamera.transform.position = pose.position;
                targetCamera.transform.rotation = pose.rotation;

                // Get real camera intrinsics
                var intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(passthroughEye);
                captureWidth = intrinsics.Resolution.x;
                captureHeight = intrinsics.Resolution.y;

                // Set projection matrix
                targetCamera.projectionMatrix = GetProjectionMatrix(intrinsics, targetCamera.nearClipPlane, targetCamera.farClipPlane);
            }

            // Initialize render texture and capture texture
            renderTexture = new RenderTexture(captureWidth, captureHeight, 24, RenderTextureFormat.ARGB32);
            captureTexture = new Texture2D(captureWidth, captureHeight, TextureFormat.RGBA32, false);
        }

        /// <summary>
        /// Update camera position and handle timed captures
        /// </summary>
        void Update()
        {
            if (!isEditorMode)
            {
                var pose = PassthroughCameraUtils.GetCameraPoseInWorld(passthroughEye);
                targetCamera.transform.position = pose.position;
                targetCamera.transform.rotation = pose.rotation;
            }

            captureTimer += Time.deltaTime;

            if (framesPerSecond > 0)
            {
                captureInterval = 1f / framesPerSecond;

                if (captureTimer >= captureInterval)
                {
                    captureTimer = 0f;
                    CaptureFrame();
                }
            }
        }

        /// <summary>
        /// Capture the current camera view to texture
        /// </summary>
        private void CaptureFrame()
        {
            targetCamera.targetTexture = renderTexture;
            targetCamera.Render();

            targetCamera.targetTexture = null;

            RenderTexture.active = renderTexture;

            captureTexture.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
            captureTexture.Apply();

            RenderTexture.active = null;
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        void OnDestroy()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }

            if (captureTexture != null)
            {
                Destroy(captureTexture);
            }
        }

        /// <summary>
        /// Get the latest captured texture, capturing a new frame if needed
        /// </summary>
        /// <returns>The captured camera texture</returns>
        public Texture2D GetCaptureTexture()
        {
            if (framesPerSecond > 0)
            {
                return captureTexture;
            }
            else
            {
                CaptureFrame();
                return captureTexture;
            }
        }

        /// <summary>
        /// Create a projection matrix from camera intrinsics
        /// </summary>
        /// <param name="intrinsics">Camera intrinsic parameters</param>
        /// <param name="near">Near clipping plane</param>
        /// <param name="far">Far clipping plane</param>
        /// <returns>A projection matrix for the camera</returns>
        private Matrix4x4 GetProjectionMatrix(PassthroughCameraIntrinsics intrinsics, float near, float far)
        {
            float fx = intrinsics.FocalLength.x;
            float fy = intrinsics.FocalLength.y;
            float cx = intrinsics.PrincipalPoint.x;
            float cy = intrinsics.PrincipalPoint.y;
            float w = intrinsics.Resolution.x;
            float h = intrinsics.Resolution.y;

            float left = -cx * near / fx;
            float right = (w - cx) * near / fx;
            float bottom = -(h - cy) * near / fy;
            float top = cy * near / fy;

            return Matrix4x4.Frustum(left, right, bottom, top, near, far);
        }

        public Camera GetTargetCamera()
        {
            return targetCamera;
        }

        public Rect WorldBoundsToScreenRect(Bounds bounds)
        {
            Vector3[] corners = new Vector3[8];
            corners[0] = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
            corners[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
            corners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
            corners[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
            corners[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
            corners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
            corners[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
            corners[7] = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            foreach (var corner in corners)
            {
                Vector2 screenPoint = targetCamera.WorldToScreenPoint(corner);
                min = Vector2.Min(min, screenPoint);
                max = Vector2.Max(max, screenPoint);
            }

            float padding = 10f;
            min -= new Vector2(padding, padding);
            max += new Vector2(padding, padding);

            min.x = Mathf.Max(0, min.x);
            min.y = Mathf.Max(0, min.y);
            max.x = Mathf.Min(captureWidth, max.x);
            max.y = Mathf.Min(captureHeight, max.y);

            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        public int GetCaptureWidth()
        {
            return captureWidth;
        }

        public int GetCaptureHeight()
        {
            return captureHeight;
        }
    }
}