namespace Metamatic
{
    /// <summary>
    /// Represents the level of detail of an object. It is not only about rendering, but also about AI and animation updates.
    /// </summary>
    public enum LODLevel
    {
        /// <summary>
        /// E.g. It is very far away or can not be reached by the player.
        /// On this level the object should not be rendered, and its animations should not be updated.
        /// Instead, its position should be updated by the LODService or AI service by very simplified logic.
        /// </summary>
        No,
        /// <summary>
        /// E.g. Object is not visible or is far away. AI update rate should be very low.
        /// No movements based on physics should be calculated. Only position updates.
        /// IMPORTANT: This level should be used for objects that are not visible, but still have to be updated
        /// tp be in the correct position when they become visible.
        /// </summary>
        Low,
        /// <summary>
        /// E.g. Object at the side of the screen or close to it. AI update rate should be medium.
        /// It can emit sounds. It can have some procedural animations, but with lower priority.
        /// It should be updated with physics, but not with high frequency.
        /// </summary>
        Medium,
        /// <summary>
        /// E.g. Object is in focus, player interacts with it. AI update rate should be high.
        /// Full physics calculations should be done.
        /// </summary>
        High
    }
}