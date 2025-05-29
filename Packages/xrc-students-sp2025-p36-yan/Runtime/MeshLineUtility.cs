using System.Collections.Generic;
using UnityEngine;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Utility class for generating and manipulating mesh-based lines
    /// </summary>
    public static class MeshLineUtility
    {
        /// <summary>
        /// Generates a mesh from a series of points and widths
        /// </summary>
        /// <param name="points">The points defining the line path</param>
        /// <param name="widths">The width at each point</param>
        /// <returns>A new mesh representing the line</returns>
        public static Mesh GenerateMeshFromLine(List<Vector3> points, List<float> widths)
        {
            if (points == null || widths == null || points.Count < 2 || points.Count != widths.Count)
            {
                Debug.LogError("Invalid data for mesh generation: points and widths must be non-null, have at least 2 points, and be the same length.");
                return null;
            }

            Mesh mesh = new Mesh();
            int pointCount = points.Count;

            Vector3[] vertices = new Vector3[pointCount * 2];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[(pointCount - 1) * 6];

            // Calculate cumulative distances for proper UV mapping
            float[] cumulativeDistances = new float[pointCount];
            cumulativeDistances[0] = 0;
            float totalDistance = 0;

            for (int i = 1; i < pointCount; i++)
            {
                totalDistance += Vector3.Distance(points[i], points[i - 1]);
                cumulativeDistances[i] = totalDistance;
            }

            // Calculate smoothed tangents for each point
            Vector3[] tangents = CalculateTangents(points);

            // Create vertices with proper orientation
            for (int i = 0; i < pointCount; i++)
            {
                Vector3 point = points[i];
                float width = widths[i];

                Vector3 tangent = tangents[i];
                Vector3 up = Vector3.up;

                // Avoid parallel vectors
                if (Mathf.Abs(Vector3.Dot(tangent, up)) > 0.99f)
                {
                    up = Vector3.right;
                }

                Vector3 right = Vector3.Cross(tangent, up).normalized;

                // Create ribbon vertices
                vertices[i * 2] = point + right * width * 0.5f;
                vertices[i * 2 + 1] = point - right * width * 0.5f;

                // Use standard 0-1 range UV mapping
                float t = (totalDistance > 0) ? cumulativeDistances[i] / totalDistance : 0;
                uvs[i * 2] = new Vector2(0, t);     // Left vertex, UV.x = 0
                uvs[i * 2 + 1] = new Vector2(1, t); // Right vertex, UV.x = 1

                // Set triangles
                if (i < pointCount - 1)
                {
                    int idx = i * 6;

                    triangles[idx] = i * 2;
                    triangles[idx + 1] = i * 2 + 1;
                    triangles[idx + 2] = i * 2 + 2;

                    triangles[idx + 3] = i * 2 + 1;
                    triangles[idx + 4] = i * 2 + 3;
                    triangles[idx + 5] = i * 2 + 2;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Calculates smoothed tangents for a series of points
        /// </summary>
        /// <param name="points">The points to calculate tangents for</param>
        /// <returns>Array of tangent vectors</returns>
        private static Vector3[] CalculateTangents(List<Vector3> points)
        {
            int pointCount = points.Count;
            Vector3[] tangents = new Vector3[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                if (i == 0)
                {
                    // First point
                    tangents[i] = (points[1] - points[0]).normalized;
                }
                else if (i == pointCount - 1)
                {
                    // Last point
                    tangents[i] = (points[i] - points[i - 1]).normalized;
                }
                else
                {
                    // Middle points - smooth curve tangent
                    Vector3 prevDir = (points[i] - points[i - 1]).normalized;
                    Vector3 nextDir = (points[i + 1] - points[i]).normalized;

                    // If sharp turn, don't average to avoid artifacts
                    if (Vector3.Dot(prevDir, nextDir) < 0.5f)
                    {
                        tangents[i] = prevDir;
                    }
                    else
                    {
                        tangents[i] = (prevDir + nextDir).normalized;
                    }
                }
            }

            return tangents;
        }

        /// <summary>
        /// Interpolates points to create a smoother line with more uniform spacing
        /// </summary>
        /// <param name="originalPoints">The original points to interpolate</param>
        /// <param name="originalWidths">The original widths corresponding to the points</param>
        /// <param name="maxSegmentLength">Maximum segment length between points</param>
        /// <param name="interpolatedPoints">Output list to receive the interpolated points</param>
        /// <param name="interpolatedWidths">Output list to receive the interpolated widths</param>
        public static void InterpolatePoints(
            List<Vector3> originalPoints,
            List<float> originalWidths,
            float maxSegmentLength,
            out List<Vector3> interpolatedPoints,
            out List<float> interpolatedWidths)
        {
            interpolatedPoints = new List<Vector3>();
            interpolatedWidths = new List<float>();

            if (originalPoints.Count == 0)
            {
                return;
            }

            // Add the first point
            interpolatedPoints.Add(originalPoints[0]);
            interpolatedWidths.Add(originalWidths[0]);

            // Process each segment
            for (int i = 0; i < originalPoints.Count - 1; i++)
            {
                Vector3 startPoint = originalPoints[i];
                Vector3 endPoint = originalPoints[i + 1];
                float startWidth = originalWidths[i];
                float endWidth = originalWidths[i + 1];

                float distance = Vector3.Distance(startPoint, endPoint);
                int segments = Mathf.CeilToInt(distance / maxSegmentLength);

                // Add intermediate points
                for (int j = 1; j <= segments; j++)
                {
                    float t = j / (float)segments;
                    Vector3 interpolatedPos = Vector3.Lerp(startPoint, endPoint, t);
                    float interpolatedWidth = Mathf.Lerp(startWidth, endWidth, t);

                    interpolatedPoints.Add(interpolatedPos);
                    interpolatedWidths.Add(interpolatedWidth);
                }
            }
        }

        /// <summary>
        /// Checks if a point is inside a polygon on the XZ plane using ray casting
        /// </summary>
        /// <param name="point">The point to check</param>
        /// <param name="polygonVertices">The vertices of the polygon</param>
        /// <returns>True if the point is inside the polygon</returns>
        public static bool IsPointInPolygonXZ(Vector3 point, Vector3[] polygonVertices)
        {
            if (polygonVertices.Length < 3)
                return false;

            int intersections = 0;
            for (int i = 0; i < polygonVertices.Length; i++)
            {
                Vector3 vert1 = polygonVertices[i];
                Vector3 vert2 = polygonVertices[(i + 1) % polygonVertices.Length];

                // Consider only the XZ plane
                if (((vert1.z <= point.z && point.z < vert2.z) ||
                     (vert2.z <= point.z && point.z < vert1.z)) &&
                    (point.x < (vert2.x - vert1.x) * (point.z - vert1.z) / (vert2.z - vert1.z) + vert1.x))
                {
                    intersections++;
                }
            }

            // Point is inside if the number of intersections is odd
            return (intersections % 2) == 1;
        }
    }
}