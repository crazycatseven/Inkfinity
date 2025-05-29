using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Implementation of StylusHandler for MX Ink stylus devices
    /// </summary>
    public class MxInkHandler : StylusHandler
    {
        /// <summary>
        /// Container for button events that can be subscribed to
        /// </summary>
        [Serializable]
        public class ButtonEvents
        {
            /// <summary>
            /// Triggered when front button is pressed
            /// </summary>
            public Action OnFrontButtonPressed;

            /// <summary>
            /// Triggered when front button is released
            /// </summary>
            public Action OnFrontButtonReleased;

            /// <summary>
            /// Triggered when side button is pressed
            /// </summary>
            public Action OnSideButtonPressed;

            /// <summary>
            /// Triggered when side button is released
            /// </summary>
            public Action OnSideButtonReleased;

            /// <summary>
            /// Triggered when pressure level changes
            /// </summary>
            public Action<float> OnPressureChanged;

            /// <summary>
            /// Triggered when tip pressure level changes
            /// </summary>
            public Action<float> OnTipPressureChanged;
        }

        [Header("Visual Feedback")]
        /// <summary>
        /// Color to display when button is active
        /// </summary>
        [Tooltip("Color to display when button is active")]
        public Color ActiveColor = Color.gray;

        /// <summary>
        /// Color to display when double tap is detected
        /// </summary>
        [Tooltip("Color to display when double tap is detected")]
        public Color DoubleTapActiveColor = Color.cyan;

        /// <summary>
        /// Default color for inactive buttons
        /// </summary>
        [Tooltip("Default color for inactive buttons")]
        public Color DefaultColor = Color.black;

        [Header("Input Actions")]
        /// <summary>
        /// Input action reference for the stylus tip
        /// </summary>
        [SerializeField, Tooltip("Input action for the stylus tip")]
        private InputActionReference tipActionRef;

        /// <summary>
        /// Input action reference for the grab button
        /// </summary>
        [SerializeField, Tooltip("Input action for the grab button")]
        private InputActionReference grabActionRef;

        /// <summary>
        /// Input action reference for the option button
        /// </summary>
        [SerializeField, Tooltip("Input action for the option button")]
        private InputActionReference optionActionRef;

        /// <summary>
        /// Input action reference for the middle button
        /// </summary>
        [SerializeField, Tooltip("Input action for the middle button")]
        private InputActionReference middleActionRef;

        [Header("Haptic Feedback")]
        [SerializeField, Range(0.001f, 0.1f), Tooltip("Duration of haptic click in seconds")]
        private float hapticClickDuration = 0.011f;

        [SerializeField, Range(0f, 1f), Tooltip("Amplitude of haptic click (0-1)")]
        private float hapticClickAmplitude = 1.0f;

        [Header("Stylus Components")]
        [SerializeField, Tooltip("The tip component of the stylus")]
        private GameObject tip;

        [SerializeField, Tooltip("The front cluster component of the stylus")]
        private GameObject clusterFront;

        [SerializeField, Tooltip("The middle cluster component of the stylus")]
        private GameObject clusterMiddle;

        [SerializeField, Tooltip("The back cluster component of the stylus")]
        private GameObject clusterBack;

        // State tracking variables
        private bool isFrontButtonPressed = false;
        private bool wasFrontButtonPressed = false;
        private bool isSideButtonPressed = false;
        private bool wasSideButtonPressed = false;
        private float currentPressure = 0f;
        private float lastPressure = 0f;
        private float currentTipPressure = 0f;
        private float lastTipPressure = 0f;
        private float pressureThreshold = 0.01f;

        /// <summary>
        /// Events that can be subscribed to for button state changes
        /// </summary>
        public ButtonEvents buttonEvents = new ButtonEvents();

        /// <summary>
        /// Gets whether the front button is currently pressed
        /// </summary>
        public bool IsFrontButtonPressed => isFrontButtonPressed;

        /// <summary>
        /// Gets whether the middle button is currently pressed
        /// </summary>
        public bool IsMiddleButtonPressed => stylus.cluster_middle_value > 0.02f;

        /// <summary>
        /// Gets whether the side button is currently pressed
        /// </summary>
        public bool IsSideButtonPressed => isSideButtonPressed;

        /// <summary>
        /// Gets whether the front button was just released
        /// </summary>
        public bool IsFrontButtonJustReleased => !isFrontButtonPressed && wasFrontButtonPressed;

        /// <summary>
        /// Gets the current pressure value from the middle button
        /// </summary>
        public float CurrentPressure => currentPressure;

        /// <summary>
        /// Gets the current pressure value from the tip
        /// </summary>
        public float CurrentTipPressure => currentTipPressure;

        /// <summary>
        /// Initializes input actions and sets up device change handling
        /// </summary>
        private void Awake()
        {
            EnableInputActions();
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        /// <summary>
        /// Unsubscribes from device change events when destroyed
        /// </summary>
        private void OnDestroy()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        /// <summary>
        /// Handles input device connection changes
        /// </summary>
        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device.name.ToLower().Contains("logitech"))
            {
                switch (change)
                {
                    case InputDeviceChange.Disconnected:
                        DisableInputActions();
                        break;
                    case InputDeviceChange.Reconnected:
                        EnableInputActions();
                        break;
                }
            }
        }

        /// <summary>
        /// Updates stylus input state and processes events
        /// </summary>
        private void Update()
        {
            // Save previous state for detecting changes
            wasFrontButtonPressed = isFrontButtonPressed;
            wasSideButtonPressed = isSideButtonPressed;
            lastPressure = currentPressure;
            lastTipPressure = currentTipPressure;

            // Update stylus data
            UpdateStylusInputs();

            // Update current state variables
            isFrontButtonPressed = stylus.cluster_front_value;
            isSideButtonPressed = stylus.cluster_back_value;
            currentPressure = stylus.cluster_middle_value;
            currentTipPressure = stylus.tip_value;

            // Process and raise events for state changes
            CheckButtonEvents();

            // Update visual representation
            UpdateVisuals();
        }

        /// <summary>
        /// Updates the StylusInputs structure with current values
        /// </summary>
        private void UpdateStylusInputs()
        {
            stylus.inkingPose.position = transform.position;
            stylus.inkingPose.rotation = transform.rotation;
            stylus.tip_value = tipActionRef.action.ReadValue<float>();
            stylus.cluster_middle_value = middleActionRef.action.ReadValue<float>();
            stylus.cluster_front_value = grabActionRef.action.IsPressed();
            stylus.cluster_back_value = optionActionRef.action.IsPressed();
            stylus.any = stylus.tip_value > 0 ||
                          stylus.cluster_front_value ||
                          stylus.cluster_middle_value > 0 ||
                          stylus.cluster_back_value;
            stylus.isActive = stylus.any;
        }

        /// <summary>
        /// Checks for button state changes and raises events accordingly
        /// </summary>
        private void CheckButtonEvents()
        {
            if (isFrontButtonPressed != wasFrontButtonPressed)
            {
                if (isFrontButtonPressed)
                {
                    buttonEvents.OnFrontButtonPressed?.Invoke();
                }
                else
                {
                    buttonEvents.OnFrontButtonReleased?.Invoke();
                }
            }

            if (isSideButtonPressed != wasSideButtonPressed)
            {
                if (isSideButtonPressed)
                {
                    buttonEvents.OnSideButtonPressed?.Invoke();
                }
                else
                {
                    buttonEvents.OnSideButtonReleased?.Invoke();
                }
            }

            if (Mathf.Abs(currentPressure - lastPressure) > pressureThreshold)
            {
                buttonEvents.OnPressureChanged?.Invoke(currentPressure);
            }

            if (Mathf.Abs(currentTipPressure - lastTipPressure) > pressureThreshold)
            {
                buttonEvents.OnTipPressureChanged?.Invoke(currentTipPressure);
            }
        }

        /// <summary>
        /// Updates the visual representation of buttons based on their state
        /// </summary>
        private void UpdateVisuals()
        {
            if (tip != null && tip.TryGetComponent<MeshRenderer>(out var tipRenderer))
            {
                tipRenderer.material.color = stylus.tip_value > 0 ? ActiveColor : DefaultColor;
            }

            if (clusterFront != null && clusterFront.TryGetComponent<MeshRenderer>(out var frontRenderer))
            {
                frontRenderer.material.color = stylus.cluster_front_value ? ActiveColor : DefaultColor;
            }

            if (clusterMiddle != null && clusterMiddle.TryGetComponent<MeshRenderer>(out var middleRenderer))
            {
                middleRenderer.material.color = stylus.cluster_middle_value > 0 ? ActiveColor : DefaultColor;
            }

            if (clusterBack != null && clusterBack.TryGetComponent<MeshRenderer>(out var backRenderer))
            {
                backRenderer.material.color = stylus.cluster_back_value ? ActiveColor : DefaultColor;
            }
        }

        /// <summary>
        /// Enables all input actions
        /// </summary>
        private void EnableInputActions()
        {
            tipActionRef?.action?.Enable();
            grabActionRef?.action?.Enable();
            optionActionRef?.action?.Enable();
            middleActionRef?.action?.Enable();
        }

        /// <summary>
        /// Disables all input actions
        /// </summary>
        private void DisableInputActions()
        {
            tipActionRef?.action?.Disable();
            grabActionRef?.action?.Disable();
            optionActionRef?.action?.Disable();
            middleActionRef?.action?.Disable();
        }

        /// <summary>
        /// Triggers a haptic pulse feedback on the device
        /// </summary>
        /// <param name="amplitude">Strength of the pulse (0-1)</param>
        /// <param name="duration">Duration of the pulse in seconds</param>
        public void TriggerHapticPulse(float amplitude, float duration)
        {
            var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(stylus.isOnRightHand ? UnityEngine.XR.XRNode.RightHand : UnityEngine.XR.XRNode.LeftHand);
            device.SendHapticImpulse(0, amplitude, duration);
        }

        /// <summary>
        /// Triggers a haptic click feedback with preset duration and amplitude
        /// </summary>
        public void TriggerHapticClick()
        {
            TriggerHapticPulse(hapticClickAmplitude, hapticClickDuration);
        }

        /// <summary>
        /// Gets the current position of the stylus tip
        /// </summary>
        public override Vector3 TipPosition => tip != null ? tip.transform.position : transform.position;

        /// <summary>
        /// Gets the current direction of the stylus tip
        /// </summary>
        public override Vector3 TipDirection => tip != null ? -tip.transform.up : -transform.up;

        /// <summary>
        /// Gets the transform of the stylus tip
        /// </summary>
        public override Transform TipTransform => tip != null ? tip.transform : transform;
    }
}