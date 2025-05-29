using UnityEngine;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// This is a component example, inheriting from MonoBehaviour.
    /// Make sure to delete this file when using the package template.
    /// </summary>
    public class ComponentExample : MonoBehaviour
    {
        [SerializeField]
        private float floatExample;
        /// <summary>
        /// This is a public property example, with a private backing field that is serialized.
        /// </summary>
        public float FloatExample
        {
            get => floatExample;
            set => floatExample = value;
        }

        /// <summary>
        /// This is a public method example.
        /// </summary>
        public void MethodExample()
        {
            // Example method body
        }
    }
}