using System;

namespace Metamatic
{
    public class DatabaseEntitySource: ObservableValue<string>
    {
        public DatabaseEntitySource(string initialName) : base(initialName)
        {
            
        }
        
        public DatabaseEntitySource(DatabaseEntitySource other) : base(other)
        {
            
        }

        public string GetEntityName()
        {
            return this.value;
        }
    }
}