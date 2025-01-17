namespace Metamatic
{
    using UnityEngine;

    public class AnchoredPositionByRenderersHelper : MonoBehaviour, IAnchoredPositionReceiver
    {
        private Renderer[] m_Renderers;

        void Awake()
        {
            m_Renderers = GetComponentsInChildren<Renderer>();
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
                anchoredPos.Anchor.x * (worldSize.x * 0.5f),
                anchoredPos.Anchor.y * (worldSize.y * 0.5f),
                anchoredPos.Anchor.z * (worldSize.z * 0.5f)
            );

            // 4. Рассчитываем итоговую позицию:
            //    Если anchorOffset > 0, сдвигаем объект в одну сторону, если < 0 — в другую.
            //    pivotOffset возвращает нас к учёту «родного» pivot’а.
            //
            //    Схема: transform.position = искомая "позиция" - anchorOffset + pivotOffset
            Vector3 finalPos = anchoredPos.Position
                               - anchorOffset
                               + pivotOffset;

            thisTransform.position = finalPos;
        }
    }
}