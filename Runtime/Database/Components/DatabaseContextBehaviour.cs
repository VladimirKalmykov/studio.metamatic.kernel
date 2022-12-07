using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic
{
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
        #endif

        private Entity m_EntityComponent;
        public Entity entityComponent =>
            m_EntityComponent == null ? (m_EntityComponent = GetComponent<Entity>()) : m_EntityComponent;

        private DatabaseBranchSource m_BrancheSource = null;
        private DatabaseEntitySource m_EntitySource = null;

        public DatabaseBranchSource branchSource =>
            m_BrancheSource ??= new DatabaseBranchSource(GetBranchNameSource());
        public DatabaseEntitySource entitySource => m_EntitySource ??= new DatabaseEntitySource(GetEntityNameSource());

        private ObservableValue<string> GetEntityNameSource()
        {
            if (useEntityAsSource)
            {
                if (entityComponent == null)
                {
                    return new ObservableWritableValue<string>(gameObject.name);
                }
                else
                {
                    return entityComponent.observableName;
                }
            }
            else
            {
                return new ObservableWritableValue<string>(entityName);
            }
        }

        private ObservableValue<string> GetBranchNameSource()
        {
            switch (branchNameSource)
            {
                case BranchNameSourceType.DatabaseCurrents:
                    return Database.currentBranchSource;
                    break;
                case BranchNameSourceType.ParentContext:
                    var parentContext = transform.parent != null
                        ? transform.parent.GetComponent<DatabaseContextBehaviour>()
                        : null;
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
                    return new ObservableGettableValue<string>(() =>
                    {
                        return branchName.Length > 0 ? branchName : Database.currentBranchSource.value;
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}