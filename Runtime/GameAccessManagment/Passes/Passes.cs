using System;
using System.Collections.Generic;
using UnityEngine;

namespace Metamatic.GameAccessManagment
{
    [Serializable]
    public class Passes: Dictionary<string, Pass[]>, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector]
        private List<string> keyData = new List<string>();
        
        [SerializeField, HideInInspector]
        private List<Pass> valueData = new List<Pass>();
        public Passes Clone()
        {
            var clone = new Passes();
            foreach (var key in Keys)
            {
                clone[key] = this[key];
            }
            return clone;
        }
        
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            this.Clear();
            string currentKey = null;
            List<Pass> currentValue = new List<Pass>();
            for (int i = 0; i < this.keyData.Count && i < this.valueData.Count; i++)
            {
                if (this.keyData[i] != currentKey)
                {
                    if (currentKey != null)
                    {
                        this[currentKey] = currentValue.ToArray();
                    }
                    currentKey = this.keyData[i];
                    currentValue = new List<Pass>();
                }
                currentValue.Add(this.valueData[i]);
            }
            if (currentKey != null)
            {
                this[currentKey] = currentValue.ToArray();
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            this.keyData.Clear();
            this.valueData.Clear();

            foreach (var item in this)
            {
                foreach (var pass in item.Value)
                {
                    this.keyData.Add(item.Key);
                    this.valueData.Add(pass);
                }
            }
        }
    }
    
    
}