using System.Collections.Generic;
using UnityEngine;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Enum defining hand bone indices
    /// </summary>
    public enum XRHandBoneIndex
    {
        XRHand_Start = 0,
        XRHand_Palm = 0,
        XRHand_Wrist = 1,
        XRHand_ThumbMetacarpal = 2,
        XRHand_ThumbProximal = 3,
        XRHand_ThumbDistal = 4,
        XRHand_ThumbTip = 5,
        XRHand_IndexMetacarpal = 6,
        XRHand_IndexProximal = 7,
        XRHand_IndexIntermediate = 8,
        XRHand_IndexDistal = 9,
        XRHand_IndexTip = 10,
        XRHand_MiddleMetacarpal = 11,
        XRHand_MiddleProximal = 12,
        XRHand_MiddleIntermediate = 13,
        XRHand_MiddleDistal = 14,
        XRHand_MiddleTip = 15,
        XRHand_RingMetacarpal = 16,
        XRHand_RingProximal = 17,
        XRHand_RingIntermediate = 18,
        XRHand_RingDistal = 19,
        XRHand_RingTip = 20,
        XRHand_LittleMetacarpal = 21,
        XRHand_LittleProximal = 22,
        XRHand_LittleIntermediate = 23,
        XRHand_LittleDistal = 24,
        XRHand_LittleTip = 25,
        XRHand_Max = 26,
        XRHand_End = 26
    }

    /// <summary>
    /// Detects and analyzes hand gestures using OVRSkeleton data.
    /// </summary>
    public class GestureDetector : MonoBehaviour
    {
        public static GestureDetector Instance { get; private set; }

        [Header("Hand Settings")]
        [SerializeField] private OVRSkeleton leftHandSkeleton;
        [SerializeField] private OVRSkeleton rightHandSkeleton;
        [SerializeField] private Camera referenceCamera;

        [Header("Calculation Parameters")]
        [SerializeField] public float bendingThreshold = 0.3f;
        [SerializeField] public float extendedThreshold = 0.7f;
        [SerializeField] private float thumbMaxOpenAngle = 45f;

        [Header("Palm Orientation Detection")]
        [SerializeField] private bool detectPalmOrientation = true;
        [SerializeField] private float orientationThreshold = 0.7f;

        private Vector3[] leftJointPositions;
        private Vector3[] rightJointPositions;
        private HandData leftHandData = new HandData();
        private HandData rightHandData = new HandData();

        private Vector3 camForward, camUp, camRight;
        private bool cameraDirectionsInitialized = false;

        public enum GesturePalmOrientation
        {
            Forward,
            Backward,
            Upward,
            Downward,
            Left,
            Right,
            Other
        }

        public class HandData
        {
            // Finger extension values (0-1)
            public float thumbExtension;
            public float indexExtension;
            public float middleExtension;
            public float ringExtension;
            public float littleExtension;

            // Orientation data
            public Vector3 palmNormal = Vector3.zero;
            public Vector3 palmForward = Vector3.zero;
            public Vector3 indexDirection = Vector3.zero;
            public GesturePalmOrientation palmOrientation = GesturePalmOrientation.Other;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (leftHandSkeleton == null || rightHandSkeleton == null)
            {
                FindOVRSkeletons();
            }

            if (referenceCamera == null)
            {
                FindReferenceCamera();
            }

            leftJointPositions = new Vector3[(int)XRHandBoneIndex.XRHand_End];
            rightJointPositions = new Vector3[(int)XRHandBoneIndex.XRHand_End];
        }

        private void FindOVRSkeletons()
        {
            OVRSkeleton[] skeletons = FindObjectsByType<OVRSkeleton>(FindObjectsSortMode.None);
            foreach (OVRSkeleton skeleton in skeletons)
            {
                OVRHand hand = skeleton.GetComponent<OVRHand>();
                if (hand == null) continue;

                bool isRightHand = false;

                if (hand.GetType().GetProperty("Handedness") != null)
                {
                    var handedness = hand.GetType().GetProperty("Handedness").GetValue(hand);
                    isRightHand = handedness.ToString().Contains("Right");
                }
                else if (hand.GetType().GetProperty("IsRightHand") != null)
                {
                    isRightHand = (bool)hand.GetType().GetProperty("IsRightHand").GetValue(hand);
                }
                else
                {
                    isRightHand = hand.gameObject.name.ToLower().Contains("right");
                }

                if (isRightHand && rightHandSkeleton == null)
                {
                    rightHandSkeleton = skeleton;
                    Debug.Log("Found right hand OVRSkeleton: " + skeleton.gameObject.name);
                }
                else if (!isRightHand && leftHandSkeleton == null)
                {
                    leftHandSkeleton = skeleton;
                    Debug.Log("Found left hand OVRSkeleton: " + skeleton.gameObject.name);
                }

                if (leftHandSkeleton != null && rightHandSkeleton != null) break;
            }

            if (leftHandSkeleton == null) Debug.LogWarning("Left hand OVRSkeleton not found");
            if (rightHandSkeleton == null) Debug.LogWarning("Right hand OVRSkeleton not found");
        }

        private void FindReferenceCamera()
        {
            GameObject centerEyeAnchor = GameObject.Find("CenterEyeAnchor");
            if (centerEyeAnchor != null && centerEyeAnchor.TryGetComponent<Camera>(out var camera))
            {
                referenceCamera = camera;
                Debug.Log("Found VR camera: CenterEyeAnchor");
                return;
            }

            OVRCameraRig ovrCameraRig = FindFirstObjectByType<OVRCameraRig>();
            if (ovrCameraRig != null && ovrCameraRig.centerEyeAnchor != null &&
                ovrCameraRig.centerEyeAnchor.TryGetComponent<Camera>(out var rigCamera))
            {
                referenceCamera = rigCamera;
                Debug.Log("Found VR camera: OVRCameraRig.centerEyeAnchor");
                return;
            }

            referenceCamera = Camera.main;
            if (referenceCamera != null)
            {
                Debug.Log("Using main camera as reference");
            }
            else
            {
                Debug.LogWarning("Reference camera not found, palm orientation will use world coordinate system");
            }
        }

        private void UpdateCameraDirections()
        {
            if (referenceCamera != null)
            {
                camForward = referenceCamera.transform.forward;
                camUp = referenceCamera.transform.up;
                camRight = referenceCamera.transform.right;
            }
            else
            {
                camForward = Vector3.forward;
                camUp = Vector3.up;
                camRight = Vector3.right;
            }
            cameraDirectionsInitialized = true;
        }

        private void Update()
        {
            if (!cameraDirectionsInitialized)
            {
                UpdateCameraDirections();
            }

            UpdateHandData(leftHandSkeleton, leftJointPositions, leftHandData, false);
            UpdateHandData(rightHandSkeleton, rightJointPositions, rightHandData, true);
        }

        /// <summary>
        /// Updates all hand tracking data for a single hand
        /// </summary>
        private void UpdateHandData(OVRSkeleton skeleton, Vector3[] jointPositions, HandData handData, bool isRightHand)
        {
            if (skeleton == null || skeleton.Bones == null || skeleton.Bones.Count == 0) return;

            // Update joint positions
            foreach (var bone in skeleton.Bones)
            {
                int id = (int)bone.Id;
                if (id < jointPositions.Length)
                {
                    jointPositions[id] = bone.Transform.position;
                }
            }

            if (detectPalmOrientation)
            {
                CalculatePalmOrientation(jointPositions, handData, isRightHand);
            }

            CalculateFingerExtensions(jointPositions, handData, isRightHand);
        }

        private void CalculatePalmOrientation(Vector3[] jointPositions, HandData handData, bool isRightHand)
        {
            Vector3 wristPos = jointPositions[(int)XRHandBoneIndex.XRHand_Wrist];
            Vector3 palmPos = jointPositions[(int)XRHandBoneIndex.XRHand_Palm];
            Vector3 indexKnucklePos = jointPositions[(int)XRHandBoneIndex.XRHand_IndexProximal];
            Vector3 middleKnucklePos = jointPositions[(int)XRHandBoneIndex.XRHand_MiddleProximal];
            Vector3 ringKnucklePos = jointPositions[(int)XRHandBoneIndex.XRHand_RingProximal];
            Vector3 indexTipPos = jointPositions[(int)XRHandBoneIndex.XRHand_IndexTip];

            if (wristPos == Vector3.zero || palmPos == Vector3.zero || indexKnucklePos == Vector3.zero ||
                middleKnucklePos == Vector3.zero || ringKnucklePos == Vector3.zero || indexTipPos == Vector3.zero)
            {
                return;
            }

            Vector3 palmToIndex = indexKnucklePos - palmPos;
            Vector3 palmToRing = ringKnucklePos - palmPos;

            handData.palmNormal = isRightHand ?
                Vector3.Cross(palmToIndex, palmToRing).normalized :
                Vector3.Cross(palmToRing, palmToIndex).normalized;

            handData.palmForward = (middleKnucklePos - wristPos).normalized;
            handData.indexDirection = (indexTipPos - indexKnucklePos).normalized;

            float[] dotProducts = new float[6];
            dotProducts[0] = Vector3.Dot(handData.palmNormal, -camForward); // Forward
            dotProducts[1] = Vector3.Dot(handData.palmNormal, camForward);  // Backward
            dotProducts[2] = Vector3.Dot(handData.palmNormal, -camUp);      // Upward
            dotProducts[3] = Vector3.Dot(handData.palmNormal, camUp);       // Downward
            dotProducts[4] = Vector3.Dot(handData.palmNormal, -camRight);   // Left
            dotProducts[5] = Vector3.Dot(handData.palmNormal, camRight);    // Right

            float maxDot = 0;
            int maxDotIndex = -1;

            for (int i = 0; i < dotProducts.Length; i++)
            {
                if (dotProducts[i] > maxDot)
                {
                    maxDot = dotProducts[i];
                    maxDotIndex = i;
                }
            }

            if (maxDot < orientationThreshold)
            {
                handData.palmOrientation = GesturePalmOrientation.Other;
            }
            else
            {
                handData.palmOrientation = (GesturePalmOrientation)maxDotIndex;
            }
        }

        private void CalculateFingerExtensions(Vector3[] jointPositions, HandData handData, bool isRightHand)
        {
            CalculateThumbExtension(jointPositions, handData, isRightHand);

            handData.indexExtension = CalculateFingerExtension(
                jointPositions,
                XRHandBoneIndex.XRHand_IndexMetacarpal,
                XRHandBoneIndex.XRHand_IndexProximal,
                XRHandBoneIndex.XRHand_IndexIntermediate,
                XRHandBoneIndex.XRHand_IndexDistal
            );

            handData.middleExtension = CalculateFingerExtension(
                jointPositions,
                XRHandBoneIndex.XRHand_MiddleMetacarpal,
                XRHandBoneIndex.XRHand_MiddleProximal,
                XRHandBoneIndex.XRHand_MiddleIntermediate,
                XRHandBoneIndex.XRHand_MiddleDistal
            );

            handData.ringExtension = CalculateFingerExtension(
                jointPositions,
                XRHandBoneIndex.XRHand_RingMetacarpal,
                XRHandBoneIndex.XRHand_RingProximal,
                XRHandBoneIndex.XRHand_RingIntermediate,
                XRHandBoneIndex.XRHand_RingDistal
            );

            handData.littleExtension = CalculateFingerExtension(
                jointPositions,
                XRHandBoneIndex.XRHand_LittleMetacarpal,
                XRHandBoneIndex.XRHand_LittleProximal,
                XRHandBoneIndex.XRHand_LittleIntermediate,
                XRHandBoneIndex.XRHand_LittleDistal
            );
        }

        private float CalculateFingerExtension(
            Vector3[] jointPositions,
            XRHandBoneIndex metacarpalIndex,
            XRHandBoneIndex proximalIndex,
            XRHandBoneIndex intermediateIndex,
            XRHandBoneIndex distalIndex)
        {
            Vector3 metacarpalPos = jointPositions[(int)metacarpalIndex];
            Vector3 proximalPos = jointPositions[(int)proximalIndex];
            Vector3 intermediatePos = jointPositions[(int)intermediateIndex];
            Vector3 distalPos = jointPositions[(int)distalIndex];

            if (metacarpalPos == Vector3.zero || proximalPos == Vector3.zero ||
                intermediatePos == Vector3.zero || distalPos == Vector3.zero)
            {
                return 0f;
            }

            Vector3 dir1 = (proximalPos - metacarpalPos).normalized;
            Vector3 dir2 = (intermediatePos - proximalPos).normalized;
            Vector3 dir3 = (distalPos - intermediatePos).normalized;

            float angle = (Vector3.Angle(dir1, dir2) + Vector3.Angle(dir2, dir3)) * 0.5f;

            return 1.0f - Mathf.Clamp01(angle / 90f);
        }

        private void CalculateThumbExtension(Vector3[] jointPositions, HandData handData, bool isRightHand)
        {
            Vector3 thumbProximalPos = jointPositions[(int)XRHandBoneIndex.XRHand_ThumbProximal];
            Vector3 thumbDistalPos = jointPositions[(int)XRHandBoneIndex.XRHand_ThumbDistal];

            if (thumbProximalPos == Vector3.zero || thumbDistalPos == Vector3.zero || handData.palmForward == Vector3.zero)
            {
                handData.thumbExtension = 0f;
                return;
            }

            Vector3 thumbVector = (thumbDistalPos - thumbProximalPos).normalized;
            float signedAngle = Vector3.SignedAngle(thumbVector, handData.palmForward, handData.palmNormal);

            if (!isRightHand)
            {
                signedAngle = -signedAngle;
            }

            handData.thumbExtension = signedAngle < 0f ? 0f : Mathf.Clamp01(signedAngle / thumbMaxOpenAngle);
        }

        #region Public API

        /// <summary>
        /// Gets the palm orientation for the specified hand
        /// </summary>
        /// <param name="isRightHand">Whether to get data for right hand (true) or left hand (false)</param>
        /// <returns>The palm orientation enum value</returns>
        public GesturePalmOrientation GetPalmOrientation(bool isRightHand = true)
        {
            return isRightHand ? rightHandData.palmOrientation : leftHandData.palmOrientation;
        }

        /// <summary>
        /// Gets the extension value for a specific finger
        /// </summary>
        /// <param name="fingerIndex">Index of finger (0=thumb, 1=index, 2=middle, 3=ring, 4=little)</param>
        /// <param name="isRightHand">Whether to get data for right hand (true) or left hand (false)</param>
        /// <returns>Extension value between 0-1 (0=fully bent, 1=fully extended)</returns>
        public float GetFingerExtension(int fingerIndex, bool isRightHand = true)
        {
            HandData handData = isRightHand ? rightHandData : leftHandData;

            switch (fingerIndex)
            {
                case 0: return handData.thumbExtension;
                case 1: return handData.indexExtension;
                case 2: return handData.middleExtension;
                case 3: return handData.ringExtension;
                case 4: return handData.littleExtension;
                default: return 0f;
            }
        }

        /// <summary>
        /// Checks if a specific gesture with palm orientation is being performed
        /// </summary>
        /// <param name="thumb">Whether thumb should be extended</param>
        /// <param name="index">Whether index finger should be extended</param>
        /// <param name="middle">Whether middle finger should be extended</param>
        /// <param name="ring">Whether ring finger should be extended</param>
        /// <param name="little">Whether little finger should be extended</param>
        /// <param name="orientation">Required palm orientation</param>
        /// <param name="isRightHand">Whether to check right hand (true) or left hand (false)</param>
        /// <returns>True if the gesture matches, false otherwise</returns>
        public bool CheckGestureWithOrientation(bool thumb, bool index, bool middle, bool ring, bool little,
                                              GesturePalmOrientation orientation, bool isRightHand = true)
        {
            HandData handData = isRightHand ? rightHandData : leftHandData;

            bool thumbMatch = thumb ? (handData.thumbExtension > extendedThreshold) : (handData.thumbExtension < bendingThreshold);
            bool indexMatch = index ? (handData.indexExtension > extendedThreshold) : (handData.indexExtension < bendingThreshold);
            bool middleMatch = middle ? (handData.middleExtension > extendedThreshold) : (handData.middleExtension < bendingThreshold);
            bool ringMatch = ring ? (handData.ringExtension > extendedThreshold) : (handData.ringExtension < bendingThreshold);
            bool littleMatch = little ? (handData.littleExtension > extendedThreshold) : (handData.littleExtension < bendingThreshold);
            bool orientationMatch = !detectPalmOrientation || orientation == GesturePalmOrientation.Other || handData.palmOrientation == orientation;

            return thumbMatch && indexMatch && middleMatch && ringMatch && littleMatch && orientationMatch;
        }

        /// <summary>
        /// Checks if the index finger is pointing toward a specific direction
        /// </summary>
        /// <param name="direction">The target direction to check against</param>
        /// <param name="isRightHand">Whether to check right hand (true) or left hand (false)</param>
        /// <returns>True if the index finger is extended and pointing towards the specified direction</returns>
        public bool IsIndexPointingTowards(Vector3 direction, bool isRightHand = true)
        {
            HandData handData = isRightHand ? rightHandData : leftHandData;

            if (!(handData.indexExtension > extendedThreshold)) return false;
            float dotProduct = Vector3.Dot(handData.indexDirection.normalized, direction.normalized);
            return dotProduct > orientationThreshold;
        }

        /// <summary>
        /// Gets the palm center position for the specified hand
        /// </summary>
        /// <param name="isRightHand">Whether to get data for right hand (true) or left hand (false)</param>
        /// <returns>The position of the palm center in world space</returns>
        public Vector3 GetPalmCenter(bool isRightHand = true)
        {
            Vector3[] positions = isRightHand ? rightJointPositions : leftJointPositions;
            return positions[(int)XRHandBoneIndex.XRHand_Palm];
        }

        /// <summary>
        /// Gets the palm normal vector for the specified hand
        /// </summary>
        /// <param name="isRightHand">Whether to get data for right hand (true) or left hand (false)</param>
        /// <returns>The normalized vector perpendicular to palm surface</returns>
        public Vector3 GetPalmNormal(bool isRightHand = true)
        {
            return isRightHand ? rightHandData.palmNormal : leftHandData.palmNormal;
        }

        /// <summary>
        /// Gets the palm forward direction for the specified hand
        /// </summary>
        /// <param name="isRightHand">Whether to get data for right hand (true) or left hand (false)</param>
        /// <returns>The normalized vector pointing forward from the palm</returns>
        public Vector3 GetPalmForward(bool isRightHand = true)
        {
            return isRightHand ? rightHandData.palmForward : leftHandData.palmForward;
        }

        /// <summary>
        /// Gets the left hand data structure
        /// </summary>
        /// <returns>Reference to the left hand data structure</returns>
        public HandData GetLeftHandData()
        {
            return leftHandData;
        }

        /// <summary>
        /// Gets the right hand data structure
        /// </summary>
        /// <returns>Reference to the right hand data structure</returns>
        public HandData GetRightHandData()
        {
            return rightHandData;
        }

        /// <summary>
        /// Gets the array of left hand joint positions
        /// </summary>
        /// <returns>Array of joint positions for the left hand</returns>
        public Vector3[] GetLeftJointPositions()
        {
            return leftJointPositions;
        }

        /// <summary>
        /// Gets the array of right hand joint positions
        /// </summary>
        /// <returns>Array of joint positions for the right hand</returns>
        public Vector3[] GetRightJointPositions()
        {
            return rightJointPositions;
        }

        #endregion
    }
}
