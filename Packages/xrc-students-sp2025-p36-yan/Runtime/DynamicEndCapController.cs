using UnityEngine;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Controls LineRenderer cap vertices based on point count and segment length.
    /// Hides caps when only two points remain and their distance is below a threshold.
    /// Otherwise, restores the original cap setting.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class DynamicEndCapController : MonoBehaviour
    {
        [Tooltip("Cap vertices when only two points are present and short.")]
        public int CapsWhenLineOnly = 0;

        [Tooltip("Cap vertices when line is longer or has multiple points.")]
        public int CapsWhenMultiPoint = 4;

        [Tooltip("Minimum segment length to keep caps visible.")]
        public float MinDistance = 0.01f;

        private LineRenderer lineRenderer;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.numCapVertices = CapsWhenMultiPoint;
        }

        private void Update()
        {
            int count = lineRenderer.positionCount;
            if (count == 2)
            {
                Vector3[] pts = new Vector3[2];
                lineRenderer.GetPositions(pts);
                float dist = Vector3.Distance(pts[0], pts[1]);
                lineRenderer.numCapVertices = dist < MinDistance
                    ? CapsWhenLineOnly
                    : CapsWhenMultiPoint;
            }
            else
            {
                lineRenderer.numCapVertices = CapsWhenMultiPoint;
            }
        }
    }
}
