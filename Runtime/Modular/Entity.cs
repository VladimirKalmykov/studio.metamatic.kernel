using UnityEngine;

namespace Metamatic
{
    public class Entity : Element, ISerializationCallbackReceiver
    {
        [SerializeField] private new string name = "Unknown";
        [SerializeField] private EntityClass entityClass;

        public readonly ObservableValue<string> observableName = new ObservableValue<string>();


        public void OnBeforeSerialize()
        {
            //
        }

        public void OnAfterDeserialize()
        {
            observableName.SetValue(name);
        }
    }
}