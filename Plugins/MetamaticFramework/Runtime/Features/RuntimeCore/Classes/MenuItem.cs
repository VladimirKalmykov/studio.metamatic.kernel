using System;
using Cysharp.Threading.Tasks;

namespace Metamatic.Framework
{
    public class MenuItem : IDisposable
    {
        private Menu m_Parent;
        // ReSharper disable once UnassignedReadonlyField
        public readonly Observable<Menu> SubMenu = new();
        public ObservableReadOnly<bool> BoolState { get; private set; }
        public Action Trigger { get; private set; }

        public event Action OnChanged;
        
        public string name { get; }
        
        public Menu AddSubMenu()
        {
            if (SubMenu.value != null)
            {
                Console.Warning("MenuItem", "AddSubMenu", "SubMenu already exists! Why are you trying to add another one?");
                return SubMenu.value;
            }
            SubMenu.value = new Menu(this);
            return SubMenu.value;
        }
        
        public void RemoveSubMenu()
        {
            SubMenu.value = null;
        }
        
        public bool HasSubMenu => SubMenu.value != null;

        public MenuItem(Menu parent, string name, ObservableReadOnly<bool> boolState = null, Action trigger = null)
        {
            m_Parent = parent;
            this.name = name;
            BoolState = boolState;
            Trigger = trigger;
        }
        
        public DisposableAction WatchSubmenuLifecycle(Func<Menu, CancellationSignal, UniTaskVoid> action)
        {
            return SubMenu.WatchAsync(action);
        }
        
        public DisposableAction Subscribe(Action action)
        {
            OnChanged += action;
            return new DisposableAction(() => OnChanged -= action);
        }

        public void Dispose()
        {
            m_Parent.RemoveMenuItem(this);
        }

        public void SetDirty()
        {
            OnChanged?.Invoke();
            m_Parent?.SetDirty();
        }
    }
}