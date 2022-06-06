using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Effects.Graph
{
    public class EffectsGraph<TTarget>
    where TTarget: Behaviour
    {
        public readonly EffectsChannel<TTarget> channel;
        public Node<TTarget> entryNode { get; private set; }

        public EffectsGraph(EffectsChannel<TTarget> channel)
        {
            this.channel = channel;
            entryNode = new Node<TTarget>();
            entryNode.graph = this;
        }

        public Node<TTarget> AddNode(Node<TTarget> node)
        {
            return entryNode.AddChildrenNode(node);
        }

        public Node<TTarget> CreateChildrenNode()
        {
            return entryNode.AddChildrenNode(new Node<TTarget>());
        }
        
        public async UniTask Execute()
        {
            try
            {
                await entryNode.Execute();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw e;
            }
        }
    }
}