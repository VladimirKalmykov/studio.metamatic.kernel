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
        
        public void Add(string key, Pass value)
        {
            if (TryGetValue(key, out var passes))
            {
                var list = new List<Pass>(passes);
                list.Add(value);
                this[key] = list.ToArray();
            }
            else
            {
                this[key] = new[] {value};
            }
        }
        
        public bool Remove(string key, int count)
        {
            if (TryGetValue(key, out var passes))
            {
                var list = new List<Pass>(passes);
                if (list.Count == 0)
                {
                    return false;
                }
                var firstPass = list[0];
                if (firstPass.count < count)
                {
                    return false;
                }
                if (firstPass.count > count)
                {
                    firstPass.count -= count;
                    list[0] = firstPass;
                }
                else
                {
                    list.RemoveAt(0);
                }
                if (list.Count == 0)
                {
                    Remove(key);
                }
                else
                {
                    this[key] = list.ToArray();
                }
                return true;
            }

            return false;
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