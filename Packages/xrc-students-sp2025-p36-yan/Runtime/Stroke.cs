using UnityEngine;
using System.Collections.Generic;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Stroke data container.
    /// </summary>
    [System.Serializable]
    public class Stroke
    {
        /// <summary>
        /// Mesh object.
        /// </summary>
        public GameObject MeshObject { get; private set; }
        /// <summary>
        /// Stroke points.
        /// </summary>
        public List<Vector3> Points { get; private set; }
        /// <summary>
        /// Widths at each point.
        /// </summary>
        public List<float> Widths { get; private set; }
        /// <summary>
        /// Bounding box min X.
        /// </summary>
        public float MinX { get; private set; }
        /// <summary>
        /// Bounding box min Y.
        /// </summary>
        public float MinY { get; private set; }
        /// <summary>
        /// Bounding box min Z.
        /// </summary>
        public float MinZ { get; private set; }
        /// <summary>
        /// Bounding box max X.
        /// </summary>
        public float MaxX { get; private set; }
        /// <summary>
        /// Bounding box max Y.
        /// </summary>
        public float MaxY { get; private set; }
        /// <summary>
        /// Bounding box max Z.
        /// </summary>
        public float MaxZ { get; private set; }
        /// <summary>
        /// Center point.
        /// </summary>
        public Vector3 Center { get; private set; }
        /// <summary>
        /// Create stroke.
        /// </summary>
        /// <param name="meshObject">Mesh object.</param>
        /// <param name="points">Stroke points.</param>
        /// <param name="widths">Widths.</param>
        public Stroke(GameObject meshObject, List<Vector3> points, List<float> widths)
        {
            MeshObject = meshObject;
            Points = new List<Vector3>(points);
            Widths = new List<float>(widths);
            CalculateBoundingBox();
        }
        /// <summary>
        /// Flatten stroke to height.
        /// </summary>
        /// <param name="targetY">Target Y.</param>
        /// <returns>Flattened height.</returns>
        public float Flatten(float? targetY = null)
        {
            if (targetY == null)
            {
                float averageY = 0f;
                foreach (Vector3 point in Points)
                {
                    averageY += point.y;
                }
                averageY /= Points.Count;
                targetY = averageY;
            }
            for (int i = 0; i < Points.Count; i++)
            {
                Vector3 point = Points[i];
                point.y = targetY.Value;
                Points[i] = point;
            }
            MeshFilter meshFilter = MeshObject.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.mesh != null)
            {
                Vector3[] vertices = meshFilter.mesh.vertices;
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i].y = targetY.Value;
                }
                meshFilter.mesh.vertices = vertices;
                meshFilter.mesh.RecalculateBounds();
                meshFilter.mesh.RecalculateNormals();
            }
            CalculateBoundingBox();
            return targetY.Value;
        }
        /// <summary>
        /// Calculate bounding box.
        /// </summary>
        private void CalculateBoundingBox()
        {
            if (Points == null || Points.Count == 0)
            {
                MinX = MinY = MinZ = 0;
                MaxX = MaxY = MaxZ = 0;
                Center = Vector3.zero;
                return;
            }
            Vector3 firstPoint = Points[0];
            Center = Vector3.zero;
            MinX = MaxX = firstPoint.x;
            MinY = MaxY = firstPoint.y;
            MinZ = MaxZ = firstPoint.z;
            foreach (Vector3 point in Points)
            {
                if (point.x < MinX) MinX = point.x;
                if (point.x > MaxX) MaxX = point.x;
                if (point.y < MinY) MinY = point.y;
                if (point.y > MaxY) MaxY = point.y;
                if (point.z < MinZ) MinZ = point.z;
                if (point.z > MaxZ) MaxZ = point.z;
                Center += point;
            }
            float maxWidth = 0.001f;
            if (Widths != null && Widths.Count > 0)
            {
                foreach (float width in Widths)
                {
                    maxWidth = Mathf.Max(maxWidth, width);
                }
            }
            MinX -= maxWidth;
            MinY -= maxWidth;
            MinZ -= maxWidth;
            MaxX += maxWidth;
            MaxY += maxWidth;
            MaxZ += maxWidth;
            Center /= Points.Count;
        }
    }
}