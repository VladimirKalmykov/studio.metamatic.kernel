namespace Metamatic
{
    public readonly struct IOOperationConfig
    {
        public enum OperationType
        {
            Read,
            Write
        }
        public readonly string address;
        public readonly OperationType operationType;
        public readonly float timeout;
        public readonly int retryCount;
        public IOOperationConfig(string address, OperationType operationType, float timeout = 0.0f, int retryCount = 0)
        {
            this.address = address;
            this.operationType = operationType;
            this.timeout = timeout;
            this.retryCount = retryCount;
        }
        
        public IOOperationConfig Retry()
        {
            return new IOOperationConfig(address, operationType, timeout, retryCount + 1);
        }
    }
}