namespace Metamatic
{
    public class DatabaseEntitySource: ObservableProxyValue<string>
    {
        public DatabaseEntitySource(string initialName) : base(new ObservableWritableValue<string>(initialName))
        {
            
        }
        
        public DatabaseEntitySource(ObservableValue<string> source) : base(source)
        {
            PlugIn(source);
        }

        public string GetEntityName()
        {
            return this.value;
        }
    }
}