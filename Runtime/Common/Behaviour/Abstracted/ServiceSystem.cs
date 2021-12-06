using System.Collections.Generic;

namespace Metamatic
{
    public abstract class ServiceSystem : PluggableMonoBehaviour
    {
        private List<Service> m_Services = new List<Service>();

        public Service[] services
        {
            get
            {
                return m_Services.ToArray();
            }
        }

        public void RegisterService(Service service)
        {
            m_Services.Add(service);
        }

        public void UnregisterSerive(Service service)
        {
            m_Services.Remove(service);
        }
    }
}