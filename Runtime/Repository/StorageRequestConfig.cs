namespace Metamatic
{
    public readonly struct StorageRequestConfig
    {
        public enum Method
        {
            Read,
            Write
        }

        public readonly string address;
        public readonly string query;
        public readonly Method method;
        public readonly float timeout;
        public readonly int retryCount;
        public StorageRequestConfig(string address, Method method, string query = "", float timeout = 0.0f, int retryCount = 0)
        {
            this.address = address;
            this.method = method;
            this.query = query;
            this.timeout = timeout;
            this.retryCount = retryCount;
        }
        
        public StorageRequestConfig Retry()
        {
            return new StorageRequestConfig(address: address, method, query: query, timeout: timeout, retryCount: retryCount + 1);
        }
    }
}