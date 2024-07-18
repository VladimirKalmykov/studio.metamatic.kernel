using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Effects.Graph
{
    public class Node
    {
        public enum ExecutionStatus
        {
            None,
            Pending,
            Completed,
            Errored
        }
        public EffectsGraph graph;
        private IEffect currentEffect;
        protected readonly List<Node> children = new List<Node>();
        public Node parent { get; private set; }
        private EffectCancellableOperationAwaiter activeEffectExecution;
        public ExecutionStatus status { get; private set; } = ExecutionStatus.None;

        public Node()
        {
        }

        public Node AddChildrenNode(Node node)
        {
            children.Add(node);
            node.SetParent(this);
            return node;
        }
        
        public Node CreateChildrenNode()
        {
            return AddChildrenNode(new Node());
        }

        public void SetParent(Node node)
        {
            parent = node;
            graph = parent.graph;
        }
        
        /*[Obsolete("Apply propsless effect is obsolete", true)]
        public async UniTask ApplyEffect(IEffect<TTarget> effect)
        {
            try
            {
                TryCancel();
                currentEffect = effect;
                activeEffectExecution = this.graph.channel.Add(effect);
                HandleEffectExecution().Forget();
                await this.graph.Execute();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }*/
        
        public EffectCancellableOperationAwaiter ApplyEffect<TEffector, TEffectProps>(IEffect<TEffector, TEffectProps> effect, TEffectProps props = default) where TEffectProps: struct where TEffector: Effector
        {
            TryCancel();
            currentEffect = effect;
            activeEffectExecution = this.graph.channel.Add(effect, props);
            this.graph.Execute().Forget();
            return activeEffectExecution;
        }
        
        public EffectCancellableOperationAwaiter ApplyEffect<TEffector, TEffectProps, TResult>(IEffect<TEffector, TEffectProps, TResult> effect, TEffectProps props = default) where TEffectProps: struct where TEffector: Effector
        {
            TryCancel();
            currentEffect = effect;
            activeEffectExecution = this.graph.channel.Add(effect, props);
            this.graph.Execute().Forget();
            return activeEffectExecution;
        }

        public void TryCancel()
        {
            if (!activeEffectExecution.IsCompleted)
            {
                activeEffectExecution.Dispose();
            }
        }

        public void RemoveEffect(IEffect<Effector> effect)
        {
            if (currentEffect == effect)
            {
                TryCancel();
            }
        }

        public async UniTask ReleaseEffect(IEffect<Effector> effect)
        {
            RemoveEffect(effect);
            await graph.Execute();
        }

        public async UniTask Execute()
        {
            if (activeEffectExecution.status == IEffect.EffectExecutionContext.Status.Pending)
            {
                activeEffectExecution.Replay();
            }
            await UniTask.WhenAll(children.Select(node => node.Execute()));
        }

        private async UniTaskVoid HandleEffectExecution()
        {
            try
            {
                status = ExecutionStatus.Pending;
                await activeEffectExecution;
            } catch (OperationCanceledException)
            {
                // Skip
            }
            catch (Exception e)
            {
                status = ExecutionStatus.Errored;
                Debug.LogError(e);
                throw;
            }
            finally
            {
                status = ExecutionStatus.Completed;
            }
        }
    }
}