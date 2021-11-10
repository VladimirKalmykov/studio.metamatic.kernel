using System;
using UnityEngine;

namespace Metamatic
{
    public abstract class LogicAsset: ScriptableObject
    {
        
    }

    public abstract class LogicAsset<TState> : LogicAsset
    where TState: struct
    {
        public abstract Logic Instantiate(MonoBehaviour parent, TState initialState);
    }
    
    public abstract class LogicAsset<TLogic, TLogicSettings, TState> : LogicAsset<TState>
        where TLogic: Logic<TLogicSettings, TState>
        where TLogicSettings: struct
        where TState: struct
    {
        [SerializeField]
        public TLogicSettings settings;
        public override Logic Instantiate(MonoBehaviour parent, TState initialState)
        {
            var logic = (TLogic) Activator.CreateInstance(typeof(TLogic),
                new object[] {parent, settings, initialState});
            return logic;
        }
    }
}