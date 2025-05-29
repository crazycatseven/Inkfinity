using TMPro;
using UnityEngine;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Debug display for hand gesture data
    /// </summary>
    public class GestureDebugger : MonoBehaviour
    {
        [Header("Debug Text Display")]
        [SerializeField] private TextMeshPro leftHandDebugText;
        [SerializeField] private TextMeshPro rightHandDebugText;

        [Header("Display Options")]
        [SerializeField] private bool showDebugVectors = false;
        [SerializeField] private bool showFingerExtensions = true;
        [SerializeField] private bool showGestureAnalysis = true;

        private void Update()
        {
            if (GestureDetector.Instance == null)
                return;

            var detector = GestureDetector.Instance;

            if (leftHandDebugText != null)
            {
                string leftDisplayText = GenerateHandDebugText(detector, false);
                leftHandDebugText.text = leftDisplayText;
            }

            if (rightHandDebugText != null)
            {
                string rightDisplayText = GenerateHandDebugText(detector, true);
                rightHandDebugText.text = rightDisplayText;
            }
        }

        private string GenerateHandDebugText(GestureDetector detector, bool isRightHand)
        {
            string handName = isRightHand ? "Right Hand" : "Left Hand";
            GestureDetector.HandData handData = isRightHand ? detector.GetRightHandData() : detector.GetLeftHandData();

            string displayText = $"Gesture Debug ({handName})\n";
            displayText += "------------------------\n";

            if (showFingerExtensions)
            {
                displayText += $"Thumb: {handData.thumbExtension:F2}\n";
                displayText += $"Index: {handData.indexExtension:F2}\n";
                displayText += $"Middle: {handData.middleExtension:F2}\n";
                displayText += $"Ring: {handData.ringExtension:F2}\n";
                displayText += $"Little: {handData.littleExtension:F2}\n";
                displayText += "------------------------\n";
            }

            displayText += $"Palm Orientation: {handData.palmOrientation}\n";

            if (showDebugVectors)
            {
                displayText += $"Palm Normal: {FormatVector3(detector.GetPalmNormal(isRightHand))}\n";
                displayText += $"Palm Forward: {FormatVector3(detector.GetPalmForward(isRightHand))}\n";
                displayText += $"Index Direction: {FormatVector3(handData.indexDirection)}\n";
            }

            if (showGestureAnalysis)
            {
                string gestureName = AnalyzeGesture(detector, handData);
                displayText += "------------------------\n" + gestureName;
            }

            return displayText;
        }

        private string FormatVector3(Vector3 vector)
        {
            return $"({vector.x:F1}, {vector.y:F1}, {vector.z:F1})";
        }

        /// <summary>
        /// Analyzes hand data to determine the current gesture
        /// </summary>
        private string AnalyzeGesture(GestureDetector detector, GestureDetector.HandData handData)
        {
            bool allExtended = handData.thumbExtension > detector.extendedThreshold &&
                               handData.indexExtension > detector.extendedThreshold &&
                               handData.middleExtension > detector.extendedThreshold &&
                               handData.ringExtension > detector.extendedThreshold &&
                               handData.littleExtension > detector.extendedThreshold;

            bool allCurled = handData.thumbExtension < detector.bendingThreshold &&
                             handData.indexExtension < detector.bendingThreshold &&
                             handData.middleExtension < detector.bendingThreshold &&
                             handData.ringExtension < detector.bendingThreshold &&
                             handData.littleExtension < detector.bendingThreshold;

            bool indexPointingOnly = handData.indexExtension > detector.extendedThreshold &&
                                    handData.thumbExtension < detector.bendingThreshold &&
                                    handData.middleExtension < detector.bendingThreshold &&
                                    handData.ringExtension < detector.bendingThreshold &&
                                    handData.littleExtension < detector.bendingThreshold;

            bool thumbsUp = handData.thumbExtension > detector.extendedThreshold &&
                            handData.indexExtension < detector.bendingThreshold &&
                            handData.middleExtension < detector.bendingThreshold &&
                            handData.ringExtension < detector.bendingThreshold &&
                            handData.littleExtension < detector.bendingThreshold;

            string gestureName = "Unknown Gesture";

            if (allExtended)
            {
                gestureName = "Open Hand";
                gestureName += $" ({handData.palmOrientation})";
            }
            else if (allCurled)
            {
                gestureName = "Fist";
            }
            else if (indexPointingOnly)
            {
                gestureName = "Index Pointing";
            }
            else if (thumbsUp && handData.palmOrientation == GestureDetector.GesturePalmOrientation.Left)
            {
                gestureName = "Thumbs Up";
            }

            return gestureName;
        }
    }
}
