using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic
{
    [Obsolete("Is obsolete, don't use it anymore", false)]
    public class DatabaseContextBehaviour : MonoBehaviour
    {
        public enum BranchNameSourceType
        {
            DatabaseCurrents,
            ParentContext,
            FixedValue
        }
        [SerializeField, PropertyOrder(0)] private bool useEntityAsSource = true;
        [HideIf("useEntityAsSource"), SerializeField, PropertyOrder(1)] private string entityName = "Default";
        [ShowIf("useEntityAsSource"), ShowInInspector, LabelText("Entity"), PropertyOrder(1)]
        private string nameFromTheEntityComponent =>
            entityComponent == null ? gameObject.name : entityComponent.fullname;
        
        [Tooltip("By the default the branch name will be taken from the database current branch. You can specify another branch name source")]
        [SerializeField, PropertyOrder(2)] private BranchNameSourceType branchNameSource = BranchNameSourceType.DatabaseCurrents;
        [ShowIf("@this.branchNameSource == BranchNameSourceType.FixedValue"), PropertyOrder(3)]
        public string branchName = "";
        
        #if UNITY_EDITOR
        [ShowIf("@this.branchNameSource == BranchNameSourceType.DatabaseCurrents"), ShowInInspector, LabelText("Current branch"), PropertyOrder(3)]
        // ReSharper disable once InconsistentNaming
        private string editor_databaseCurrentBranchName => Database.currentBranchSource.value;
        [ShowIf("@this.branchNameSource == BranchNameSourceType.ParentContext"), ShowInInspector, LabelText("Context branch"), PropertyOrder(4)]
        // ReSharper disable once InconsistentNaming
        private string editor_parentContexttBranchName => branchSource == null ? "" : branchSource.value;
        #endif

        private Entity m_EntityComponent;
        public Entity entityComponent =>
            m_EntityComponent == null ? (m_EntityComponent = GetComponentInParent<Entity>(true)) : m_EntityComponent;
        
        [ShowIf("@this.branchNameSource == BranchNameSourceType.ParentContext"), ShowInInspector, ReadOnly, LabelText("Context"), PropertyOrder(3)]
        public DatabaseContextBehaviour parentContext => transform.parent != null
            ? transform.parent.GetComponentInParent<DatabaseContextBehaviour>()
            : null;
        
        private DatabaseBranchSource m_BrancheSource = null;
        private DatabaseEntitySource m_EntitySource = null;

        public DatabaseBranchSource branchSource =>
            m_BrancheSource ??= new DatabaseBranchSource(GetBranchNameSource());
        public DatabaseEntitySource entitySource => m_EntitySource ??= new DatabaseEntitySource(GetEntityNameSource());

        private MicroStateReadOnly<string> GetEntityNameSource()
        {
            if (useEntityAsSource)
            {
                if (entityComponent == null)
                {
                    return new MicroState<string>(gameObject.name);
                }
                else
                {
                    return entityComponent.observableName;
                }
            }
            else
            {
                return new MicroState<string>(entityName);
            }
        }

        private MicroStateReadOnly<string> GetBranchNameSource()
        {
            switch (branchNameSource)
            {
                case BranchNameSourceType.DatabaseCurrents:
                    return Database.currentBranchSource;
                    break;
                case BranchNameSourceType.ParentContext:
                    if (parentContext)
                    {
                        return parentContext.branchSource;
                    }
                    else
                    {
                        return Database.currentBranchSource;
                    }
                    break;
                case BranchNameSourceType.FixedValue:
                    return new MicroStateGetter<string>(() =>
                    {
                        return branchName.Length > 0 ? branchName : Database.currentBranchSource.value;
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Transform m_CachedParent;
        void FixedUpdate()
        {
            if ((useEntityAsSource || branchNameSource == BranchNameSourceType.ParentContext) && m_CachedParent != transform.parent)
            {
                m_CachedParent = transform.parent;
                UpdateInheritanceValuesOnParentChanged();
            }
        }
        
        /// <summary>
        /// This method should be called each time parent did change
        /// </summary>
        private void UpdateInheritanceValuesOnParentChanged()
        {
            if (useEntityAsSource && m_EntitySource != null)
            {
                var nextEntity = GetComponentInParent<Entity>();
                if (nextEntity != m_EntityComponent)
                {
                    m_EntitySource.SetProxy(GetEntityNameSource());
                }
            }

            if (branchNameSource == BranchNameSourceType.ParentContext && m_BrancheSource != null)
            {
                m_BrancheSource.SetProxy(GetBranchNameSource());
            }
        }
    }
}