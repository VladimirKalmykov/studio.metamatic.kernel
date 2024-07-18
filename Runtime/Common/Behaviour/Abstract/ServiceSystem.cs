using System.Collections.Generic;

namespace Metamatic
{
    public abstract class ServiceSystem : PluggableMonoBehaviour
    {
        private List<ServiceLegacy> m_Services = new List<ServiceLegacy>();

        public ServiceLegacy[] services
        {
            get
            {
                return m_Services.ToArray();
            }
        }

        public void RegisterService(ServiceLegacy service)
        {
            m_Services.Add(service);
        }

        public void UnregisterSerive(ServiceLegacy service)
        {
            m_Services.Remove(service);
        }
    }
}