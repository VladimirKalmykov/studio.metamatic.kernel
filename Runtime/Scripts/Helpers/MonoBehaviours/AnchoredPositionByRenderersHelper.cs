using System;

namespace Metamatic
{
    using UnityEngine;

    public class AnchoredPositionByRenderersHelper : MonoBehaviour, IAnchoredPositionReceiver
    {
        [SerializeField] private bool useOnlyRootRenderer = false;
        [Tooltip("Multiplier for the anchor")] [SerializeField]
        private float multiplier = 1f;
        private Renderer[] m_Renderers;
        private Transform m_TargetTransform;

        void Awake()
        {
            m_Renderers = useOnlyRootRenderer ? new [] { GetComponent<Renderer>() }: GetComponentsInChildren<Renderer>();
            if (m_Renderers[0] == null) m_Renderers = Array.Empty<Renderer>();
            if (m_TargetTransform == null)
            {
                m_TargetTransform = transform;
            }
        }
        
        /// <summary>
        /// Allows to substitute the target transform. In this case all resulting positions will be applied to the target transform instead of the current one.
        /// Ensure that the local position of this transform is zero to avoid unexpected results.
        /// You can use this method to ignore some renderers in the hierarchy. E.g. if you have a parent object with a renderer that should not be taken into account but should be transformed.
        /// </summary>
        /// <param name="targetTransform"></param>
        public void SetTargetTransform(Transform targetTransform)
        {
            m_TargetTransform = targetTransform;
        }
        
        public AnchoredPosition anchoredPosition
        {
            set => ApplyAnchoredPosition(value);
        }
        
        private Bounds GetCombinedBounds()
        {
            if (m_Renderers.Length == 0)
            {
                return new Bounds(transform.position, Vector3.zero);
            }

            // Инициализируем Bounds первым Renderer
            Bounds combinedBounds = m_Renderers[0].bounds;

            // Объединяем остальные Bounds
            foreach (Renderer renderer in m_Renderers)
            {
                if (!renderer.gameObject.activeInHierarchy) continue;
                combinedBounds.Encapsulate(renderer.bounds);
            }

            return combinedBounds;
        }

        private void ApplyAnchoredPosition(AnchoredPosition anchoredPos)
        {
            Vector3 finalAnchor = anchoredPos.Anchor * multiplier;
            Bounds worldBounds = GetCombinedBounds();
            Vector3 worldSize = worldBounds.size; // размеры (ширина, высота, глубина) в мировом пространстве
            Vector3 boundCenter = worldBounds.center; // центр bounding-box'а в мире

            // 2. Узнаём смещение pivot’а (т. е. где сейчас transform.position 
            //    относительно центра объекта).
            //    Если pivot в Unity совпадает с center bounds, pivotOffset будет (0,0,0).
            //    Если нет — мы это учитываем, чтобы «не прыгало».
            var thisTransform = transform;
            Vector3 pivotOffset = thisTransform.position - boundCenter;

            // 3. Считаем «якорное» смещение по formulaм.
            //    Предположим, anchor=-1 => левый/нижний край => хотим, 
            //    чтобы левый край объекта стал в заданной позиции => 
            //    значит надо сдвинуть объект +halfSize вправо/вверх.
            //    
            //    Для anchor = -1: anchorOffset = -0.5 * size => 
            //    потом мы делаем (position - anchorOffset) => получается +0.5*size.
            //    
            //    Для anchor = 0: anchorOffset = 0 => объект центр = position.
            //    Для anchor = 1: anchorOffset = +0.5 * size => итоговое смещение будет -0.5*size.
            Vector3 anchorOffset = new Vector3
            (
                finalAnchor.x * (worldSize.x * 0.5f),
                finalAnchor.y * (worldSize.y * 0.5f),
                finalAnchor.z * (worldSize.z * 0.5f)
            );

            // 4. Рассчитываем итоговую позицию:
            //    Если anchorOffset > 0, сдвигаем объект в одну сторону, если < 0 — в другую.
            //    pivotOffset возвращает нас к учёту «родного» pivot’а.
            //
            //    Схема: transform.position = искомая "позиция" - anchorOffset + pivotOffset
            Vector3 finalPos = anchoredPos.Position
                               - anchorOffset
                               + pivotOffset;

            m_TargetTransform.position = finalPos;
        }
    }
}