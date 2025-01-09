using System;

namespace Metamatic.Framework
{
    public readonly struct MenuItemConfig
    {
        public readonly string Name;
        public readonly Action Action;
        public readonly int SortOrder;
        public readonly Observable<bool> boolState;
            
        public MenuItemConfig(string name, Action action, int sortOrder, Observable<bool> boolState = null)
        {
            this.Name = name;
            this.Action = action;
            this.SortOrder = sortOrder;
            this.boolState = boolState;
        }
    }
}