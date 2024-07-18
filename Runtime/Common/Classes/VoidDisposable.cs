using System;

namespace Metamatic
{
    public class VoidDisposable: IDisposable
    {
        public static readonly VoidDisposable None = new VoidDisposable();
        public void Dispose()
        {
            
        }
    }
}