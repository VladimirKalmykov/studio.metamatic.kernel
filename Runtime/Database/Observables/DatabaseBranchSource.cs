namespace Metamatic
{
    public class DatabaseBranchSource: MicroStateProxy<string>
    {
        public DatabaseBranchSource(string initialName) : base(new MicroState<string>(initialName))
        {
            
        }
        
        public DatabaseBranchSource(ReadOnlyMicroState<string> other) : base(other)
        {
            
        }

        public string GetBranchName()
        {
            return this.value;
        }
    }
}