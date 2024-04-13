namespace Metamatic
{
    public class DatabaseEntitySource: MicroStateProxy<string>
    {
        public DatabaseEntitySource(string initialName) : base(new MicroState<string>(initialName))
        {
            
        }
        
        public DatabaseEntitySource(MicroStateReadOnly<string> source) : base(source)
        {
            SetProxy(source);
        }

        public string GetEntityName()
        {
            return this.value;
        }
    }
}