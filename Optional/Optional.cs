
using UnityEngine;

namespace Utils
{
    // <summary>
    // Optional is a generic class that can be used to represent an optional value.
    // It contains a boolean flag to indicate whether the value is enabled or not.
    [System.Serializable]
    public class Optional<T> : ISerializationCallbackReceiver where T : new()
    {
        public bool Enabled = false;
        public T Value;

        public Optional()
        {
            Value = new();
        }

        public void OnAfterDeserialize()
        {
        }

        public void OnBeforeSerialize()
        {
            if (!Enabled)
            {
                Value = default;
            }
        }
    }
}
