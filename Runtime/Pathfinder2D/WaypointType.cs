namespace Metamatic.Pathfinder
{
    public enum WaypointType
    {
        Default,
        /// <summary>
        /// Waypoint that can be reached only by flying of jumping. Character can't stand on it.
        /// </summary>
        Gravity,
        /// <summary>
        /// Waypoint only for character who can stick to the ceiling (like bat)
        /// </summary>
        Ceiling
    }
}