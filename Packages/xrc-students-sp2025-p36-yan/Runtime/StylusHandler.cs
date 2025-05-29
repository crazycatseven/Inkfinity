using UnityEngine;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Stylus input state data.
    /// </summary>
    public struct StylusInputs
    {
        /// <summary>
        /// Tip pressure (0-1).
        /// </summary>
        public float tip_value;
        /// <summary>
        /// Front button pressed.
        /// </summary>
        public bool cluster_front_value;
        /// <summary>
        /// Middle button pressure (0-1).
        /// </summary>
        public float cluster_middle_value;
        /// <summary>
        /// Back button pressed.
        /// </summary>
        public bool cluster_back_value;
        /// <summary>
        /// Back button double tap.
        /// </summary>
        public bool cluster_back_double_tap_value;
        /// <summary>
        /// Any input active.
        /// </summary>
        public bool any;
        /// <summary>
        /// Inking pose.
        /// </summary>
        public Pose inkingPose;
        /// <summary>
        /// Stylus is active.
        /// </summary>
        public bool isActive;
        /// <summary>
        /// Stylus in right hand.
        /// </summary>
        public bool isOnRightHand;
        /// <summary>
        /// Stylus is docked.
        /// </summary>
        public bool docked;
    }

    /// <summary>
    /// Base class for stylus input handling.
    /// </summary>
    public abstract class StylusHandler : MonoBehaviour
    {
        /// <summary>
        /// Current stylus state.
        /// </summary>
        protected StylusInputs stylus;
        /// <summary>
        /// Get current stylus state.
        /// </summary>
        public StylusInputs CurrentState => stylus;
        /// <summary>
        /// Set handedness.
        /// </summary>
        /// <param name="isOnRightHand">True for right hand.</param>
        public void SetHandedness(bool isOnRightHand)
        {
            stylus.isOnRightHand = isOnRightHand;
        }
        /// <summary>
        /// Get tip position.
        /// </summary>
        public abstract Vector3 TipPosition { get; }
        /// <summary>
        /// Get tip direction.
        /// </summary>
        public abstract Vector3 TipDirection { get; }
        /// <summary>
        /// Get tip transform.
        /// </summary>
        public abstract Transform TipTransform { get; }
    }
}
