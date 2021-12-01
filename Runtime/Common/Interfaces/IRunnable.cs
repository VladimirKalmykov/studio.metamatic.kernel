using System;
using System.Threading;
using System.Threading.Tasks;

public interface IRunnable: IDisposable
{
    public Task Run(CancellationToken cancellationToken);
}