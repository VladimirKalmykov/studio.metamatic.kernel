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
        /// Entity should not 
        /// </summary>
        Kick,
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

    public enum KickReason
    {
        None,
        /// <summary>
        /// Game object is out of active areas and should be saved or virtualized, also GO can be returned to the pool.
        /// If the last area of residence is activated again, the entity will be revived.
        /// </summary>
        Abandon,
        /// <summary>
        /// Game object should be virtualized, also GO can be returned to the pool.
        /// It can be returned from virtualization to the world only by programmer's decision.
        /// </summary>
        Manual
    }
}