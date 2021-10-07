namespace Metamatic {
    using UnityEngine.Events;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IPlayable {
        UnityEvent OnBeforePlay {
            get;
        }
        UnityEvent OnAfterPlay {
            get;
        }

        Task Play();
        Task Play(CancellationToken cancellationToken);
    }
}