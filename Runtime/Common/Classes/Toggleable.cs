using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic
{
    [Toggle("Enabled")]
    public class Toggleable<T>
    {
        [SerializeField]
        public bool Enabled;
        [SerializeField]
        public T Value;

        public bool enabled => Enabled;
        public T value => Value;
    }
}