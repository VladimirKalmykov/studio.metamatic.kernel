using System;

namespace Metamatic.Exceptions
{
    public class FatalError: Exception
    {
        public FatalError(string message): base(message)
        {
            
        }
    }
}