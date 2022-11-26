using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Effects.Graph
{
    public class EffectsGraph
    {
        public readonly EffectsChannel channel;
        public Node node { get; private set; }

        public EffectsGraph(EffectsChannel channel)
        {
            this.channel = channel;
            node = new Node();
            node.graph = this;
        }

        public Node AddNode(Node node)
        {
            return this.node.AddChildrenNode(node);
        }

        public Node CreateChildrenNode()
        {
            return node.AddChildrenNode(new Node());
        }
        
        public async UniTask Execute()
        {
            try
            {
                await node.Execute();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw e;
            }
        }
    }
}