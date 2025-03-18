namespace Metamatic.Framework.Workshop
{
    public enum DismissReason
    {
        /// <summary>
        /// Killed. The entity is dead. It can be revived and should not be saved or virtualized, but can be returned to the pool.
        /// </summary>
        Kill,
        /// <summary>
        /// Kicked. The entity is removed from the world and should be saved or virtualized, also GO can be returned to the pool.
        /// Entity should not materialize again without explicit request. 
        /// </summary>
        Kick,
        /// <summary>
        /// Abandoned. The entity is removed from the world and should be saved or virtualized, also GO can be returned to the pool.
        /// Entity can materialize again without explicit request if its area is active again.
        /// </summary>
        Abandon,
        /// <summary>
        /// Entity is removed from the world, don't save or virtualize, but return to the pool.
        /// </summary>
        Release,
        /// <summary>
        /// Destroyed by Unity.
        /// </summary>
        Destroy
    }

    public enum KillReason
    {
        None,
        /// <summary>
        /// Is killed by another character of effect according to game logic.
        /// </summary>
        Logic,
        /// <summary>
        /// The object has left the play area, or unforeseen circumstances have occurred.
        /// </summary>
        Exception
    }
}