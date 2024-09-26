using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Metamatic.Pathfinder
{
    public class WaypointsGraph2D : MonoBehaviour, ISerializationCallbackReceiver
    {
        #region Serialized fields

        [SerializeField] [Tooltip("Size of the comb for waypoints generation")]
        private float combSize = 1f;

        [SerializeField] private float areaWidth = 10f;

        [SerializeField] private float areaHeight = 10f;

        [SerializeField, LabelText("Max distance between edges")]
        private float maxDistanceBetweenEdges = 6f;

        [SerializeField, LabelText("Obstacles layer mask")]
        private LayerMask obstaclesLayerMask;

        [SerializeField, LabelText("Floor layer mask")]
        private LayerMask floorLayerMask;

        [SerializeField] private float m_MaxCharacterHeight = 0.5f;

        [Tooltip("The gap of waypoints on the surface")] [SerializeField]
        private float m_SurfaceGap = 0.1f;

        /// <summary>
        /// One vertex can have only N direction to have edges with. This field defines the count of sectors for edge calculation.
        /// Lower value will increase the performance, but may lead to the lack of edges.
        /// </summary>
        [SerializeField] [Tooltip("Count of sectors for edge calculation")] [Range(1, 16)]
        private int sectorsCount = 8;

        [SerializeField] [HideInInspector] private SerializedWaypoint2D[] waypointArray;

        #endregion

        #region Debug fields

        [BoxGroup("Editor")] public Color defaultWaypointColor = new Color(0.92f, 0.65f, 0.05f);
        [BoxGroup("Editor")] public Color gravityWaypointColor = new Color(0f, 0.49f, 1f);
        [BoxGroup("Editor")] public Color ceilingWaypointColor = new Color(0.93f, 0.21f, 1f);
        [BoxGroup("Editor")] public Color defaultEdgeColor = new Color(0.19f, 0.7f, 0.39f);

        #endregion

        #region Private fields

        private readonly List<Waypoint2D> m_Waypoints = new List<Waypoint2D>();
        private int m_LastId;

        #endregion

        #region Public events

        public event Action OnWaypointsChanged;

        #endregion

        #region Public properties

        public int waypointsCount => m_Waypoints.Count;

        #endregion

        #region Serialization

        public void OnBeforeSerialize()
        {
            waypointArray = new SerializedWaypoint2D[m_Waypoints.Count];
            var waypointIndexes = new Dictionary<Waypoint2D, int>();
            for (var i = 0; i < m_Waypoints.Count; i++)
            {
                waypointIndexes.Add(m_Waypoints[i], i);
            }

            for (var i = 0; i < m_Waypoints.Count; i++)
            {
                var edges = new SerializedEdge2D[m_Waypoints[i].Neighbors.Count];
                for (var j = 0; j < m_Waypoints[i].Neighbors.Count; j++)
                {
                    var targetIndex = waypointIndexes[m_Waypoints[i].Neighbors[j].Target];
                    edges[j] = new SerializedEdge2D(targetIndex, m_Waypoints[i].Neighbors[j].Cost);
                }

                var serializedWaypoint = new SerializedWaypoint2D(m_Waypoints[i].Position, m_Waypoints[i].Type, edges);
                waypointArray[i] = serializedWaypoint;
            }
        }

        public void OnAfterDeserialize()
        {
            m_Waypoints.Clear();
            if (waypointArray == null) return; // Nothing to deserialize
            var waypointIndexes = new Dictionary<int, Waypoint2D>();
            for (var i = 0; i < waypointArray.Length; i++)
            {
                var waypoint = new Waypoint2D(waypointArray[i].Position, waypointArray[i].Type);
                m_Waypoints.Add(waypoint);
                waypointIndexes.Add(i, waypoint);
            }

            for (var i = 0; i < waypointArray.Length; i++)
            {
                var waypoint = waypointIndexes[i];
                for (var j = 0; j < waypointArray[i].Neighbors.Length; j++)
                {
                    var target = waypointIndexes[waypointArray[i].Neighbors[j].TargetIndex];
                    waypoint.AddNeighbor(target, waypointArray[i].Neighbors[j].Cost);
                }
            }

            OnWaypointsChanged?.Invoke();
        }

        private void ForceSerialization()
        {
            // Явно вызываем OnBeforeSerialize
            OnBeforeSerialize();

            // Если требуется обновить объект в редакторе, можно использовать:
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        #endregion

        #region Private logic

        private void CalculateEdgesForWaypoint(Waypoint2D newWaypoint)
        {
            /* Clear sectors */
            foreach (var waypoint in m_Waypoints)
            {
                waypoint.sectorsOccupied = new bool[sectorsCount];
            }

            if (newWaypoint.sectorsOccupied == null || newWaypoint.sectorsOccupied.Length != sectorsCount)
            {
                newWaypoint.sectorsOccupied = new bool[sectorsCount];
            }

            float sectorAngle = 360f / sectorsCount;
            // Sort waypoints by distance to newWaypoint
            var sortedWaypoints = m_Waypoints
                .Where(waypoint => waypoint != newWaypoint)
                .OrderBy(waypoint => Vector2.Distance(newWaypoint.Position, waypoint.Position))
                .ToList();

            foreach (var waypoint in sortedWaypoints)
            {
                float distance = Vector2.Distance(newWaypoint.Position, waypoint.Position);

                if (distance <= maxDistanceBetweenEdges)
                {
                    // Direction from newWaypoint to waypoint
                    Vector2 direction = (waypoint.Position - newWaypoint.Position).normalized;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;

                    // Shift angle by half a sector to avoid boundary issues
                    float adjustedAngle = (angle + sectorAngle / 2f) % 360f;

                    int sectorIndex = Mathf.FloorToInt(adjustedAngle / sectorAngle) % sectorsCount;

                    // Direction from waypoint to newWaypoint
                    float reverseAngle = (adjustedAngle + 180f) % 360f;
                    int reverseSectorIndex = Mathf.FloorToInt(reverseAngle / sectorAngle) % sectorsCount;

                    // Check if sectors are free in both waypoints
                    if (!newWaypoint.sectorsOccupied[sectorIndex] && !waypoint.sectorsOccupied[reverseSectorIndex])
                    {
                        // Raycast to check for obstacles
                        RaycastHit2D hit = Physics2D.Raycast(newWaypoint.Position, direction, distance,
                            obstaclesLayerMask);

                        if (hit.collider == null)
                        {
                            newWaypoint.AddNeighbor(waypoint, distance);
                            waypoint.AddNeighbor(newWaypoint, distance);

                            // Mark sectors as occupied
                            newWaypoint.sectorsOccupied[sectorIndex] = true;
                            waypoint.sectorsOccupied[reverseSectorIndex] = true;
                        }
                    }
                }
            }

            ForceSerialization();
        }

        #endregion

        #region Public methods

        private readonly Observable<bool> m_IsGenerating = new();
        public ObservableReadOnly<bool> isGenerating => m_IsGenerating;
        private Vector3 m_CurrentGeneratorPoint;
        public (Vector3, Vector2) m_CurrentGeneratorRaycast;
        private CancellationSignalSource m_GeneratingCancellationTokenSignal;

        public async UniTask GenerateWaypoints(bool recalculateEdges = false, bool lazy = true,
            CancellationSignal cancellationSignal = default)
        {
            var startRealTime = Time.realtimeSinceStartup;
            m_GeneratingCancellationTokenSignal = CancellationSignalSource.CreateLinked(cancellationSignal);
            var signal = m_GeneratingCancellationTokenSignal.Signal;
            if (m_IsGenerating.value)
            {
                throw new InvalidOperationException("Generation process is already running");
            }

            m_IsGenerating.value = true;
            ClearWaypoints();
            bool wasObstacleSurface = false;
            var position = transform.position;
            for (float x = position.x - areaWidth / 2; x < position.x + areaWidth / 2; x += combSize)
            {
                wasObstacleSurface = false;
                for (float y = position.y + areaHeight / 2; y > position.y - areaHeight / 2; y -= 0.3f)
                {
                    var point = new Vector2(x, y);
                    float distance = 0f;
                    m_CurrentGeneratorPoint = point;
                    /* Create point ray to detect that we are not on obstacle layer */
                    var hit = Physics2D.Raycast(point, Vector2.zero, 0f, obstaclesLayerMask | floorLayerMask);
                    if (hit.collider != null)
                    {
                        wasObstacleSurface = true;
                        if (lazy)
                        {
                            await UniTask.Yield();
                            if (signal.IsCancellationRequested)
                            {
                                return;
                            }
                        }

                        continue;
                    }

                    if (wasObstacleSurface)
                    {
                        wasObstacleSurface = false;
                        /* Create ceiling waypoint */
                        distance = Vector2.Distance(point, new Vector2(x, position.y + areaHeight / 2));
                        hit = Physics2D.Raycast(point, Vector2.up, distance, obstaclesLayerMask | floorLayerMask);
                        var hitDown = Physics2D.Raycast(point, Vector2.down, m_MaxCharacterHeight,
                            obstaclesLayerMask | floorLayerMask);

                        // Distance to the ceiling is less than max character height
                        if (hit.collider != null && (hitDown.collider == null ||
                                                     (Vector2.Distance(point, hit.point) +
                                                      Vector2.Distance(point, hitDown.point)) >= m_MaxCharacterHeight))
                        {
                            Console.Debug("WaypointsGraph2D", "AddWaypoint", $"{x},{y} (Ceiling)");
                            AddWaypoint(transform.InverseTransformPoint(hit.point + Vector2.down * m_SurfaceGap),
                                WaypointType.Ceiling, false);
                            m_CurrentGeneratorPoint = new Vector3(x, y);
                            if (lazy)
                            {
                                await UniTask.Yield();
                                if (signal.IsCancellationRequested)
                                {
                                    return;
                                }
                            }
                        }
                    }

                    /* Create infinity down ray to detect ground or floor */
                    distance = Vector2.Distance(point, new Vector2(x, position.y - areaHeight / 2));
                    hit = Physics2D.Raycast(point, Vector2.down, distance, obstaclesLayerMask | floorLayerMask);
                    m_CurrentGeneratorRaycast = (point, Vector2.down * distance);

                    // Distance to the ground is less than max character height
                    if (hit.collider == null || Vector2.Distance(point, hit.point) < m_MaxCharacterHeight)
                    {
                        continue;
                    }

                    /* Add default waypoint */
                    Console.Debug("WaypointsGraph2D", "AddWaypoint", $"{x},{y} (Default)");
                    AddWaypoint(transform.InverseTransformPoint(hit.point + Vector2.up * m_SurfaceGap),
                        WaypointType.Default, false);
                    wasObstacleSurface = true;

                    /* Set y to the ground */
                    y = hit.point.y;
                    m_CurrentGeneratorPoint = new Vector3(x, y);
                    if (lazy)
                    {
                        await UniTask.Yield();
                        if (signal.IsCancellationRequested)
                        {
                            return;
                        }
                    }
                }
            }

            if (recalculateEdges)
            {
                RecalculateAllEdges();
            }
            else
            {
                ForceSerialization();
            }

            m_IsGenerating.value = false;
            var totalDuration = Time.realtimeSinceStartup - startRealTime;
            Console.Debug("WaypointsGraph2D", "GenerateWaypoints", $"Generation completed in {totalDuration} seconds");
            OnWaypointsChanged?.Invoke();
        }

        public void CancelGeneration()
        {
            m_GeneratingCancellationTokenSignal.TryCancel();
            m_IsGenerating.value = false;
        }

        /// <summary>
        /// Recalculate all edges
        /// </summary>
        public void RecalculateAllEdges()
        {
            /* Clear all edges */
            foreach (var waypoint in m_Waypoints)
            {
                waypoint.Neighbors.Clear();
            }

            foreach (var waypoint in m_Waypoints)
            {
                CalculateEdgesForWaypoint(waypoint);
            }

            OnWaypointsChanged?.Invoke();
            ForceSerialization();
        }

        public void AddWaypoint(Waypoint2D waypoint2D, bool calculateEdges = false)
        {
            unchecked
            {
                waypoint2D.id = m_LastId++;
            }
            m_Waypoints.Add(waypoint2D);
            if (calculateEdges)
            {
                CalculateEdgesForWaypoint(waypoint2D);
            }

            OnWaypointsChanged?.Invoke();
            ForceSerialization();
        }

        public void AddWaypoint(Vector2 position, WaypointType type, bool calculateEdges = false)
        {
            var waypoint = new Waypoint2D(position, type);
            unchecked
            {
                waypoint.id = m_LastId++;
            }
            m_Waypoints.Add(waypoint);
            if (calculateEdges)
            {
                CalculateEdgesForWaypoint(waypoint);
            }

            OnWaypointsChanged?.Invoke();
            ForceSerialization();
        }

        public void ClearWaypoints()
        {
            m_Waypoints.Clear();
            OnWaypointsChanged?.Invoke();

            ForceSerialization();
        }

        #endregion

        #region Gismos

        private void OnDrawGizmosSelected()
        {
            if (m_IsGenerating.value)
            {
                /* Render current generator point and raycast */
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(m_CurrentGeneratorPoint, 0.2f);
                Gizmos.DrawLine(m_CurrentGeneratorRaycast.Item1,
                    m_CurrentGeneratorRaycast.Item1 + (Vector3)m_CurrentGeneratorRaycast.Item2);
            }

            var position = transform.position;
            /* Draw area width and height */
            Gizmos.color = new Color(1f, 0.44f, 0.46f, 1f);
            Gizmos.DrawWireCube(position, new Vector3(areaWidth, areaHeight, 0.1f));

            /* Draw comb grid */
            Gizmos.color = new Color(1f, 0.44f, 0.46f, 0.51f);
            for (float x = -areaWidth / 2; x < areaWidth / 2; x += combSize)
            {
                Gizmos.DrawLine(new Vector3(position.x + x, position.y - areaHeight / 2),
                    new Vector3(position.x + x, position.y + areaHeight / 2));
            }

            foreach (var waypoint in m_Waypoints)
            {
                switch (waypoint.Type)
                {
                    case WaypointType.Default:
                        Gizmos.color = defaultWaypointColor;
                        break;
                    case WaypointType.Gravity:
                        Gizmos.color = gravityWaypointColor;
                        break;
                    case WaypointType.Ceiling:
                        Gizmos.color = ceilingWaypointColor;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Gizmos.DrawSphere(position + (Vector3)waypoint.Position, 0.1f);
                foreach (var edge in waypoint.Neighbors)
                {
                    Gizmos.color = defaultEdgeColor;
                    Gizmos.DrawLine(position + (Vector3)waypoint.Position, position + (Vector3)edge.Target.Position);
                }
                
                /* Draw waypoint id at top */
                UnityEditor.Handles.Label(position + (Vector3)waypoint.Position + Vector3.up * 0.2f, waypoint.id.ToString());
            }
        }

        #endregion
    }
}