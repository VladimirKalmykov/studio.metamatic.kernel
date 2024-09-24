using UnityEngine;

namespace Metamatic
{
    public class ExposedBehaviourLifecycle: MonoBehaviour
    {
        private CancellationSignalSource m_ActivityCancellationSignalSource;
        private CancellationSignalSource m_EnableCancellationSignalSource;
        
        public CancellationSignal activityCancellationSignal { get; private set; }
        public CancellationSignal enableCancellationSignal { get; private set; }
        
        protected virtual void Awake()
        {
            m_ActivityCancellationSignalSource = CancellationSignalSource.Create();
            activityCancellationSignal = m_ActivityCancellationSignalSource.Signal;
            enableCancellationSignal = CancellationSignal.Cancelled;
        }
        
        protected virtual void OnEnable()
        {
            m_EnableCancellationSignalSource = CancellationSignalSource.Create();
            enableCancellationSignal = m_EnableCancellationSignalSource.Signal;
        }
        
        protected virtual void OnDisable()
        {
            m_EnableCancellationSignalSource.TryCancel();
            enableCancellationSignal = CancellationSignal.Cancelled;
        }
        
        protected virtual void OnDestroy()
        {
            m_ActivityCancellationSignalSource.TryCancel();
            activityCancellationSignal = CancellationSignal.Cancelled;
        }
    }
}