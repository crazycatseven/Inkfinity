using System;
using System.Collections.Generic;
using UnityEngine;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Detection result containing confidence metrics and the fitted rectangle corners (XZ plane with averaged Y value)
    /// </summary>
    public class RectangleDetectionResult
    {
        public float FinalConfidence;
        public float BaseConfidence;
        public float AngleFactor;
        public float ClosureFactor;
        public float AdjustedClosureFactor;
        public Vector3[] Corners; // Order: 0-1-2-3 forming a closed rectangle
    }

    /// <summary>
    /// Algorithm for detecting rectangles from strokes. Takes a list of control points and returns detection results.
    /// </summary>
    public static class RectangleDetection
    {
        public static float RightAngleTolerance = 15f;     // Tolerance in degrees for right angle detection
        public static int RequiredRightAngles = 3;         // Minimum number of right angles required
        public static float ClosureWeight = 0.5f;          // Power mapping parameter for closure factor (<1 reduces penalty)
        public static float ClosureInfluence = 0.6f;       // Global influence weight of closure factor, range [0,1]

        /// <summary>
        /// Main detection method, takes stroke points and returns detection results (including fitted rectangle and confidence)
        /// </summary>
        public static RectangleDetectionResult DetectRectangle(List<Vector3> strokePoints)
        {
            if (strokePoints == null || strokePoints.Count < 3)
                return null;

            // 1. Project all points to the XZ plane with averaged Y value
            float sumY = 0f;
            foreach (var p in strokePoints)
                sumY += p.y;
            float avgY = sumY / strokePoints.Count;
            List<Vector3> points = new List<Vector3>();
            foreach (var p in strokePoints)
                points.Add(new Vector3(p.x, avgY, p.z));

            // 2. Find minimum bounding rectangle using convex hull and rotation candidates
            float rectArea;
            Vector3[] bestRectCorners = GetMinimumBoundingBox(points, out rectArea);
            if (bestRectCorners == null || bestRectCorners.Length < 4)
                return null;

            // Make sure the corners are in clockwise order
            bestRectCorners = SortInClockwiseOrder(bestRectCorners);

            // 3. Calculate convex hull area (for base confidence)
            List<Vector3> hull = ComputeConvexHull(points);
            float hullArea = PolygonArea(hull);
            float baseConfidence = hullArea / rectArea;
            if (baseConfidence > 1f)
                baseConfidence = 1f;

            // 4. Calculate closure factor (based on distance between stroke endpoints vs rectangle perimeter)
            float closureFactor = ComputeClosureFactorByLongerSide(points, bestRectCorners);

            // 5. Count approximate right angles in the original point set (on XZ plane)
            int rightAngleCount = CountRightAngles(points, RightAngleTolerance);
            float angleFactor = Mathf.Min(rightAngleCount, RequiredRightAngles) / (float)RequiredRightAngles;

            // 6. Apply power mapping to closure factor (reduces penalty for low closure)
            float adjustedClosure = Mathf.Pow(closureFactor, ClosureWeight);

            // 7. Apply global closure influence parameter
            // When ClosureInfluence==0, closure is ignored; when ==1, fully applied
            float mixedClosure = (1 - ClosureInfluence) + ClosureInfluence * adjustedClosure;

            // 8. Final confidence = baseConfidence * angleFactor * mixedClosure
            float finalConfidence = baseConfidence * angleFactor * mixedClosure;

            // 9. Build and return result
            RectangleDetectionResult result = new RectangleDetectionResult();
            result.BaseConfidence = baseConfidence;
            result.AngleFactor = angleFactor;
            result.ClosureFactor = closureFactor;
            result.AdjustedClosureFactor = adjustedClosure;
            result.FinalConfidence = finalConfidence;
            result.Corners = bestRectCorners;
            return result;
        }

        #region Algorithm Utility Functions

        // Calculate minimum bounding rectangle using rotation candidates, returns the four corners and outputs area
        public static Vector3[] GetMinimumBoundingBox(List<Vector3> points, out float bestArea)
        {
            bestArea = float.MaxValue;
            Vector3[] bestRect = null;
            // Calculate convex hull first
            List<Vector3> hull = ComputeConvexHull(points);
            if (hull == null || hull.Count < 3)
                return null;
            int n = hull.Count;
            for (int i = 0; i < n; i++)
            {
                Vector3 p1 = hull[i];
                Vector3 p2 = hull[(i + 1) % n];
                // Calculate rotation angle (using x and z for XZ plane)
                float angle = Mathf.Atan2(p2.z - p1.z, p2.x - p1.x);
                // Rotate all points
                List<Vector3> rotated = new List<Vector3>();
                foreach (Vector3 p in points)
                {
                    rotated.Add(RotatePointXZ(p, -angle));
                }
                float minX = float.MaxValue, maxX = float.MinValue;
                float minZ = float.MaxValue, maxZ = float.MinValue;
                foreach (Vector3 rp in rotated)
                {
                    if (rp.x < minX) minX = rp.x;
                    if (rp.x > maxX) maxX = rp.x;
                    if (rp.z < minZ) minZ = rp.z;
                    if (rp.z > maxZ) maxZ = rp.z;
                }
                float area = (maxX - minX) * (maxZ - minZ);
                if (area < bestArea)
                {
                    bestArea = area;
                    Vector3[] rect = new Vector3[4];
                    rect[0] = new Vector3(minX, 0, minZ);
                    rect[1] = new Vector3(maxX, 0, minZ);
                    rect[2] = new Vector3(maxX, 0, maxZ);
                    rect[3] = new Vector3(minX, 0, maxZ);
                    // Rotate rectangle corners back to original coordinate system
                    for (int j = 0; j < 4; j++)
                    {
                        rect[j] = RotatePointXZ(rect[j], angle);
                        // Set unified Y value
                        rect[j] = new Vector3(rect[j].x, points[0].y, rect[j].z);
                    }
                    bestRect = rect;
                }
            }
            return bestRect;
        }

        // Rotate a point in the XZ plane (around origin), angle in radians
        public static Vector3 RotatePointXZ(Vector3 p, float angle)
        {
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            float x = p.x * cos - p.z * sin;
            float z = p.x * sin + p.z * cos;
            return new Vector3(x, p.y, z);
        }

        // Compute convex hull (monotone chain algorithm, sorted by x then z), returns hull points
        public static List<Vector3> ComputeConvexHull(List<Vector3> points)
        {
            List<Vector3> pts = new List<Vector3>(points);
            pts.Sort((a, b) =>
            {
                if (a.x == b.x) return a.z.CompareTo(b.z);
                return a.x.CompareTo(b.x);
            });
            List<Vector3> lower = new List<Vector3>();
            foreach (var p in pts)
            {
                while (lower.Count >= 2 && Cross(lower[lower.Count - 2], lower[lower.Count - 1], p) <= 0)
                    lower.RemoveAt(lower.Count - 1);
                lower.Add(p);
            }
            List<Vector3> upper = new List<Vector3>();
            for (int i = pts.Count - 1; i >= 0; i--)
            {
                Vector3 p = pts[i];
                while (upper.Count >= 2 && Cross(upper[upper.Count - 2], upper[upper.Count - 1], p) <= 0)
                    upper.RemoveAt(upper.Count - 1);
                upper.Add(p);
            }
            upper.RemoveAt(upper.Count - 1);
            lower.RemoveAt(lower.Count - 1);
            lower.AddRange(upper);
            return lower;
        }

        // 2D cross product (using X and Z components)
        public static float Cross(Vector3 o, Vector3 a, Vector3 b)
        {
            return (a.x - o.x) * (b.z - o.z) - (a.z - o.z) * (b.x - o.x);
        }

        // Calculate polygon area using Shoelace formula (using XZ components)
        public static float PolygonArea(List<Vector3> polygon)
        {
            float area = 0f;
            int n = polygon.Count;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                area += polygon[i].x * polygon[j].z - polygon[j].x * polygon[i].z;
            }
            return Mathf.Abs(area) / 2f;
        }

        // Calculate closure factor: based on distance between stroke endpoints vs rectangle perimeter
        public static float ComputeClosureFactorByLongerSide(List<Vector3> points, Vector3[] bestRectCorners)
        {
            if (points.Count < 2 || bestRectCorners == null || bestRectCorners.Length < 4)
                return 0f;

            // Calculate endpoint distance
            float gap = Vector3.Distance(points[0], points[points.Count - 1]);

            // Calculate rectangle perimeter
            float rectPerimeter = 0f;
            for (int i = 0; i < 4; i++)
            {
                rectPerimeter += Vector3.Distance(bestRectCorners[i], bestRectCorners[(i + 1) % 4]);
            }

            // Use 1/4 of perimeter as threshold
            float threshold = rectPerimeter / 4f;

            if (gap <= 0f)
                return 1f;
            else if (gap >= threshold)
                return 0f;
            else
                return 1f - gap / threshold;
        }

        // Count approximate right angles in point set (on XZ plane)
        public static int CountRightAngles(List<Vector3> points, float tolerance)
        {
            // Ensure enough points for calculation
            if (points == null || points.Count < 3)
                return 0;

            int count = 0;
            int windowSize = 10; // Sliding window size
            float minDistance = 0.005f; // Minimum distance threshold

            // Adjust window size if too few points
            if (points.Count <= windowSize * 2)
            {
                // Adjust window size to quarter of point count, minimum 1
                windowSize = Mathf.Max(1, points.Count / 4);
            }

            // Store right angle candidates
            List<(int index, float angle)> candidates = new List<(int, float)>();

            // Safety check
            if (points.Count <= windowSize)
            {
                return 0;
            }

            for (int i = 0; i < points.Count; i++)
            {
                try
                {
                    // Get points before and after current
                    int prevIdx = (i - windowSize + points.Count) % points.Count;
                    int nextIdx = (i + windowSize) % points.Count;

                    // Ensure we're not processing the same point
                    if (prevIdx == i || nextIdx == i || prevIdx == nextIdx)
                    {
                        continue;
                    }

                    Vector3 prev = points[prevIdx];
                    Vector3 curr = points[i];
                    Vector3 next = points[nextIdx];

                    // Check if distance is sufficient
                    if (Vector3.Distance(prev, curr) < minDistance || Vector3.Distance(curr, next) < minDistance)
                        continue;

                    // Calculate vectors
                    Vector3 v1 = prev - curr;
                    Vector3 v2 = next - curr;

                    // Calculate angle (on XZ plane)
                    float angle = Vector3.Angle(new Vector3(v1.x, 0, v1.z), new Vector3(v2.x, 0, v2.z));

                    // Check if close to 90 degrees
                    if (Mathf.Abs(angle - 90) <= tolerance)
                    {
                        candidates.Add((i, angle));
                    }
                }
                catch (System.Exception e)
                {
                    // Safe exception handling
                    Debug.LogError($"Error calculating right angle: {e.Message}, Points: {points.Count}, Index: {i}, Window: {windowSize}");
                    continue;
                }
            }

            // Non-maximum suppression
            if (candidates.Count > 0)
            {
                // Sort by angle error (closest to 90 degrees first)
                candidates.Sort((a, b) => Mathf.Abs(a.angle - 90).CompareTo(Mathf.Abs(b.angle - 90)));

                // Select best candidates
                List<int> selectedIndices = new List<int>();
                foreach (var candidate in candidates)
                {
                    bool isTooClose = false;
                    foreach (int selectedIdx in selectedIndices)
                    {
                        // Skip if too close to already selected point
                        if (Mathf.Abs(candidate.index - selectedIdx) < windowSize)
                        {
                            isTooClose = true;
                            break;
                        }
                    }

                    if (!isTooClose)
                    {
                        selectedIndices.Add(candidate.index);
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Sort the rectangle corners in clockwise order based on camera view.
        /// Converts all points to camera space and then classifies them directly.
        /// </summary>
        /// <param name="corners">Original corner points array (4 points)</param>
        /// <returns>Clockwise ordered corner points: top-left, top-right, bottom-right, bottom-left</returns>
        public static Vector3[] SortInClockwiseOrder(Vector3[] corners)
        {
            if (corners == null || corners.Length != 4)
                return corners;

            // Get main camera
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("No main camera found for corner ordering. Using default ordering.");
                return corners;
            }

            // Calculate the center of the rectangle
            Vector3 center = Vector3.zero;
            foreach (var corner in corners)
                center += corner;
            center /= 4;

            // Convert center to viewport position
            Vector3 centerViewport = mainCamera.WorldToViewportPoint(center);

            // Convert all corners to viewport space
            Vector3[] viewportCorners = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                viewportCorners[i] = mainCamera.WorldToViewportPoint(corners[i]);
            }

            // Find left and right sides relative to the center in viewport space
            List<int> leftCornerIndices = new List<int>();
            List<int> rightCornerIndices = new List<int>();

            for (int i = 0; i < 4; i++)
            {
                if (viewportCorners[i].x < centerViewport.x)
                    leftCornerIndices.Add(i);
                else
                    rightCornerIndices.Add(i);
            }

            // If we don't have exactly 2 corners on each side, fall back to a distance-based method
            if (leftCornerIndices.Count != 2 || rightCornerIndices.Count != 2)
            {
                // Handle edge case where corners align with center from camera perspective
                // Sort by distance to center in viewport
                List<(int index, float dist)> cornerDists = new List<(int, float)>();
                for (int i = 0; i < 4; i++)
                {
                    float dist = Vector2.Distance(
                        new Vector2(viewportCorners[i].x, viewportCorners[i].y),
                        new Vector2(centerViewport.x, centerViewport.y));
                    cornerDists.Add((i, dist));
                }

                // Sort by distance
                cornerDists.Sort((a, b) => a.dist.CompareTo(b.dist));

                // Get furthest apart corners first (max distance from center)
                int[] furthestIndices = new int[] { cornerDists[3].index, cornerDists[2].index };

                // Determine if these two are on left or right side
                foreach (int idx in furthestIndices)
                {
                    if (viewportCorners[idx].x < centerViewport.x)
                        leftCornerIndices.Add(idx);
                    else
                        rightCornerIndices.Add(idx);
                }

                // Add remaining corners
                for (int i = 0; i < 4; i++)
                {
                    if (!leftCornerIndices.Contains(i) && !rightCornerIndices.Contains(i))
                    {
                        if (leftCornerIndices.Count < 2)
                            leftCornerIndices.Add(i);
                        else
                            rightCornerIndices.Add(i);
                    }
                }
            }

            // Sort left corners by y (height in viewport)
            int leftTopIndex = leftCornerIndices[0];
            int leftBottomIndex = leftCornerIndices[1];
            if (viewportCorners[leftTopIndex].y < viewportCorners[leftBottomIndex].y)
            {
                int temp = leftTopIndex;
                leftTopIndex = leftBottomIndex;
                leftBottomIndex = temp;
            }

            // Sort right corners by y (height in viewport)
            int rightTopIndex = rightCornerIndices[0];
            int rightBottomIndex = rightCornerIndices[1];
            if (viewportCorners[rightTopIndex].y < viewportCorners[rightBottomIndex].y)
            {
                int temp = rightTopIndex;
                rightTopIndex = rightBottomIndex;
                rightBottomIndex = temp;
            }

            // Create result in clockwise order: top-left, top-right, bottom-right, bottom-left
            Vector3[] result = new Vector3[4];
            result[0] = corners[leftTopIndex];     // top-left
            result[1] = corners[rightTopIndex];    // top-right
            result[2] = corners[rightBottomIndex]; // bottom-right
            result[3] = corners[leftBottomIndex];  // bottom-left

            // Final adjustment: Check rectangle orientation relative to camera's right direction
            // If the rectangle is more perpendicular to the camera view, we need to rotate the order
            Vector3 cameraRight = mainCamera.transform.right;

            // Calculate the two primary direction vectors of the rectangle
            Vector3 horizontalDir = (result[1] - result[0]).normalized; // Top edge direction (top-left -> top-right)
            Vector3 verticalDir = (result[3] - result[0]).normalized;   // Left edge direction (top-left -> bottom-left)

            // Calculate dot products to determine alignment with camera's right direction
            float horizontalAlignment = Mathf.Abs(Vector3.Dot(horizontalDir, cameraRight));
            float verticalAlignment = Mathf.Abs(Vector3.Dot(verticalDir, cameraRight));

            // If the vertical edge is more aligned with camera's right direction, rotate the order
            if (verticalAlignment > horizontalAlignment)
            {
                Vector3[] rotatedResult = new Vector3[4];
                rotatedResult[0] = result[3]; // Former bottom-left becomes top-left
                rotatedResult[1] = result[0]; // Former top-left becomes top-right
                rotatedResult[2] = result[1]; // Former top-right becomes bottom-right
                rotatedResult[3] = result[2]; // Former bottom-right becomes bottom-left
                return rotatedResult;
            }

            return result;
        }

        #endregion
    }
}

