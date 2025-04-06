using System;
using Cysharp.Threading.Tasks;
using Metamatic.SaveSystem;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    
    public abstract class VirtualEntityLogic: ISavable
    {
        public abstract UniTaskVoid Run(Signal signal);
        public abstract object Save();
        public abstract UniTask Load(object data);
    }

    public abstract class EntityLogic : MonoBehaviour
    {
        [Version("1.0.0")]
        public class SaveData
        {
            public Type componentType;
            public Type entityLogicType;
            public object data;
            public SaveData(Type componentType, Type entityLogicType, object data)
            {
                this.componentType = componentType;
                this.entityLogicType = entityLogicType;
                this.data = data;
            }
        }

        private Entity m_Entity;

        protected abstract UniTaskVoid Run(Signal signal);
        private event Action OnDestroyed;

        protected virtual void Awake()
        {
            m_Entity = GetComponent<Entity>();
            OnDestroyed += m_Entity.WatchMaterializedAsync(Run);
        }
        
        public abstract EntityLogic.SaveData InternalSaveEntityLogic();
        public abstract UniTask InternalLoadEntityLogic(EntityLogic.SaveData data);
    }
    
    public abstract class EntityLogic<TVirtual>: EntityLogic where TVirtual: VirtualEntityLogic
    {
        public override EntityLogic.SaveData InternalSaveEntityLogic()
        {
            var saveData = this is ISavable savable ? savable.Save() : null;
            return new SaveData(this.GetType(), typeof(TVirtual), saveData);
        }

        public override UniTask InternalLoadEntityLogic(EntityLogic.SaveData data)
        {
            if (data.entityLogicType == typeof(TVirtual))
            {
                if (this is ISavable savable)
                {
                    return savable.Load(data.data);
                }
            }
            else
            {
                throw new Exception($"EntityLogic type mismatch: {data.entityLogicType} != {typeof(TVirtual)}");
            }
            return UniTask.CompletedTask;
        }
    }
}