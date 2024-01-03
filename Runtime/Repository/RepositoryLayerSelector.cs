namespace Metamatic
{
    public struct RepositoryLayerSelector
    {
        private RepositoryBehaviour m_RepositoryBehaviour;
        private string m_LayerName;
        public RepositoryLayerSelector(RepositoryBehaviour repositoryBehaviour, string layerName)
        {
            m_RepositoryBehaviour = repositoryBehaviour;
            m_LayerName = layerName;
        }
        
        public RepositoryStore<T> GetStore<T>(string name, T defaultValue = default) where T: struct
        {
            return m_RepositoryBehaviour.GetStore<T>(m_LayerName, name, defaultValue);
        }
    }
}