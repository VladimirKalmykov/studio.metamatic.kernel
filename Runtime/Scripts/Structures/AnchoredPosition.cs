using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// Struct for storing the position and anchor of a game object.
    /// </summary>
    public readonly struct AnchoredPosition
    {
        public static AnchoredPosition zero => new AnchoredPosition(Vector3.zero, Vector3.zero);
        public readonly Vector3 Position;
        public readonly Vector3 Anchor;
        
        public AnchoredPosition(Vector3 position, Vector3 anchor)
        {
            Position = position;
            Anchor = anchor;
        }
        
        public override string ToString()
        {
            return $"[{Position}, {Anchor}]";
        }
    }
}