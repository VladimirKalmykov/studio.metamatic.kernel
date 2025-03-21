using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    [RequireComponent(typeof(Checkpoint))]
    public abstract class CheckpointTrigger: MonoBehaviour
    {
        protected Checkpoint Checkpoint;
        public void Activate(Checkpoint checkpoint)
        {
            Checkpoint = checkpoint;
            enabled = true;
        }
        public void Deactivate()
        {
            enabled = false;
        }
    }
}