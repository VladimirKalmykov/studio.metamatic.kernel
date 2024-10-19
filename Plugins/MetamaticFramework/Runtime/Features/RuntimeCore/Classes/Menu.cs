using System;
using Cysharp.Threading.Tasks;

namespace Metamatic.Framework
{
    public class Menu
    {
        private MenuItem m_ParentMenuItem;
        private ObservableList<MenuItem> m_MenuItems = new ObservableList<MenuItem>();

        public event Action OnChanged;
        
        public Menu()
        {
            
        }
        
        public Menu(MenuItem parentMenuItem)
        {
            m_ParentMenuItem = parentMenuItem;
        }
        
        public MenuItem CreateMenuItem(string name, ObservableReadOnly<bool> toggleState = null, Action onClick = null)
        {
            var menuItem = new MenuItem(this, name, toggleState, onClick);
            m_MenuItems.Add(menuItem);
            SetDirty();
            return menuItem;
        }
        
        public void RemoveMenuItem(MenuItem menuItem)
        {
            m_MenuItems.Remove(menuItem);
            SetDirty();
        }
        
        public void SetDirty()
        {
            OnChanged?.Invoke();
            m_ParentMenuItem?.SetDirty();
        }
        
        /// <summary>
        /// Select menu item by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public MenuItem Select(string name)
        {
            foreach (var menuItem in m_MenuItems)
            {
                if (menuItem.name == name)
                {
                    return menuItem;
                }
            }

            return null;
        }
        
        public DisposableAction WatchEachItemLifecycle(Func<MenuItem, int, CancellationSignal, UniTaskVoid> onEachItem)
        {
            return m_MenuItems.WatchEachLifecycle(onEachItem);
        }
    }
}