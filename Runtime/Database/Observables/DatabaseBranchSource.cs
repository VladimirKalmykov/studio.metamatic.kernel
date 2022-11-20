using System;

namespace Metamatic
{
    public class DatabaseBranchSource: ObservableValue<string>
    {
        public DatabaseBranchSource(string initialName) : base(initialName)
        {
            
        }

        public DatabaseBranchSource(DatabaseBranchSource other) : base(other)
        {
            
        }

        public string GetBranchName()
        {
            return this.value;
        }
    }
}