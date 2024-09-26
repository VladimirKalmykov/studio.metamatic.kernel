namespace Metamatic.Pathfinder
{
    public class Edge2D
    {
        public Waypoint2D Target; // Целевой узел
        public float Cost;      // Стоимость перехода

        public Edge2D(Waypoint2D target, float cost)
        {
            Target = target;
            Cost = cost;
        }
    }
}