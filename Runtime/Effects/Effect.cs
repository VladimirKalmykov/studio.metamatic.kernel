using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Metamatic.Effects
{
    public sealed class InternalEffectSource<T>: IDisposable
    where T: struct
    {
        /// <summary>
        /// Create internal effect source (do not use this method directly)
        /// </summary>
        /// <param name="producer">The object which apply effect</param>
        /// <param name="target">The object which accept effect</param>
        /// <param name="data">Effect data</param>
        /// <param name="instant">Effect is not continuously</param>
        /// <returns></returns>
        internal static InternalEffectSource<T> Create(Object producer, Object target, T data, bool instant = false)
        {
            var effect = ParamLessPool<InternalEffectSource<T>>.GetShared();
            effect.Initialize(producer, target, data, instant);
            return effect;
        }

        public Object Producer;
        public Object Consumer;
        public float StartTime;
        public bool Instant;
        public T data { get; private set; }
        public int version { get; private set; }
        public Effect<T> effect => new Effect<T>(this);
        public event Action<T> OnDataChanged;
        public CancellationSignal cancellationSignal => m_CancellationSignalSource.Signal;
        
        private CancellationSignalSource m_CancellationSignalSource;
        
        private void Initialize(Object producer, Object consumer, T data, bool instant)
        {
            this.Producer = producer;
            this.Consumer = consumer;
            this.data = data;
            this.Instant = instant;
            StartTime = Time.time;

            if (!instant)
            {
                m_CancellationSignalSource = CancellationSignalSource.Create();
            }
        }
        
        public void UpdateData(T data)
        {
            this.data = data;
            OnDataChanged?.Invoke(data);
        }
        
        /// <summary>
        /// Subscribe to data changes
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public DisposableAction Subscribe(Action<T> action)
        {
            OnDataChanged += action;
            action(data);
            return new DisposableAction(() => OnDataChanged -= action);
        }

        public void Release()
        {
            unchecked
            {
                version++;
            }
            m_CancellationSignalSource.TryCancel();
            m_CancellationSignalSource = default;
            ParamLessPool<InternalEffectSource<T>>.ReturnShared(this);
        }

        public void Dispose()
        {
            m_CancellationSignalSource.TryCancel();
        }
    }
    
    public readonly struct EffectSource<T>: IDisposable
    where T: struct
    {
        /// <summary>
        /// Create new effect source
        /// </summary>
        /// <param name="producer"></param>
        /// <param name="consumer"></param>
        /// <param name="data"></param>
        /// <param name="instant"></param>
        /// <returns></returns>
        public static EffectSource<T> Create(Object producer, Object consumer, T data, bool instant = false)
        {
            return new EffectSource<T>(InternalEffectSource<T>.Create(producer, consumer, data, instant));
        }
        private readonly InternalEffectSource<T> m_Source;
        private readonly int m_Version;

        public T data
        {
            get => source?.data ?? default;
            set => source?.UpdateData(value);
        }

        public EffectSource(InternalEffectSource<T> source)
        {
            m_Source = source;
            m_Version = source.version;
        }
        
        private InternalEffectSource<T> source => m_Source != null && m_Source.version == m_Version ? m_Source : null;

        public void Dispose()
        {
            source?.Release();
        }
    }
    
    public readonly struct Effect<T>: IDisposable, IEffect
    where T: struct
    {
        private readonly InternalEffectSource<T> m_Source;
        private readonly int m_Version;
        public T data => source?.data ?? default;

        public Effect(InternalEffectSource<T> source)
        {
            m_Source = source;
            m_Version = source.version;
        }
        
        private InternalEffectSource<T> source => m_Source != null && m_Source.version == m_Version ? m_Source : null;

        public void Dispose()
        {
            if (m_Source != null)
            {
                m_Source.Release();
            }
        }

        public Object producer => source?.Producer;
        public Object consumer => source?.Consumer;
        public float startTime => source?.StartTime ?? 0;
        public bool instant => source?.Instant ?? false;
        public CancellationSignal cancellationSignal => source?.cancellationSignal ?? default;
        

        public DisposableAction SubscribeToRelease(Action action)
        {
            if (instant)
            {
                action();
                return DisposableAction.Empty;
            }
            return cancellationSignal.Register(action);
        }
    }
}