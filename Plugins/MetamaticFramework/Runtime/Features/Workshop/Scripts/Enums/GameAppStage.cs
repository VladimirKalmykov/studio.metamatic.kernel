namespace Metamatic.Framework.Workshop
{
    public enum GameAppStage
    {
        /// <summary>
        /// Game is loading
        /// </summary>
        Loading,
        /// <summary>
        ///  Game is unloading (e.g. stop game and return to main menu)
        /// </summary>
        Unloading,
        /// <summary>
        /// Load another game during runtime
        /// </summary>
        Reloading,
        /// <summary>
        /// In main menu
        /// </summary>
        Stopped,
        /// <summary>
        /// Game is running
        /// </summary>
        Running,
        /// <summary>
        /// Game can not continue because of an error
        /// </summary>
        Error
    }
}