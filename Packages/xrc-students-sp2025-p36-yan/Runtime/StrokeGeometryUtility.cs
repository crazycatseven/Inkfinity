using UnityEngine;
using System.Collections.Generic;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Stroke geometry calculation utilities
    /// </summary>
    public static class StrokeGeometryUtility
    {
        /// <summary>
        /// Calculate average position of a stroke
        /// </summary>
        public static Vector3 CalculateAveragePosition(Stroke stroke)
        {
            if (stroke == null || stroke.Points.Count == 0)
            {
                return Vector3.zero;
            }

            Vector3 average = Vector3.zero;
            foreach (var point in stroke.Points)
            {
                average += point;
            }

            return average / stroke.Points.Count;
        }

        /// <summary>
        /// Calculate bounds of a stroke
        /// </summary>
        public static Bounds CalculateStrokeBounds(Stroke stroke)
        {
            if (stroke == null || stroke.Points.Count == 0)
                return new Bounds(Vector3.zero, Vector3.zero);

            Vector3 center = Vector3.zero;
            foreach (var point in stroke.Points)
            {
                center += point;
            }
            center /= stroke.Points.Count;

            Bounds bounds = new Bounds(center, Vector3.zero);
            foreach (var point in stroke.Points)
            {
                bounds.Encapsulate(point);
            }

            bounds.Expand(0.3f);
            return bounds;
        }

        /// <summary>
        /// Get combined bounds of multiple strokes
        /// </summary>
        public static Bounds GetCombinedBounds(List<Stroke> strokes)
        {
            Bounds combinedBounds = new Bounds();
            bool first = true;

            foreach (var stroke in strokes)
            {
                Bounds strokeBounds = CalculateStrokeBounds(stroke);
                if (first)
                {
                    combinedBounds = strokeBounds;
                    first = false;
                }
                else
                {
                    combinedBounds.Encapsulate(strokeBounds);
                }
            }

            return combinedBounds;
        }

        /// <summary>
        /// Convert 3D bounds to screen rectangle
        /// </summary>
        public static Rect GetScreenRectFromBounds(Bounds bounds, Camera cam, int texWidth, int texHeight, float padding)
        {
            Vector2 min = cam.WorldToScreenPoint(bounds.min);
            Vector2 max = cam.WorldToScreenPoint(bounds.max);

            min -= new Vector2(padding, padding);
            max += new Vector2(padding, padding);

            float scaleX = (float)texWidth / Screen.width;
            float scaleY = (float)texHeight / Screen.height;

            return new Rect(
                min.x * scaleX,
                min.y * scaleY,
                (max.x - min.x) * scaleX,
                (max.y - min.y) * scaleY
            );
        }

        /// <summary>
        /// Calculate crop rectangle from stroke
        /// </summary>
        public static Rect CalculateCropRectFromStroke(Stroke stroke, int textureWidth, int textureHeight, Camera cam = null)
        {
            if (cam == null)
            {
                cam = Camera.main;
            }

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            foreach (var point in stroke.Points)
            {
                Vector2 screenPoint = cam.WorldToScreenPoint(point);
                min = Vector2.Min(min, screenPoint);
                max = Vector2.Max(max, screenPoint);
            }

            float padding = 50f;
            min -= new Vector2(padding, padding);
            max += new Vector2(padding, padding);

            float scaleX = (float)textureWidth / Screen.width;
            float scaleY = (float)textureHeight / Screen.height;

            return new Rect(
                min.x * scaleX,
                min.y * scaleY,
                (max.x - min.x) * scaleX,
                (max.y - min.y) * scaleY
            );
        }
    }
}
