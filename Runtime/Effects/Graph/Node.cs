using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Effects.Graph
{
    public class Node<TTarget>
        where TTarget: Behaviour
    {
        public enum ExecutionStatus
        {
            None,
            Pending,
            Completed,
            Errored
        }
        public EffectsGraph<TTarget> graph;
        private IEffect currentEffect;
        protected readonly List<Node<TTarget>> children = new List<Node<TTarget>>();
        public Node<TTarget> parent { get; private set; }= null;
        private EffectCancellableOperationAwaiter activeEffectExecution;
        public ExecutionStatus status { get; private set; } = ExecutionStatus.None;

        public Node()
        {
        }

        public Node<TTarget> AddChildrenNode(Node<TTarget> node)
        {
            children.Add(node);
            node.SetParent(this);
            return node;
        }
        
        public Node<TTarget> CreateChildrenNode()
        {
            return AddChildrenNode(new Node<TTarget>());
        }

        public void SetParent(Node<TTarget> node)
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
        
        public async UniTask ApplyEffect<TProps>(IEffectWithProps<TTarget, TProps> effect, TProps props = default)
        {
            TryCancel();
            currentEffect = effect;
            activeEffectExecution = this.graph.channel.Add(effect, props);
            HandleEffectExecution().Forget();
            await this.graph.Execute();
        }

        public void TryCancel()
        {
            if (!activeEffectExecution.IsCompleted)
            {
                activeEffectExecution.Dispose();
            }
        }

        public void RemoveEffect(IEffect<TTarget> effect)
        {
            if (currentEffect == effect)
            {
                TryCancel();
            }
        }

        public async UniTask ReleaseEffect(IEffect<TTarget> effect)
        {
            RemoveEffect(effect);
            await graph.Execute();
        }

        public async UniTask Execute()
        {
            if (activeEffectExecution.status == EffectExecutionContext.Status.Pending)
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