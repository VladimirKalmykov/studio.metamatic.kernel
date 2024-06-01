using System.Threading;

namespace Metamatic
{
    public abstract class Service: AsyncBehaviour
    {
        protected sealed override void RunAsync(CancellationToken cancellationToken)
        {
            RunService(cancellationToken);
        }
        
        protected abstract void RunService(CancellationToken cancellationToken);
    }
}