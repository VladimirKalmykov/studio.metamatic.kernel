using UnityEngine;

namespace Metamatic
{
    public class MeshRenderer2DSortingHelper: MonoBehaviour
    {
        [SortingLayer]
        [SerializeField] int m_SortingLayer;
        [SerializeField] private int orderInLayer;
        
        private MeshRenderer m_MeshRenderer;

        public MeshRenderer meshRenderer
        {
            get
            {
                if (m_MeshRenderer == null)
                {
                    m_MeshRenderer = GetComponent<MeshRenderer>();
                }

                return m_MeshRenderer;
            }
        }
        
        private void OnValidate()
        {
            if (meshRenderer != null)
            {
                meshRenderer.sortingLayerID = m_SortingLayer;
                meshRenderer.sortingOrder = orderInLayer;
            }
        }
        
        private void Reset()
        {
            if (meshRenderer != null)
            {
                m_SortingLayer = meshRenderer.sortingLayerID;
                orderInLayer = meshRenderer.sortingOrder;
            }
        }
    }
}