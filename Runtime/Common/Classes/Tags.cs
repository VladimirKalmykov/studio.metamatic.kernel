using System.Collections;
using System.Linq;
using UnityEngine;

namespace Metamatic {
    /// <summary>
    /// Serializable tags field that has property drawer
    /// </summary>
    [System.Serializable]
    public struct Tags {
        [SerializeField]
        public string[] names;

        public int Count {
            get {
                return names == null
                    ? 0
                    : names.Length;
            }
        }
        public string[] ToArray() {
            return names == null
                ? new string[0]
                : names.ToArray();
        }

        public bool Contains(string tag) {
            return names == null
                ? false
                : names.Contains(tag);
        }
        public IEnumerator GetEnumerator() {
            return ToArray().GetEnumerator();
        }

        public string this[int index] {
            get {
                if (names == null) {
                    throw new System.IndexOutOfRangeException("Index out of range");
                }
                return names[index];
            }
            set {
                names[index] = value;
            }
        }
    }
}