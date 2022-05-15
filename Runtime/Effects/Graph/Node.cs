using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Effects.Graph
{
    public class Node<TTarget>
        where TTarget: MonoBehaviour
    {
        public enum ExecutionStatus
        {
            None,
            Pending,
            Completed,
            Errored
        }
        public EffectsGraph<TTarget> graph;
        protected readonly List<Node<TTarget>> children = new List<Node<TTarget>>();
        public Node<TTarget> parent { get; private set; }= null;
        public EffectInstruction<TTarget> effectInstruction { get; private set; }
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

        public void SetEffect(IEffect<TTarget> effect)
        {
            effectInstruction = new EffectInstruction<TTarget>(effect);
        }
        
        public async UniTask ApplyEffect(IEffect<TTarget> effect)
        {
            effectInstruction = new EffectInstruction<TTarget>(effect);
            await graph.Execute();
        }

        public void SetEffect<TProps>(IEffectWithProps<TTarget, TProps> effect, TProps props = default)
        {
            effectInstruction = new EffectInstruction<TTarget, TProps>(effect, props);
        }
        
        public async UniTask ApplyEffect<TProps>(IEffectWithProps<TTarget, TProps> effect, TProps props = default)
        {
            effectInstruction = new EffectInstruction<TTarget, TProps>(effect, props);
            await graph.Execute();
        }

        public void SetEffect(EffectInstruction<TTarget> instruction)
        {
            effectInstruction = instruction;
        }
        
        public async UniTask ApplyEffect(EffectInstruction<TTarget> instruction)
        {
            effectInstruction = instruction;
            await graph.Execute();
        }

        public void RemoveEffect(IEffect<TTarget> effect)
        {
            if (effectInstruction.effect == effect)
            {
                effectInstruction = null;
            }
        }

        public async UniTask ReleaseEffect(IEffect<TTarget> effect)
        {
            RemoveEffect(effect);
            await graph.Execute();
        }

        public async UniTask Execute()
        {
            activeEffectExecution.Dispose();
            if (effectInstruction != null)
            {
                activeEffectExecution = effectInstruction.Run(graph.channel);
            }
            
            try
            {
                await UniTask.WhenAll(children.Select(node => node.Execute()));
                status = ExecutionStatus.Pending;
                await activeEffectExecution;
            }
            catch (OperationCanceledException)
            {
                // Skip
            }
            catch (Exception e)
            {
                status = ExecutionStatus.Errored;
                throw;
            }
            finally
            {
                status = ExecutionStatus.Completed;
            }
        }
    }
}