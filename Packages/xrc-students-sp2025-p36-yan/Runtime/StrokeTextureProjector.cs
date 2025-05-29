using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace XRC.Students.Sp2025.P36.Yan
{
    public class StrokeTextureProjector : MonoBehaviour
    {
        [SerializeField]
        private float proximityThreshold = 0.2f;
        [SerializeField]
        private int maxStrokesToConsider = 50;
        [SerializeField]
        private int textureResolution = 1024;
        [SerializeField]
        private float paddingPercent = 0.1f;
        [SerializeField]
        private int penThickness = 3;
        [SerializeField]
        private bool saveDebugImages = true;
        [SerializeField]
        private VRCameraCaptureTexture vrCameraCaptureTexture;

        private static StrokeTextureProjector instance;
        public static StrokeTextureProjector Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<StrokeTextureProjector>();
                    if (instance == null)
                    {
                        GameObject obj = new GameObject("StrokeTextureProjector");
                        instance = obj.AddComponent<StrokeTextureProjector>();
                    }
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            if (vrCameraCaptureTexture == null)
            {
                vrCameraCaptureTexture = FindObjectOfType<VRCameraCaptureTexture>();
            }
        }

        /// <summary>
        /// Generate a texture from the last stroke and its related strokes. Returns the center and used strokes.
        /// </summary>
        public Texture2D GeneratePlaneTextureFromLastStroke(out Vector3 centerPosition, out List<Stroke> usedStrokes)
        {
            centerPosition = Vector3.zero;
            usedStrokes = new List<Stroke>();
            StrokeManager strokeManager = StrokeManager.Instance;
            if (strokeManager == null || strokeManager.Strokes.Count == 0)
                return null;
            Stroke lastStroke = strokeManager.GetLastStroke();
            List<Stroke> relatedStrokes = FindRelatedStrokes(lastStroke);
            usedStrokes = relatedStrokes;
            if (relatedStrokes.Count == 0)
                return null;
            Vector3 center = Vector3.zero;
            foreach (var stroke in relatedStrokes)
                center += stroke.Center;
            center /= relatedStrokes.Count;
            centerPosition = center;
            Texture2D texture = ProjectStrokesToTexture(relatedStrokes);
            if (saveDebugImages && texture != null)
                ImageProcessingUtility.SaveTextureToFile(texture, "Auto_Plane", "AutoPlaneImages");
            return texture;
        }

        public Texture2D GeneratePlaneTextureFromLastStroke()
        {
            return GeneratePlaneTextureFromLastStroke(out _, out _);
        }

        /// <summary>
        /// Entry point for Marking Menu. Generates a texture from the last stroke and related strokes.
        /// </summary>
        public void CaptureTextureFromLastStroke()
        {
            StrokeManager strokeManager = StrokeManager.Instance;
            if (strokeManager == null || strokeManager.Strokes.Count == 0)
                return;
            Texture2D planeTexture = GeneratePlaneTextureFromLastStroke();
            if (planeTexture != null)
                Debug.Log("Texture captured. Size: " + planeTexture.width + "x" + planeTexture.height);
            else
                Debug.LogWarning("Failed to capture texture. Not enough strokes.");
        }

        // Find all strokes related to the start stroke.
        private List<Stroke> FindRelatedStrokes(Stroke startStroke)
        {
            HashSet<Stroke> processedStrokes = new HashSet<Stroke>();
            Queue<Stroke> strokeQueue = new Queue<Stroke>();
            List<Stroke> relatedStrokes = new List<Stroke>();
            strokeQueue.Enqueue(startStroke);
            processedStrokes.Add(startStroke);
            relatedStrokes.Add(startStroke);
            StrokeManager strokeManager = StrokeManager.Instance;
            IReadOnlyList<Stroke> allStrokes = strokeManager.Strokes;
            Vector3 planeNormal = EstimatePlaneNormal(startStroke);
            float adjustedThreshold = proximityThreshold;
            if (IsLikelyTextContent(startStroke))
                adjustedThreshold = proximityThreshold * 2.0f;
            while (strokeQueue.Count > 0 && relatedStrokes.Count < maxStrokesToConsider)
            {
                Stroke currentStroke = strokeQueue.Dequeue();
                foreach (Stroke stroke in allStrokes)
                {
                    if (processedStrokes.Contains(stroke))
                        continue;
                    if (AreStrokesRelated(currentStroke, stroke, adjustedThreshold, planeNormal))
                    {
                        strokeQueue.Enqueue(stroke);
                        processedStrokes.Add(stroke);
                        relatedStrokes.Add(stroke);
                        if (relatedStrokes.Count >= maxStrokesToConsider)
                            break;
                    }
                }
            }
            return relatedStrokes;
        }

        // Estimate the normal of the plane where the stroke lies.
        private Vector3 EstimatePlaneNormal(Stroke stroke)
        {
            if (stroke.Points.Count < 3)
                return Vector3.up;
            Vector3 v1 = stroke.Points[1] - stroke.Points[0];
            Vector3 v2 = stroke.Points[2] - stroke.Points[0];
            Vector3 normal = Vector3.Cross(v1, v2).normalized;
            if (Mathf.Abs(Vector3.Dot(normal, Vector3.up)) < 0.3f)
                normal = Vector3.up;
            return normal;
        }

        // Heuristic: is this stroke likely to be text?
        private bool IsLikelyTextContent(Stroke stroke)
        {
            if (stroke.Points.Count < 5)
                return false;
            float pathLength = 0;
            for (int i = 1; i < stroke.Points.Count; i++)
                pathLength += Vector3.Distance(stroke.Points[i], stroke.Points[i - 1]);
            float directDistance = Vector3.Distance(stroke.Points[0], stroke.Points[stroke.Points.Count - 1]);
            float ratio = pathLength / directDistance;
            return ratio > 1.5f;
        }

        // Are two strokes related (distance and plane)?
        private bool AreStrokesRelated(Stroke stroke1, Stroke stroke2, float threshold, Vector3 planeNormal)
        {
            if (stroke1 == stroke2)
                return true;
            bool samePlane = true;
            if (planeNormal != Vector3.zero)
            {
                float dot1 = Vector3.Dot(stroke1.Center, planeNormal);
                float dot2 = Vector3.Dot(stroke2.Center, planeNormal);
                if (Mathf.Abs(dot1 - dot2) > threshold * 2)
                    samePlane = false;
            }
            if (!samePlane)
                return false;
            float distance = Vector3.Distance(stroke1.Center, stroke2.Center);
            if (distance <= threshold)
                return true;
            float minDistance = float.MaxValue;
            foreach (Vector3 point1 in stroke1.Points)
            {
                foreach (Vector3 point2 in stroke2.Points)
                {
                    float dist = Vector3.Distance(point1, point2);
                    minDistance = Mathf.Min(minDistance, dist);
                    if (minDistance <= threshold)
                        return true;
                }
            }
            return false;
        }

        // Project strokes to a 2D plane using camera parameters and ignoring Z-tilt.
        private Texture2D ProjectStrokesToTexture(List<Stroke> strokes)
        {
            List<Vector3> allPoints = new List<Vector3>();
            foreach (var stroke in strokes)
                allPoints.AddRange(stroke.Points);
            if (allPoints.Count < 3)
                return null;
            Vector3 center = Vector3.zero;
            foreach (var point in allPoints)
                center += point;
            center /= allPoints.Count;
            Camera camera = vrCameraCaptureTexture != null ? vrCameraCaptureTexture.GetTargetCamera() : Camera.main;
            Vector3 viewDirection = (center - camera.transform.position).normalized;
            Vector3 normal = viewDirection;
            Vector3 camUp = camera.transform.up;
            Vector3 up = camUp - Vector3.Dot(camUp, normal) * normal;
            if (up.magnitude < 0.001f)
            {
                up = Vector3.up - Vector3.Dot(Vector3.up, normal) * normal;
                if (up.magnitude < 0.001f)
                    up = Vector3.forward - Vector3.Dot(Vector3.forward, normal) * normal;
            }
            up = up.normalized;
            Vector3 principalAxis1 = Vector3.Cross(up, normal).normalized;
            Vector3 principalAxis2 = Vector3.Cross(normal, principalAxis1).normalized;
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            List<Vector2> projectedPoints = new List<Vector2>();
            foreach (var point in allPoints)
            {
                Vector3 centered = point - center;
                Vector2 projected = new Vector2(
                    Vector3.Dot(centered, principalAxis1),
                    Vector3.Dot(centered, principalAxis2)
                );
                projectedPoints.Add(projected);
            }
            foreach (var point in projectedPoints)
            {
                minX = Mathf.Min(minX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxX = Mathf.Max(maxX, point.x);
                maxY = Mathf.Max(maxY, point.y);
            }
            float width = maxX - minX;
            float height = maxY - minY;
            float paddingX = width * paddingPercent;
            float paddingY = height * paddingPercent;
            minX -= paddingX;
            minY -= paddingY;
            maxX += paddingX;
            maxY += paddingY;
            width = maxX - minX;
            height = maxY - minY;
            int texWidth, texHeight;
            float aspectRatio = width / height;
            if (vrCameraCaptureTexture != null)
            {
                float cameraAspect = (float)vrCameraCaptureTexture.GetCaptureWidth() / vrCameraCaptureTexture.GetCaptureHeight();
                if (Mathf.Abs(Vector3.Dot(normal, Vector3.forward)) > 0.7f)
                    aspectRatio = cameraAspect;
            }
            if (width > height)
            {
                texWidth = textureResolution;
                texHeight = Mathf.RoundToInt(textureResolution / aspectRatio);
            }
            else
            {
                texHeight = textureResolution;
                texWidth = Mathf.RoundToInt(textureResolution * aspectRatio);
            }
            texWidth = Mathf.Clamp(texWidth, 256, 2048);
            texHeight = Mathf.Clamp(texHeight, 256, 2048);
            Texture2D texture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
            Color[] colors = new Color[texWidth * texHeight];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = Color.white;
            texture.SetPixels(colors);
            for (int s = 0; s < strokes.Count; s++)
            {
                Stroke stroke = strokes[s];
                if (stroke.Points.Count < 2)
                    continue;
                for (int i = 0; i < stroke.Points.Count - 1; i++)
                {
                    Vector3 p1 = stroke.Points[i] - center;
                    Vector3 p2 = stroke.Points[i + 1] - center;
                    Vector2 proj1 = new Vector2(
                        Vector3.Dot(p1, principalAxis1),
                        Vector3.Dot(p1, principalAxis2)
                    );
                    Vector2 proj2 = new Vector2(
                        Vector3.Dot(p2, principalAxis1),
                        Vector3.Dot(p2, principalAxis2)
                    );
                    int x1 = Mathf.RoundToInt((proj1.x - minX) / width * (texWidth - 1));
                    int y1 = Mathf.RoundToInt((proj1.y - minY) / height * (texHeight - 1));
                    int x2 = Mathf.RoundToInt((proj2.x - minX) / width * (texWidth - 1));
                    int y2 = Mathf.RoundToInt((proj2.y - minY) / height * (texHeight - 1));
                    ImageProcessingUtility.DrawLine(texture, x1, y1, x2, y2, Color.black, penThickness);
                }
            }
            texture.Apply();
            return texture;
        }
    }
}