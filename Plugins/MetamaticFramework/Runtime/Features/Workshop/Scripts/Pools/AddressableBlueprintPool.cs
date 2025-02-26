using System;
using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.Framework.Workshop;
using Metamatic.Systems;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Component = System.ComponentModel.Component;
using Object = UnityEngine.Object;

namespace Plugins.MetamaticFramework.Workshop
{
    public class AddressableBlueprintPool<T> where T: Component
    {
        private class BlueprintGameObjectPoolConfig : IPoolConfig<RecyclableBehaviour>
        {
            private readonly GameObject m_Prefab;
            
            public BlueprintGameObjectPoolConfig(GameObject prefab)
            {
                m_Prefab = prefab;
            }
            public object GetKey()
            {
                return (m_Prefab, GetType());
            }

            public RecyclableBehaviour OnCreate()
            {
                var instance = UnityEngine.Object.Instantiate(m_Prefab);
                var recyclable = instance.GetOrAddComponent<RecyclableBehaviour>();
                return recyclable;
            }

            public void OnGet(RecyclableBehaviour instance)
            {
                // Ignore
            }

            public void OnReturn(RecyclableBehaviour instance)
            {
                instance.gameObject.SetActive(false);
            }

            public void OnDestroy(RecyclableBehaviour instance)
            {
                Object.Destroy(instance.gameObject);
            }
        }

        private int m_Capacity;
        private event Action OnDisposeEvent;
        private ConfigurablePool<RecyclableBehaviour> m_Pool;
        private SignalSource m_DisposeSignalSource;
        private bool m_IsInitialized = false;
        private Exception m_InitializationException = null;
        private event Action OnInitialized;
        private event Action<Exception> OnException;
        private readonly SignedIndexedPool<SignalSource> m_SummonedSignals = new SignedIndexedPool<SignalSource>(100); // TODO: Decide capacity
        
        
        public AddressableBlueprintPool(AssetReferenceT<ScriptableBlueprint> assetReference, int initialCapacity = 0)
        {
            m_DisposeSignalSource = SignalSource.Create();
            Initialize(assetReference, m_DisposeSignalSource.Signal, initialCapacity).Forget();
        }

        private async UniTask Initialize(AssetReferenceT<ScriptableBlueprint> assetReference, Signal cancellationSignal, int initialCapacity)
        {
            try
            {
                var blueprintSummon = ResourcesSystem.Summon<ScriptableBlueprint>(assetReference);
                OnDisposeEvent += blueprintSummon.Dispose;
                var blueprint = await blueprintSummon.Wait(cancellationSignal);
                var prefabSummon = blueprint.SummonPrefab(cancellationSignal);
                var prefab = await prefabSummon.Wait(cancellationSignal);
                OnDisposeEvent += prefabSummon.Dispose;
                m_Pool = new ConfigurablePool<RecyclableBehaviour>(new BlueprintGameObjectPoolConfig(prefab), initialCapacity);
                m_IsInitialized = true;
                OnInitialized?.Invoke();
                OnInitialized = null;
            } catch (Exception e)
            {
                m_InitializationException = e;
                OnException?.Invoke(e);
                OnException = null;
            }
        }

        private UniTask WhenInitialized(Signal cancellationSignal)
        {
            if (m_InitializationException != null)
            {
                return UniTask.FromException(m_InitializationException);
            }
            if (m_IsInitialized)
            {
                return UniTask.CompletedTask;
            }
            var tsc = SafeUniTaskCompletionSource.Create();
            OnInitialized += tsc.TrySetResultSilently;
            OnException += tsc.TrySetExceptionSilently;

            return tsc.Task.ContinueWith(cancellationSignal.Register(tsc.TrySetCanceledSilently));
        }
        
        /// <summary>
        /// Get an instance of T from the pool and return it when its Recycle method is called
        /// </summary>
        /// <param name="cancellationSignal"></param>
        /// <returns></returns>
        public async UniTask<T> Get(Signal cancellationSignal = default)
        {
            await WhenInitialized(cancellationSignal);
            var summon = m_Pool.Summon();
            var instance = summon.resource;

            instance.OnRecycle += summon.Dispose;

            return instance.GetComponent<T>();
        }
        
        public AsyncSummon<T> Summon()
        {
            var css = SignalSource.Create();
            var task = InternalGetRaw(css.Signal);
            var token = m_SummonedSignals.Add(css);
            
            return new AsyncSummon<T>(token, task, ReturnRaw);
        }
        
        private async UniTask<T> InternalGetRaw(Signal cancellationSignal = default)
        {
            await WhenInitialized(cancellationSignal);
            var summon = m_Pool.Summon();
            var instance = summon.resource;
            cancellationSignal.Register(summon.Dispose);
            return instance.GetComponent<T>();
        }

        private void ReturnRaw(Token96Bit token)
        {
            if (m_SummonedSignals.TryGet(token, out var css))
            {
                css.TryRaise();
            }
        }
    }
}