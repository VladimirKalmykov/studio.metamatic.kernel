using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic.EditorTools;
using Metamatic.UIToolKit;
using UnityEditor;
using UnityEngine.UIElements;

namespace Metamatic.Inspector.Windows.PoolsInspector
{
    public class PoolsInspectorWindow: AsyncEditorWindow
    {
        [MenuItem ("Window/Metamatic/Pools Inspector")]
        public static void ShowWindow () {
            GetWindow(typeof(PoolsInspectorWindow), false, "Pools Inspector");
        }
        
        private readonly ObservableList<IDevPool> m_PoolsData = new ObservableList<IDevPool>();

        protected override async UniTaskVoid EnableAsync(Func<UniTask> ready, Signal signal)
        {
            var allPools = new List<IDevPool>();
            var paramLessPools = ParamLessPool.Pools;
            foreach (var pool in paramLessPools)
            {
                allPools.Add(pool);
            }
            
            var configurablePools = ConfigurablePool.Pools;
            foreach (var pool in configurablePools)
            {
                allPools.Add(pool);
            }
            
            m_PoolsData.Synchronize(allPools);
            
            ParamLessPool.OnPoolAdded += AddToData;
            ParamLessPool.OnPoolRemoved += RemoveFromData;
            ConfigurablePool.OnPoolAdded += AddToData;
            ConfigurablePool.OnPoolRemoved += RemoveFromData;
            await ready();
            ParamLessPool.OnPoolAdded -= AddToData;
            ParamLessPool.OnPoolRemoved -= RemoveFromData;
            ConfigurablePool.OnPoolAdded -= AddToData;
            ConfigurablePool.OnPoolRemoved -= RemoveFromData;
        }
        
        private void AddToData(IDevPool pool)
        {
            m_PoolsData.Add(pool);
        }
        
        private void RemoveFromData(IDevPool pool)
        {
            m_PoolsData.Remove(pool);
        }

        private Table<IDevPool> m_Table;

        protected override void OnCreateGUI()
        {
            m_Table = new Table<IDevPool>(new Table<IDevPool>.Column[]
            {
                new()
                {
                    Header = "Name",
                    Accessor = pool => new Label(pool.displayName),
                    Width = new StyleLength(new Length(60, LengthUnit.Percent))
                },
                new()
                {
                    Header = "In pool",
                    Accessor = pool => new Tracker<Label>(new Label(pool.countInPool.ToString()), (label) => label.text = pool.countInPool.ToString()),
                    Width = new StyleLength(new Length(20, LengthUnit.Percent))
                },
                new()
                {
                    Header = "Out pool",
                    Accessor = pool => new Tracker<Label>(new Label(pool.countOutPool.ToString()), (label) => label.text = pool.countOutPool.ToString()),
                    Width = new StyleLength(new Length(20, LengthUnit.Percent))
                }
            }, m_PoolsData);
            rootVisualElement.Add(m_Table);
        }
    }
}