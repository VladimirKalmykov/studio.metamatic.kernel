using System;

namespace Metamtic.Exceptions
{
    public class FatalError: Exception
    {
        public FatalError(string message): base(message)
        {
            
        }
    }
}