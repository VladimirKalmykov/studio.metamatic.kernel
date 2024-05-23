using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.MicroBehaviours
{
    /// <summary>
    /// MicroBehaviour that tracks spawn areas and emit events when they are inside the camera field of view
    /// </summary>
    public class CameraFOVTracker2d: MicroBehaviour
    {
        private Camera m_CustomCamera;
        private float m_InnerBuffer;
        private float m_OuterBuffer;
        private IReadOnlyList<VirtualArea2d> m_TrackedAreas;
        private IReadOnlyList<Transform> m_TrackedTransforms;
        private float m_ZDepth;
        private Rect[] m_SideRects;

        public float innerBuffer
        {
            get => m_InnerBuffer;
            set => m_InnerBuffer = value;
        }

        public float outerBuffer
        {
            get => m_OuterBuffer;
            set => m_OuterBuffer = value;
        }

        public IReadOnlyList<VirtualArea2d> trackedAreas
        {
            get => m_TrackedAreas;
            set => m_TrackedAreas = value;
        }

        public IReadOnlyList<Transform> trackedTransforms
        {
            get => m_TrackedTransforms;
            set => m_TrackedTransforms = value;
        }

        public float zDepth
        {
            get => m_ZDepth;
            set => m_ZDepth = value;
        }
        private Rect[] sideRects => m_SideRects;
        
        #region Events
        public event Action<VirtualArea2d, Rect, float> OnAreaOverlapped;
        public event Action<VirtualArea2d> OnAreaNotOverlapped;
        public event Action<Transform> OnTransformOverlapped;
        public event Action<Transform> OnTransformNotOverlapped;
        public event Action OnPostTrack;
        #endregion
        
        public Camera camera
        {
            get
            {
                if (m_CustomCamera == null)
                {
                    m_CustomCamera = Camera.main;
                }

                return m_CustomCamera;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera">Current camera (by default Camera.main)</param>
        /// <param name="innerBuffer">Gap around FOV that considered as visible for camera as well</param>
        /// <param name="outerBuffer">Second gap around inner gap is used to overlap with spawn areas (because they should be activated outside the FOV but close to it)</param>
        /// <param name="zDepth">Distance from screen and tracked plane z depth</param>
        /// <param name="trackAreas">Array of areas for tracking</param>
        /// <param name="trackedTransforms">Array of transforms for tracking (unlike the areas all transforms are tracked within outer buffer full size)</param>
        /// <example>
        /// await using var activator = new CameraFOVBufferSpawnAreasActivator2d(Camera.main, 0.1f, 0.2f, 0f, new SpawnArea2d[]{new RectSpawnArea2d(new Rect(0, 0, 1, 1))});
        /// </example>
        public CameraFOVTracker2d(
            Camera camera = null,
            float innerBuffer = 0f,
            float outerBuffer = 1f,
            float zDepth = 0f,
            IReadOnlyList<VirtualArea2d> trackAreas = null,
            IReadOnlyList<Transform> trackedTransforms = null
            ): base()
        {
            m_CustomCamera = camera;
            this.m_InnerBuffer = innerBuffer;
            this.m_OuterBuffer = outerBuffer;
            this.m_TrackedAreas = trackAreas;
            this.m_TrackedTransforms = trackedTransforms;
            this.m_ZDepth = zDepth;

            Run();
        }
        
        #region Private Methods

        protected override async UniTask Main(CancellationToken cancellationToken)
        {
            var camera = this.camera;
            Rect previousBounds = default;
            var diceThrower = new UniqueDice(4);
            using var lifecycle = AsyncUtils.PlayerLoopAction(() =>
            {
                var (mainBounds, innerBufferBounds) = CalculateCameraBounds(camera, m_ZDepth, outerBuffer, innerBuffer);
                if (previousBounds == mainBounds) return;
                
                m_SideRects = new Rect[]
                {
                    new Rect(mainBounds.xMin, mainBounds.yMin, innerBufferBounds.xMin - mainBounds.xMin,
                        mainBounds.height), // lEFT
                    new Rect(innerBufferBounds.xMax, mainBounds.yMin, mainBounds.xMax - innerBufferBounds.xMax,
                        mainBounds.height), // RIGHT
                    new Rect(mainBounds.xMin, innerBufferBounds.yMax, mainBounds.width,
                        mainBounds.yMax - innerBufferBounds.yMax), // TOP
                    new Rect(mainBounds.xMin, mainBounds.yMin, mainBounds.width,
                        innerBufferBounds.yMin - mainBounds.yMin) // BOTTOM
                };
                    
                if (m_TrackedAreas != null) {
                    /* Track areas */
                    foreach (var area in m_TrackedAreas)
                    {
                        diceThrower.Reset();
                        int side = 0;
                        while((side = diceThrower.Next()) > -1)
                        {
                            var t = m_SideRects[side];
                            var overlapSize = area.OverlapsAreaSize(t);
                            if (overlapSize > 0f)
                            {
                                OnAreaOverlapped?.Invoke(area, t, overlapSize);
                                break;
                            }
                            else
                            {
                                OnAreaNotOverlapped?.Invoke(area);
                            }
                        }
                    }
                }
                
                if (m_TrackedTransforms != null)
                {
                    /* Track transforms */
                    foreach (var t in m_TrackedTransforms)
                    {
                        if (mainBounds.Contains(t.position))
                        {
                            OnTransformOverlapped?.Invoke(t);
                        }
                        else
                        {
                            OnTransformNotOverlapped?.Invoke(t);
                        }
                    }
                }
                OnPostTrack?.Invoke();
                OnPostTrack = null;
            }, PlayerLoopTiming.FixedUpdate);

            await cancellationToken;
        }
        
        private (Rect mainBounds, Rect innerBufferBounds) CalculateCameraBounds(Camera camera, float zDepth, float outerBuffer, float innerBuffer)
        {
            Vector3 bottomLeft = camera.ViewportToWorldPoint(new Vector3(0, 0, Mathf.Abs(camera.transform.position.z - zDepth)));
            Vector3 topRight = camera.ViewportToWorldPoint(new Vector3(1, 1, Mathf.Abs(camera.transform.position.z - zDepth)));

            Rect mainBounds = new Rect(
                bottomLeft.x - outerBuffer - innerBuffer,
                bottomLeft.y - outerBuffer - innerBuffer,
                (topRight.x - bottomLeft.x) + (outerBuffer + innerBuffer) * 2,
                (topRight.y - bottomLeft.y) + (outerBuffer + innerBuffer) * 2
            );

            Rect innerBufferBounds = new Rect(
                bottomLeft.x - innerBuffer,
                bottomLeft.y - innerBuffer,
                (topRight.x - bottomLeft.x) + innerBuffer * 2,
                (topRight.y - bottomLeft.y) + innerBuffer * 2
            );

            return (mainBounds, innerBufferBounds);
        }
        #endregion
        
        #if UNITY_EDITOR
        public void DrawGizmos()
        {
            if (camera != null)
            {
                var (mainBounds, bufferBounds) = CalculateCameraBounds(camera, zDepth, outerBuffer, innerBuffer);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(new Vector3(mainBounds.xMin, mainBounds.yMin, zDepth), new Vector3(mainBounds.xMax, mainBounds.yMin, zDepth));
                Gizmos.DrawLine(new Vector3(mainBounds.xMax, mainBounds.yMin, zDepth), new Vector3(mainBounds.xMax, mainBounds.yMax, zDepth));
                Gizmos.DrawLine(new Vector3(mainBounds.xMax, mainBounds.yMax, zDepth), new Vector3(mainBounds.xMin, mainBounds.yMax, zDepth));
                Gizmos.DrawLine(new Vector3(mainBounds.xMin, mainBounds.yMax, zDepth), new Vector3(mainBounds.xMin, mainBounds.yMin, zDepth));

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(new Vector3(bufferBounds.xMin, bufferBounds.yMin, zDepth), new Vector3(bufferBounds.xMax, bufferBounds.yMin, zDepth));
                Gizmos.DrawLine(new Vector3(bufferBounds.xMax, bufferBounds.yMin, zDepth), new Vector3(bufferBounds.xMax, bufferBounds.yMax, zDepth));
                Gizmos.DrawLine(new Vector3(bufferBounds.xMax, bufferBounds.yMax, zDepth), new Vector3(bufferBounds.xMin, bufferBounds.yMax, zDepth));
                Gizmos.DrawLine(new Vector3(bufferBounds.xMin, bufferBounds.yMax, zDepth), new Vector3(bufferBounds.xMin, bufferBounds.yMin, zDepth));
            
                /* Draw side rects as filled rectangles */
                var colors = new Color[]
                {
                    Color.red,
                    Color.blue,
                    Color.green,
                    Color.yellow
                };
                for (var i = 0; i < m_SideRects.Length; i++)
                {
                    Gizmos.color = colors[i];
                    var sideRect = m_SideRects[i];
                    Gizmos.DrawCube(new Vector3(sideRect.center.x, sideRect.center.y, zDepth), new Vector3(sideRect.width, sideRect.height, 0));
                }
            }
        }
        #endif
    }
}