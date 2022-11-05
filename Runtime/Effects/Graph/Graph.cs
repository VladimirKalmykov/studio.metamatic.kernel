using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Effects.Graph
{
    public class EffectsGraph
    {
        public readonly EffectsChannel channel;
        public Node entryNode { get; private set; }

        public EffectsGraph(EffectsChannel channel)
        {
            this.channel = channel;
            entryNode = new Node();
            entryNode.graph = this;
        }

        public Node AddNode(Node node)
        {
            return entryNode.AddChildrenNode(node);
        }

        public Node CreateChildrenNode()
        {
            return entryNode.AddChildrenNode(new Node());
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