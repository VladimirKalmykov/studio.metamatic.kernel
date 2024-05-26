using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic.Actions
{
    public interface IDispatcher
    {
        delegate bool Condition<T>(T source) where T : notnull;
        delegate void AnnouncementsHandler<T>(T[] sources) where T : notnull;
        delegate void RetractionsHandler<T>(T source) where T : notnull;

        delegate void EventHandler<T>(T source);
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
        public DisposableAction Subscribe<T>(EventHandler<T> handler) where T : notnull;
        /// <summary>
        /// Unsubscribe from an event of type T
        /// </summary>
        /// <param name="handler"></param>
        /// <typeparam name="T"></typeparam>
        public void Unsubscribe<T>(EventHandler<T> handler) where T : notnull;
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
        public DisposableAction SubscribeAnnouncements<T>(AnnouncementsHandler<T> handler) where T : notnull;
        /// <summary>
        /// Subscribe to retractions of type T
        /// </summary>
        /// <param name="handler"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public DisposableAction SubscribeRetractions<T>(RetractionsHandler<T> handler) where T : notnull;
        /// <summary>
        /// Register an object of type T with an id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="source"></param>
        /// <typeparam name="T"></typeparam>
        public IDisposable Register<TKey, TValue>(TKey id, TValue source) where TValue : class;
        /// <summary>
        /// Subscribe to registered objects of type T with an id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="handler"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public DisposableAction SubscribeRegistry<TKey, TValue>(TKey id, EventHandler<TValue> handler, bool skipInitial = false) where TValue : class;
        /// <summary>
        /// A version of Register method without id (it takes a self reference as an id)
        /// </summary>
        /// <param name="source"></param>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <returns></returns>
        public IDisposable Register<TValue>(TValue source) where TValue : class;

        /// <summary>
        /// A version of SubscribeRegistry method without id (it takes a self reference as an id)
        /// </summary>
        /// <param name="handler"></param>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <returns></returns>
        public DisposableAction SubscribeRegistry<TValue>(IDispatcher.EventHandler<TValue> handler, bool skipInitial = false) where TValue : class;
        /// <summary>
        /// Wait for a registered object of type T with an id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public UniTask<TValue> WhenRegistered<TKey, TValue>(TKey id, CancellationToken cancellationToken) where TValue : class;
        /// <summary>
        /// A version of WhenRegistered method without id (it takes a self reference as an id)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TValue"></typeparam>
        /// <returns></returns>
        public UniTask<TValue> WhenRegistered<TValue>(CancellationToken cancellationToken) where TValue : class;
        /// <summary>
        /// Get helper object that provides access to a registered object of type T with an id
        /// </summary>
        /// <param name="id"></param>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <returns></returns>
        public RegistrySource<TValue> GetRegistry<TKey, TValue>(TKey id) where TValue : class;
    }
}