using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic
{
    [Obsolete("Use Entity instead", true)]
    public class DatabaseGameObjectEntity : DatabaseGameObjectContext, ISerializationCallbackReceiver
    {
        [LabelText("Name")]
        [SerializeField] private string m_EntityName = "";
        public DatabaseEntitySource entitySource = new DatabaseEntitySource("");
        public string entityName
        {
            get
            {
                return m_EntityName ?? gameObject.name;
            }
        }

        public void OnBeforeSerialize()
        {
            throw new System.NotImplementedException();
        }

        public void OnAfterDeserialize()
        {
            entitySource.SetValue(m_EntityName);
        }
    }
}