using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Metamatic.Actions
{
    class TypesRegistry : Dictionary<Type, List<object>> {}
    
    [DisallowMultipleComponent]
    public class Dispatcher : MonoBehaviour, IDispatcher
    {
        #region Global
        private static Dispatcher _globalDispatcher;

        public static Dispatcher global
        {
            get => _globalDispatcher ??= new GameObject("__GLOBALDISPATCHER__").AddComponent<Dispatcher>();
        }
        #endregion

        public void Emit<T>(T source) where T : notnull
        {
            throw new NotImplementedException();
        }

        public DisposableAction Subscribe<T>(System.Action<T> handler) where T : notnull
        {
            throw new NotImplementedException();
        }

        public UniTask<T> WhenEmitted<T>(IDispatcher.Condition<T> condition, CancellationToken cancellationToken) where T : notnull
        {
            throw new NotImplementedException();
        }

        public DisposableAction Announce<T>(T source) where T : notnull
        {
            throw new NotImplementedException();
        }

        public void Retract<T>(T source) where T : notnull
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<T> GetAnnounced<T>() where T : notnull
        {
            throw new NotImplementedException();
        }

        public UniTask<T> WhenAnnounce<T>(IDispatcher.Condition<T> condition, CancellationToken cancellationToken) where T : notnull
        {
            throw new NotImplementedException();
        }

        public DisposableAction SubscribeAnnouncements<T>(System.Action<T[]> handler) where T : notnull
        {
            throw new NotImplementedException();
        }

        public DisposableAction SubscribeRetractions<T>(System.Action<T> handler) where T : notnull
        {
            throw new NotImplementedException();
        }
    }
}