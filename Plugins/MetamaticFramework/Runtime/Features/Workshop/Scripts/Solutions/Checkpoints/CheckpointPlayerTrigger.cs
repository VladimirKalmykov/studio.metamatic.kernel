using UnityEngine;
using UnityEngine.Events;

namespace Metamatic.Framework.Workshop
{
    [RequireComponent(typeof(Checkpoint))]
    public class CheckpointPlayerTrigger: MonoBehaviour
    {
        public UnityEvent<Entity> OnTrigger;
        private Checkpoint m_Checkpoint;
        private void OnEnable()
        {
            m_Checkpoint = GetComponent<Checkpoint>();
            m_Checkpoint.OnTrigger += OnCheckpointTrigger;
        }

        private void OnDisable()
        {
            m_Checkpoint.OnTrigger -= OnCheckpointTrigger;
        }

        private void OnCheckpointTrigger(Entity entity)
        {
            if (PlayerSystem.IsPlayer(entity))
            {
                OnTrigger?.Invoke(entity);
            }            
        }
    }
}