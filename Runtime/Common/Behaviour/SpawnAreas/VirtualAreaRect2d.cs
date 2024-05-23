using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Metamatic
{
    public class VirtualAreaRect2d: VirtualArea2d
    {
        [FormerlySerializedAs("rect")] [SerializeField] private Rect m_Rect;
        public override float areaSize
        {
            get
            {
                var worldRect = rect;
                return worldRect.width * worldRect.height;
            }
        }

        public Rect rect {
            get
            {
                Vector2 localMin = m_Rect.min;
                Vector2 localMax = m_Rect.max;

                Vector2 worldMin = transform.TransformPoint(localMin);
                Vector2 worldMax = transform.TransformPoint(localMax);

                Rect worldRect = new Rect(worldMin, worldMax - worldMin);
                return worldRect;
            }
        }
        public override Vector2 GetRandomPosition()
        {
            /* Get random position on rect */
            var worldRect = this.rect;
            return new Vector3(UnityEngine.Random.Range(worldRect.xMin, worldRect.xMax), UnityEngine.Random.Range(worldRect.yMin, worldRect.yMax));
        }

        public override Vector2 GetRandomPosition(Rect otherRect)
        {
            var worldRect = rect;
            if (!otherRect.Overlaps(worldRect))
            {
                throw new System.ArgumentException("Rectangles do not overlap.");
            }

            Rect intersectRect = Rect.MinMaxRect(
                Mathf.Max(otherRect.xMin, worldRect.xMin),
                Mathf.Max(otherRect.yMin, worldRect.yMin),
                Mathf.Min(otherRect.xMax, worldRect.xMax),
                Mathf.Min(otherRect.yMax, worldRect.yMax)
            );

            float randomX = Random.Range(intersectRect.xMin, intersectRect.xMax);
            float randomY = Random.Range(intersectRect.yMin, intersectRect.yMax);

            return new Vector2(randomX, randomY);
        }

        public override bool Overlaps(Rect rect)
        {
            return this.rect.Overlaps(rect, true);
        }

        public override float OverlapsAreaSize(Rect otherRect)
        {
            var worldRect = rect;
            if (!otherRect.Overlaps(worldRect))
            {
                return 0f;
            }

            Rect intersectRect = Rect.MinMaxRect(
                Mathf.Max(otherRect.xMin, worldRect.xMin),
                Mathf.Max(otherRect.yMin, worldRect.yMin),
                Mathf.Min(otherRect.xMax, worldRect.xMax),
                Mathf.Min(otherRect.yMax, worldRect.yMax)
            );

            return intersectRect.width * intersectRect.height;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Handles.color = overlapped ? Color.red : debugColor;
            var worldRect = this.rect;
            Handles.DrawDottedLines(new []
            {
                (Vector3)(worldRect.center + new Vector2(worldRect.width / 2, -worldRect.height / 2f)),
                (Vector3)(worldRect.center + new Vector2(worldRect.width / 2, worldRect.height / 2f)),
                (Vector3)(worldRect.center + new Vector2(worldRect.width / 2, worldRect.height / 2f)),
                (Vector3)(worldRect.center + new Vector2(-worldRect.width / 2, worldRect.height / 2f)),
                (Vector3)(worldRect.center + new Vector2(-worldRect.width / 2, worldRect.height / 2f)),
                (Vector3)(worldRect.center + new Vector2(-worldRect.width / 2, -worldRect.height / 2f)),
                (Vector3)(worldRect.center + new Vector2(-worldRect.width / 2, -worldRect.height / 2f)),
                (Vector3)(worldRect.center + new Vector2(worldRect.width / 2, -worldRect.height / 2f)),
            }, 0.3f);
        }
        #endif
    }
}