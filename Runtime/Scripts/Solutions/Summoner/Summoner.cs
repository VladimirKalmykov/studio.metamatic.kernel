using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public readonly struct Summon<T>: IDisposable
    {
        // ReSharper disable once InconsistentNaming
        public static Summon<T> Empty => new Summon<T>(Token96Bit.Empty, default, null);
        private readonly Action<Token96Bit> m_Dispose;
        private readonly Token96Bit m_Token;
        #if DEBUG
        private readonly Container<bool> m_IsDisposed;
        #endif
        
        public bool isEmpty => m_Dispose == null;
        
        public T resource { get; }
        
        public Summon(Token96Bit token, T instance, Action<Token96Bit> dispose)
        {
            #if DEBUG
            m_IsDisposed = new Container<bool>(false);            
            #endif
            m_Token = token;
            this.resource = instance;
            m_Dispose = dispose;
        }

        public void Dispose()
        {
            #if DEBUG
            if (m_IsDisposed.value)
            {
                throw new InvalidOperationException("Summon already disposed");
            }
            m_IsDisposed.value = true;
            #endif
            m_Dispose?.Invoke(m_Token);
        }
        
        public static implicit operator T(Summon<T> summon)
        {
            return summon.resource;
        }

        public static implicit operator AsyncSummon<T>(Summon<T> syncSummon)
        {
            return new AsyncSummon<T>(syncSummon.m_Token, UniTask.FromResult(syncSummon.resource), syncSummon.m_Dispose);
        }
    }

    public readonly struct AsyncSummon<T> : IDisposable
    {
        // ReSharper disable once InconsistentNaming
        public static AsyncSummon<T> Empty => new AsyncSummon<T>(Token96Bit.Empty, UniTask.FromResult<T>(default), null);
        private readonly Action<Token96Bit> m_Dispose;
        private readonly Token96Bit m_Token;
#if DEBUG
        private readonly Container<bool> m_IsDisposed;
#endif
        
        public bool isEmpty => m_Dispose == null;
        
        public UniTask<T> task { get; }

        public AsyncSummon(Token96Bit token, UniTask<T> task, Action<Token96Bit> dispose)
        {
            #if DEBUG
            m_IsDisposed = new Container<bool>(false);
            #endif
            m_Token = token;
            this.task = task;
            m_Dispose = dispose;
        }

        public void Dispose()
        {
            #if DEBUG
            if (m_IsDisposed.value)
            {
                throw new InvalidOperationException("Summon already disposed");
            }
            m_IsDisposed.value = true;
            #endif
            m_Dispose?.Invoke(m_Token);
        }
    }

    public readonly struct Summoner<T>: ISummoner<T>
    {
        private readonly Func<Summon<T>> m_Summon;
        private readonly object m_Owner;
        private readonly int m_Identity;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="summon">Function that returns Summon</param>
        /// <param name="identity">Hash code of identity (any int that allows you to distinguish this adder from the rest. e.g. you can use GetHashCode of owner)</param>
        /// <param name="owner">Any object that can provide unique hash code</param>
        public Summoner(Func<Summon<T>> summon, int identity, object owner = null)
        {
            m_Summon = summon;
            m_Identity = identity;
            m_Owner = owner;
        }
        
        public Summon<T> Summon()
        {
            return m_Summon();
        }
        
        /* Get hash code for the summoner */
        public override int GetHashCode()
        {
            return m_Identity;
        }
        
        /* Check if the summoner is equal to another object */
        public override bool Equals(object obj)
        {
            /* Check if the object is a summoner */
            if (obj is Summoner<T> summoner)
            {
                /* Check if the owner and the summoner are equal */
                return m_Owner == summoner.m_Owner && m_Summon == summoner.m_Summon && m_Identity == summoner.m_Identity;
            }
            return false;
        }
    }
    
    
    public abstract class SummonerLegacy<T>
    {
        public Summon<T> Summon()
        {
            
            var (instance, token) = OnSummon();
            return new Summon<T>(token, instance, Banish);
        }

        private void Banish(Token96Bit token)
        {
            OnBanish(token);
        }
        
        protected abstract (T instance, Token96Bit token) OnSummon();
        protected abstract void OnBanish(Token96Bit token);
    }
}