using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// Exposes the OnDisable and OnDestroy events as signals.
    /// TODO: Consider to merge this class with ObservableMonoBehaviour.
    /// </summary>
    public class MonoBehaviourLifecycleSignals: MonoBehaviour
    {
        private SignalSource m_ActivityCancellationSignalSource;
        private SignalSource m_EnableCancellationSignalSource;
        
        public Signal activityCancellationSignal { get; private set; }
        public Signal enableCancellationSignal { get; private set; }
        
        protected virtual void Awake()
        {
            m_ActivityCancellationSignalSource = SignalSource.Create();
            activityCancellationSignal = m_ActivityCancellationSignalSource.Signal;
            enableCancellationSignal = Signal.Raised;
        }
        
        protected virtual void OnEnable()
        {
            m_EnableCancellationSignalSource = SignalSource.Create();
            enableCancellationSignal = m_EnableCancellationSignalSource.Signal;
        }
        
        protected virtual void OnDisable()
        {
            m_EnableCancellationSignalSource.TryRaise();
            enableCancellationSignal = Signal.Raised;
        }
        
        protected virtual void OnDestroy()
        {
            m_ActivityCancellationSignalSource.TryRaise();
            activityCancellationSignal = Signal.Raised;
        }
    }
}