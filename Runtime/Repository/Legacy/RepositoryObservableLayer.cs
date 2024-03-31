using System;
using System.Collections.Generic;
using UnityEngine;

namespace Metamatic
{
    public class RepositoryObservableLayer: ReadOnlyMicroState<RepositoryLayer>, IDisposable, IReserved
    {
        private static Dictionary<(RepositoryBehaviour, string), RepositoryObservableLayer> m_Pool = new();
        public static RepositoryObservableLayer Get(RepositoryBehaviour repositoryBehaviour, string name)
        {
            if (!m_Pool.TryGetValue((repositoryBehaviour, name), out var selector))
            {
                selector = new RepositoryObservableLayer(repositoryBehaviour, name);
                m_Pool.Add((repositoryBehaviour, name), selector);
            }
            return selector;
        }
        public static void ReleaseFromPool(RepositoryObservableLayer observableLayer)
        {
            Debug.Assert(!observableLayer.isReserved, "Repository layer selector is still reserved");
            if (m_Pool.TryGetValue((observableLayer.m_RepositoryBehaviour, observableLayer.name), out var s))
            {
                if (s == observableLayer)
                {
                    m_Pool.Remove((observableLayer.m_RepositoryBehaviour, observableLayer.name));
                }
            }
        }
        public string name { get; private set; }
        private RepositoryLayer m_Value;
        private RepositoryBehaviour m_RepositoryBehaviour;
        private bool m_IsReleased;
        private bool m_IsOwner;
        private readonly Reservations m_Reservations = new();
        /// <summary>
        /// Determines if this selector represents the original layer (true) or a proxy layer (false)
        /// </summary>
        public bool isOriginal => m_Value.name == name;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="repositoryBehaviour">Ref to repositoryBehaviour</param>
        /// <param name="name">Name of the layer</param>
        /// <param name="isOwner">Is owner of the layer - will release layer when be released itself</param>
        public RepositoryObservableLayer(RepositoryBehaviour repositoryBehaviour, string name, bool isOwner = false)
        {
            m_Reservations.OnReadyForRelease += Release;
            m_RepositoryBehaviour = repositoryBehaviour;
            SetLayerName(name);
            m_RepositoryBehaviour.OnLayerAdded += OnLayerAdded;
            m_RepositoryBehaviour.OnLayerRemoved += OnLayerRemoved;
        }

        ~RepositoryObservableLayer()
        {
            Release();
        }
        public override RepositoryLayer GetValue()
        {
            return m_Value;
        }

        public override void SetValue(RepositoryLayer value)
        {
            throw new Exception("Set value of RepositoryLayerSelector is out of concept");
        }
        
        protected void SetLayerName(string name)
        {
            this.name = name;
            foreach (var layer in m_RepositoryBehaviour.layers)
            {
                if (layer.name == name)
                {
                    m_Value = layer;
                    EmitValue(m_Value);
                    break;
                }
            }
            if (m_Value == null)
            {
                m_Value = m_RepositoryBehaviour.rootLayer;
            }
            EmitValue(m_Value);
        }
        
        private void OnLayerAdded(RepositoryLayer layer)
        {
            if (layer.name == name)
            {
                m_Value = layer;
                EmitValue(layer);
            }
        }
        
        private void OnLayerRemoved(RepositoryLayer layer)
        {
            if (layer.name == name)
            {
                m_Value = m_RepositoryBehaviour.rootLayer;
                EmitValue(m_Value);
            }
        }

        public bool isReserved  => m_Reservations.isReserved;

        public Action Acquire(object booker)
        {
            return m_Reservations.Acquire(booker);
        }
        
        public void Dispose()
        {
            if (!m_Reservations.isReserved)
            {
                Release();
            }
        }
        
        public void Dispose(object booker)
        {
            m_Reservations.Release(booker);
            if (!m_Reservations.isReserved)
            {
                Release();
            }
        }

        private void Release()
        {
            if (m_IsReleased)
            {
                return;
            }
            ReleaseFromPool(this);
            m_IsReleased = true;
            m_RepositoryBehaviour.OnLayerAdded -= OnLayerAdded;
            m_RepositoryBehaviour.OnLayerRemoved -= OnLayerRemoved;
            if (m_IsOwner)
            {
                m_RepositoryBehaviour.RemoveLayer(name);
            }
            GC.SuppressFinalize(this);
        }
        
        public RepositoryStore<T> GetStore<T>(string name) where T: struct
        {
            return m_RepositoryBehaviour.GetStore<T>(this, name);
        }
    }
    
    public class RepositoryObservableWritableLayer: RepositoryObservableLayer
    {
        public RepositoryObservableWritableLayer(RepositoryBehaviour repositoryBehaviour, string name = "") : base(repositoryBehaviour, name)
        {
        }

        public void SetName(string name)
        {
            SetLayerName(name);
        }
    }
}