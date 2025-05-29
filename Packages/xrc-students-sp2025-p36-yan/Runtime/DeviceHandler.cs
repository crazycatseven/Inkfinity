using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using XRC.Students.Sp2025.P36.Yan;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Handles stylus device connections and manages stylus handedness
    /// </summary>
    public class DeviceHandler : MonoBehaviour
    {
        [Header("Stylus")]
        /// <summary>
        /// Left hand stylus game object
        /// </summary>
        [SerializeField, Tooltip("Left hand stylus game object")]
        private GameObject leftStylus;

        /// <summary>
        /// Right hand stylus game object
        /// </summary>
        [SerializeField, Tooltip("Right hand stylus game object")]
        private GameObject rightStylus;

        [Header("Device Detection")]
        /// <summary>
        /// Manufacturer name to detect (lowercase)
        /// </summary>
        [SerializeField, Tooltip("Manufacturer name to detect (lowercase)")]
        private string deviceNameFilter = "logitech";

        /// <summary>
        /// Gets or sets the left hand stylus
        /// </summary>
        public GameObject LeftStylus
        {
            get { return leftStylus; }
            set { leftStylus = value; }
        }

        /// <summary>
        /// Gets or sets the right hand stylus
        /// </summary>
        public GameObject RightStylus
        {
            get { return rightStylus; }
            set { rightStylus = value; }
        }

        /// <summary>
        /// Registers device connection/disconnection callbacks
        /// </summary>
        private void Awake()
        {
            InputDevices.deviceConnected += DeviceConnected;
            InputDevices.deviceDisconnected += DeviceDisconnected;

            // Check for already connected devices
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevices(devices);
            foreach (InputDevice device in devices)
            {
                DeviceConnected(device);
            }
        }

        /// <summary>
        /// Unregisters device callbacks when component is destroyed
        /// </summary>
        private void OnDestroy()
        {
            InputDevices.deviceConnected -= DeviceConnected;
            InputDevices.deviceDisconnected -= DeviceDisconnected;
        }

        /// <summary>
        /// Handles device disconnection events
        /// </summary>
        /// <param name="device">The disconnected input device</param>
        private void DeviceDisconnected(InputDevice device)
        {
            Debug.Log($"Device disconnected: {device.name}");
            bool stylusDisconnected = device.name.ToLower().Contains(deviceNameFilter);

            if (stylusDisconnected)
            {
                if (leftStylus != null) leftStylus.SetActive(false);
                if (rightStylus != null) rightStylus.SetActive(false);
            }
        }

        /// <summary>
        /// Handles device connection events
        /// </summary>
        /// <param name="device">The connected input device</param>
        private void DeviceConnected(InputDevice device)
        {
            Debug.Log($"Device connected: {device.name}");
            bool stylusConnected = device.name.ToLower().Contains(deviceNameFilter);

            if (stylusConnected)
            {
                bool isOnRightHand = (device.characteristics & InputDeviceCharacteristics.Right) != 0;

                if (leftStylus != null) leftStylus.SetActive(!isOnRightHand);
                if (rightStylus != null) rightStylus.SetActive(isOnRightHand);

                // Find and configure the stylus and drawing component
                ConfigureStylusHandedness(isOnRightHand);
            }
        }

        /// <summary>
        /// Configures stylus handedness and links to line drawing component
        /// </summary>
        /// <param name="isOnRightHand">Whether the stylus is in the right hand</param>
        private void ConfigureStylusHandedness(bool isOnRightHand)
        {
            // Look for any stylus handler in the scene
            StylusHandler stylus = FindFirstObjectByType<StylusHandler>();
            if (stylus != null)
            {
                stylus.SetHandedness(isOnRightHand);

                // Find the line drawing component and link it to the stylus
                LineDrawing lineDrawing = FindFirstObjectByType<LineDrawing>();
                if (lineDrawing != null)
                {
                    lineDrawing.Stylus = stylus;
                }
            }
        }
    }
}
