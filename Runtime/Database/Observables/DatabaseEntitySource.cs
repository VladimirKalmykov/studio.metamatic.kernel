using System;

namespace Metamatic
{
    public class DatabaseEntitySource: ObservableValue<string>
    {
        public DatabaseEntitySource(string initialName) : base(initialName)
        {
            
        }
        
        public DatabaseEntitySource(ObservableValue<string> source) : base(source)
        {
            
        }

        public string GetEntityName()
        {
            return this.value;
        }
    }
}