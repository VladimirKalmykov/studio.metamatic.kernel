using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic
{
    [Obsolete("Entity is obsolete, don't use it anymore", false)]
    public class Entity : Element
    {
        [ShowInInspector, BoxGroup("Entity"), PropertyOrder(0)]
        public string fullname {
            get
            {
                var className = entityClass != null ? entityClass.name : "";
                var localName = useObjectName
                    ? gameObject.name
                    : (useObjectNameAsSub ? $"{name}/{gameObject.name}" : name);
                
                return className.Length > 0 ? $"{className}/{localName}" : localName;
            }
        }
        [BoxGroup("Entity"), PropertyOrder(1)]
        [SerializeField] private bool useObjectName;
        [BoxGroup("Entity"), PropertyOrder(1)]
        [HideIf("useObjectName"), SerializeField] private bool useObjectNameAsSub = true;
        [BoxGroup("Entity")]
        [HideIf("useObjectName"), SerializeField, PropertyOrder(2)] private new string name = "Unknown";
        [BoxGroup("Entity")]
        [ShowInInspector, ReadOnly, ShowIf("useObjectName"), LabelText("Name"), PropertyOrder(2)] public string gameObjectName => gameObject.name;
        [BoxGroup("Entity")]
        [SerializeField, PropertyOrder(3)] private EntityClass entityClass;

        private MicroStateReadOnly<string> n_ObservableValue;
        public MicroStateReadOnly<string> observableName => n_ObservableValue ??= new MicroStateGetter<string>(() => this.fullname);
    }
}