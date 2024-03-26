using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic.Actions
{
    public interface IDispatcher
    {
        delegate bool Condition<T>(T source) where T : notnull;
        /// <summary>
        /// Emit an event of type T
        /// </summary>
        /// <param name="source"></param>
        /// <typeparam name="T"></typeparam>
        public void Emit<T>(T source) where T : notnull;
        /// <summary>
        /// Subscribe to an event of type T
        /// </summary>
        /// <param name="handler"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public DisposableAction Subscribe<T>(System.Action<T> handler) where T : notnull;
        /// <summary>
        /// Wait for an event of type T
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public UniTask<T> WhenEmitted<T>(Condition<T> condition, CancellationToken cancellationToken) where T : notnull;
        /// <summary>
        /// Announce an object of type T (announced objects are stored in a list)
        /// </summary>
        /// <param name="source"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public DisposableAction Announce<T>(T source) where T : notnull;
        /// <summary>
        /// Retract an object of type T (retracted objects are removed from the list)
        /// </summary>
        /// <param name="source"></param>
        /// <typeparam name="T"></typeparam>
        public void Retract<T>(T source) where T : notnull;
        /// <summary>
        /// Get all announced objects of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IReadOnlyList<T> GetAnnounced<T>() where T : notnull;
        /// <summary>
        /// Wait for an announced object of type T which satisfies the condition
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public UniTask<T> WhenAnnounce<T>(Condition<T> condition, CancellationToken cancellationToken) where T : notnull;
        /// <summary>
        /// Subscribe to announcements of type T (the handler is called with all of announced objects first time and then with each new announced object)
        /// </summary>
        /// <param name="handler"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public DisposableAction SubscribeAnnouncements<T>(System.Action<T[]> handler) where T : notnull;
        /// <summary>
        /// Subscribe to retractions of type T
        /// </summary>
        /// <param name="handler"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public DisposableAction SubscribeRetractions<T>(System.Action<T> handler) where T : notnull;
    }
}