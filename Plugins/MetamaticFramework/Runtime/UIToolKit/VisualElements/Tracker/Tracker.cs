using System;
using Cysharp.Threading.Tasks;
using Metamatic.UIElements;
using UnityEngine.UIElements;

namespace Metamatic.UIToolKit
{
    /// <summary>
    /// Allows to update a VisualElement with data from a source
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    /// <typeparam name="TData"></typeparam>
    public class Tracker<TElement, TData>: AsyncVisualElement
    where TElement: VisualElement
    {
        private TElement m_Target;
        private TData m_Source;
        private Action<TElement, TData> m_Update;
        private PlayerLoopTiming m_Timing;
        public Tracker(TElement target, TData source, Action<TElement, TData> update, PlayerLoopTiming timing = PlayerLoopTiming.LastUpdate)
        {
            m_Target = target;
            m_Source = source;
            m_Update = update;
            Add(target);
        }
        public override async UniTask OnAttachedAsync(CancellationSignal detachSignal)
        {
            UniTask.WaitUntil(() =>
            {
                m_Update(m_Target, m_Source);
                return detachSignal.IsCancellationRequested;
            }, m_Timing);
            await detachSignal;
        }
    }
    
    public class Tracker<TElement>: AsyncVisualElement
        where TElement: VisualElement
    {
        private TElement m_Target;
        private Action<TElement> m_Update;
        private PlayerLoopTiming m_Timing;
        public Tracker(TElement target, Action<TElement> update, PlayerLoopTiming timing = PlayerLoopTiming.LastUpdate)
        {
            m_Target = target;
            m_Update = update;
            Add(target);
        }
        public override async UniTask OnAttachedAsync(CancellationSignal detachSignal)
        {
            UniTask.WaitUntil(() =>
            {
                m_Update(m_Target);
                return detachSignal.IsCancellationRequested;
            }, m_Timing);
            await detachSignal;
        }
    }
}