namespace Metamatic
{
    public class DatabaseBranchSource: ObservableProxyValue<string>
    {
        public DatabaseBranchSource(string initialName) : base(new ObservableWritableValue<string>(initialName))
        {
            
        }
        
        public DatabaseBranchSource(ObservableValue<string> other) : base(other)
        {
            
        }

        public string GetBranchName()
        {
            return this.value;
        }
    }
}