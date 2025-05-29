using UnityEngine;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Base class for all smart widgets
    /// </summary>
    public abstract class SmartWidget : MonoBehaviour
    {
        /// <summary>
        /// Parent sticky note
        /// </summary>
        protected StickyNote ParentStickyNote { get; private set; }

        /// <summary>
        /// Set parent sticky note
        /// </summary>
        /// <param name="stickyNote">The parent sticky note.</param>
        public void SetParentStickyNote(StickyNote stickyNote)
        {
            ParentStickyNote = stickyNote;
        }

        /// <summary>
        /// Initialize widget with data from recognition
        /// </summary>
        /// <param name="recognizedText">The recognized text for initialization.</param>
        public abstract void Initialize(string recognizedText);
    }
}