using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// Struct for storing the position and anchor for relative positioning
    /// </summary>
    public readonly struct AnchoredPosition
    {
        public static AnchoredPosition zero => new AnchoredPosition(Vector3.zero, Vector3.zero, Vector3.zero);
        /// <summary>
        /// Position of placement
        /// </summary>
        public readonly Vector3 Position;
        /// <summary>
        /// Self anchor in local space (for hints it is offset from the hint center where 0,0,0 is the center of the hint and 1,1,1 is the top right backward corner)
        /// </summary>
        public readonly Vector3 Anchor;
        /// <summary>
        /// Relative position (for hints it is position of the object that the hint is attached to)
        /// </summary>
        public readonly Vector3 Relative;
        
        public AnchoredPosition(Vector3 position, Vector3 anchor, Vector3 relative)
        {
            Position = position;
            Anchor = anchor;
            Relative = relative;
        }
        
        public override string ToString()
        {
            return $"[{Position}, {Anchor}]";
        }
    }
}