using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using XRC.Students.Sp2025.P36.Yan;
using UnityEngine.XR.Interaction.Toolkit;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Stylus marking menu for front button.
    /// </summary>
    public class StylusMarkingMenu : MonoBehaviour
    {
        [Header("Stylus Settings")]
        [SerializeField] private MxInkHandler stylusHandler;
        [SerializeField] private Transform menuSpawnPoint;
        [SerializeField] private float menuDistance = 0.10f;
        [SerializeField] private float longPressThreshold = 0.35f;

        [Header("Interaction Settings")]
        [Tooltip("Reference to the XRRayInteractor used for menu interaction")]
        [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rayInteractor;

        [Header("Menu Reference")]
        [Tooltip("Reference to the root GameObject of the menu in the scene")]
        [SerializeField] private GameObject sceneMenu;
        [SerializeField] private bool animateMenuAppearance = true;
        [SerializeField] private float appearAnimationDuration = 0.2f;

        [Header("Button Hover Effect")]
        [SerializeField] private float buttonHoverScale = 1.075f;
        [SerializeField] private float hoverScaleTransitionDuration = 0.1f;

        private bool isMenuActive;
        private bool isFrontButtonPressed;
        private float frontButtonPressStartTime;
        private Vector3 originalMenuScale;
        private GameObject lastHoveredButton;
        private GameObject currentlyHoveredButton;

        private Dictionary<GameObject, Coroutine> buttonScaleCoroutines = new Dictionary<GameObject, Coroutine>();

        private void Awake()
        {
            if (rayInteractor == null)
            {
                rayInteractor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
            }

            isMenuActive = false;
            isFrontButtonPressed = false;

            if (sceneMenu != null)
            {
                originalMenuScale = sceneMenu.transform.localScale;
                sceneMenu.SetActive(false);
            }
        }

        private void Start()
        {
            if (stylusHandler == null)
            {
                stylusHandler = GetComponentInParent<MxInkHandler>();
                if (stylusHandler == null)
                {
                    Debug.LogError("StylusMarkingMenu: MxInkHandler not found. Attach this component to a stylus with MxInkHandler.");
                    enabled = false;
                    return;
                }
            }

            if (menuSpawnPoint == null)
            {
                menuSpawnPoint = stylusHandler.TipTransform;
                if (menuSpawnPoint == null)
                {
                    Debug.LogError("StylusMarkingMenu: Menu spawn point not set and stylus tip transform not found.");
                    enabled = false;
                    return;
                }
            }

            if (sceneMenu == null)
            {
                Debug.LogError("StylusMarkingMenu: Scene menu reference not set. Please assign an existing menu GameObject.");
                enabled = false;
                return;
            }

            if (rayInteractor == null)
            {
                Debug.LogWarning("StylusMarkingMenu: XRRayInteractor not found. Menu interaction will be limited.");
            }

            SetupStylusEvents();
        }

        private void Update()
        {
            if (isFrontButtonPressed && !isMenuActive)
            {
                if (Time.time - frontButtonPressStartTime >= longPressThreshold)
                {
                    if (CanShowMenu())
                    {
                        ShowMenu();
                    }
                }
            }

            if (isMenuActive)
            {
                TrackHoveredButton();
            }
        }

        /// <summary>
        /// Can show menu.
        /// </summary>
        private bool CanShowMenu()
        {
            // Only show menu when not interacting with other objects
            if (rayInteractor != null)
            {
                return !rayInteractor.hasSelection;
            }

            return true;
        }

        /// <summary>
        /// Track hovered button.
        /// </summary>
        private void TrackHoveredButton()
        {
            if (rayInteractor == null || !rayInteractor.enabled) return;

            GameObject newHoveredButton = null;

            if (rayInteractor.TryGetCurrentUIRaycastResult(out RaycastResult raycastResult))
            {
                Button button = raycastResult.gameObject.GetComponent<Button>();
                if (button != null)
                {
                    newHoveredButton = raycastResult.gameObject;
                    lastHoveredButton = newHoveredButton;
                }
            }

            if (newHoveredButton != currentlyHoveredButton)
            {
                if (currentlyHoveredButton != null)
                {
                    ScaleButton(currentlyHoveredButton, Vector3.one);
                }

                currentlyHoveredButton = newHoveredButton;

                if (currentlyHoveredButton != null)
                {
                    ScaleButton(currentlyHoveredButton, Vector3.one * buttonHoverScale);
                }
            }
        }

        /// <summary>
        /// Scale button with animation.
        /// </summary>
        private void ScaleButton(GameObject button, Vector3 targetScale)
        {
            if (buttonScaleCoroutines.TryGetValue(button, out Coroutine existingCoroutine))
            {
                if (existingCoroutine != null)
                {
                    StopCoroutine(existingCoroutine);
                }
            }

            Coroutine newCoroutine = StartCoroutine(AnimateButtonScale(button, targetScale));
            buttonScaleCoroutines[button] = newCoroutine;
        }

        /// <summary>
        /// Animate button scale.
        /// </summary>
        private IEnumerator AnimateButtonScale(GameObject button, Vector3 targetScale)
        {
            if (button == null) yield break;

            float startTime = Time.time;
            Vector3 startScale = button.transform.localScale;

            while (Time.time < startTime + hoverScaleTransitionDuration)
            {
                if (button == null) yield break;

                float progress = (Time.time - startTime) / hoverScaleTransitionDuration;
                button.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
                yield return null;
            }

            if (button != null)
            {
                button.transform.localScale = targetScale;
            }

            if (buttonScaleCoroutines.ContainsKey(button))
            {
                buttonScaleCoroutines.Remove(button);
            }
        }

        /// <summary>
        /// Setup stylus events.
        /// </summary>
        private void SetupStylusEvents()
        {
            if (stylusHandler != null)
            {
                stylusHandler.buttonEvents.OnFrontButtonPressed += OnStylusFrontButtonPressed;
                stylusHandler.buttonEvents.OnFrontButtonReleased += OnStylusFrontButtonReleased;
            }
        }

        private void OnDestroy()
        {
            if (stylusHandler != null)
            {
                stylusHandler.buttonEvents.OnFrontButtonPressed -= OnStylusFrontButtonPressed;
                stylusHandler.buttonEvents.OnFrontButtonReleased -= OnStylusFrontButtonReleased;
            }

            StopAllButtonCoroutines();
        }

        /// <summary>
        /// Stop all button coroutines.
        /// </summary>
        private void StopAllButtonCoroutines()
        {
            foreach (var coroutine in buttonScaleCoroutines.Values)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }

            buttonScaleCoroutines.Clear();
        }

        private void OnStylusFrontButtonPressed()
        {
            isFrontButtonPressed = true;
            frontButtonPressStartTime = Time.time;
        }

        private void OnStylusFrontButtonReleased()
        {
            isFrontButtonPressed = false;

            if (isMenuActive)
            {
                HandleMenuSelection();
                HideMenu();
            }
        }

        /// <summary>
        /// Show menu.
        /// </summary>
        private void ShowMenu()
        {
            if (sceneMenu == null) return;

            if (isMenuActive)
            {
                HideMenu();
            }

            if (rayInteractor != null && !rayInteractor.enabled)
            {
                rayInteractor.enabled = true;
            }

            Vector3 menuPosition = CalculateMenuPosition();
            sceneMenu.transform.position = menuPosition;
            OrientMenuToRay();

            sceneMenu.SetActive(true);

            if (animateMenuAppearance)
            {
                StartCoroutine(AnimateMenuAppearance());
            }
            else
            {
                sceneMenu.transform.localScale = originalMenuScale;
            }

            isMenuActive = true;
            lastHoveredButton = null;
            currentlyHoveredButton = null;

            ResetAllButtonScales();
            buttonScaleCoroutines.Clear();

            if (stylusHandler != null)
            {
                stylusHandler.TriggerHapticPulse(0.5f, 0.1f);
            }
        }

        /// <summary>
        /// Reset all button scales.
        /// </summary>
        private void ResetAllButtonScales()
        {
            Button[] allButtons = sceneMenu.GetComponentsInChildren<Button>();
            foreach (Button button in allButtons)
            {
                button.transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Hide menu.
        /// </summary>
        private void HideMenu()
        {
            StopAllButtonCoroutines();

            if (sceneMenu != null && sceneMenu.activeInHierarchy)
            {
                ResetAllButtonScales();
            }

            currentlyHoveredButton = null;

            if (sceneMenu != null)
            {
                sceneMenu.SetActive(false);
            }

            isMenuActive = false;
            lastHoveredButton = null;
        }

        /// <summary>
        /// Calculate menu position.
        /// </summary>
        private Vector3 CalculateMenuPosition()
        {
            Vector3 tipUp = menuSpawnPoint.up;

            return menuSpawnPoint.position - tipUp * menuDistance;
        }

        /// <summary>
        /// Orient menu to ray.
        /// </summary>
        private void OrientMenuToRay()
        {
            if (sceneMenu == null) return;

            Quaternion stylusRotation = menuSpawnPoint.rotation;
            Vector3 eulerAngles = stylusRotation.eulerAngles;

            eulerAngles.x = -90;
            Quaternion finalRotation = Quaternion.Euler(eulerAngles);

            sceneMenu.transform.rotation = finalRotation;
        }

        /// <summary>
        /// Handle menu selection.
        /// </summary>
        private void HandleMenuSelection()
        {
            if (!isMenuActive || sceneMenu == null) return;

            if (lastHoveredButton != null)
            {
                Button button = lastHoveredButton.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.Invoke();

                    if (stylusHandler != null)
                    {
                        stylusHandler.TriggerHapticClick();
                    }
                }
            }
        }

        /// <summary>
        /// Animate menu appearance.
        /// </summary>
        private IEnumerator AnimateMenuAppearance()
        {
            float startTime = Time.time;
            sceneMenu.transform.localScale = Vector3.zero;

            while (Time.time < startTime + appearAnimationDuration)
            {
                float progress = (Time.time - startTime) / appearAnimationDuration;
                sceneMenu.transform.localScale = Vector3.Lerp(Vector3.zero, originalMenuScale, progress);
                yield return null;
            }

            sceneMenu.transform.localScale = originalMenuScale;
        }
    }
}