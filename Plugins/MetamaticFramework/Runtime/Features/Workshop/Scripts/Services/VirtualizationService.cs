using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Metamatic.Framework.Workshop
{
    public class VirtualizationService: Service
    {
        private Dictionary<string, Entity.SaveData> m_VirtualizedEntities = new();
        protected override UniTask RunService(Ready ready, Signal signal)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Virtualize entity
        /// </summary>
        /// <param name="entity"></param>
        public void Virtualize(Entity entity)
        {
            var saveData = (Entity.SaveData)entity.Save();
            m_VirtualizedEntities[entity.id] = saveData;
            if (!entity.isStatic)
            {
                RecyclableBehaviour.Banish(entity.gameObject);
            }
        }

        public void Virtualize(GameArea gameArea)
        {
            switch (gameArea)
            {
                case GameArea2D gameArea2D:
                    var entities = GameAreas2dService.GetIntersectedEntities(gameArea);
                    break;
                case GameArea3D gameArea3D:
                    throw new System.NotImplementedException();
            }
        }
        
        /// <summary>
        /// Materialize entity from virtualized state and entity instance
        /// </summary>
        /// <param name="entity"></param>
        public void Materialize(Entity entity)
        {
            if (m_VirtualizedEntities.TryGetValue(entity.id, out var saveData))
            {
                entity.Load(saveData);
            }
        }
    }
}