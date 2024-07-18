using System;

namespace Metamatic
{
    public abstract class ServiceLegacy: RunnableBehaviour {}
    [Obsolete("Use Service instead")]
    public abstract class ServiceLegacy<TSystem> : ServiceLegacy
    where TSystem : ServiceSystem
    {
        public TSystem serviceSystem { get; set; }

        private void Awake()
        {
            serviceSystem = GetComponent<TSystem>();
            if (serviceSystem == null)
            {
                throw new Exception("Service must belong to the GameObject wirh apoppriate service system");
            }

            serviceSystem.RegisterService(this);
            OnServiceAwake();
        }

        private void OnDestroy()
        {
            serviceSystem.UnregisterSerive(this);
        }
        
        protected virtual void OnServiceAwake() {}
    }
}