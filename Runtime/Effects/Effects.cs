using System.Collections.Generic;
using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// Contains all the effects applied to the object
    /// </summary>
    public class Effects: MonoBehaviour
    {
        private readonly HashSet<EffectCast> m_ActiveEffects = new ();
        
        public void AddEffect(EffectCast effect, GameObject caster, GameObject target)
        {
            m_ActiveEffects.Add(effect);
            effect.cancellationSignal.Register(() => m_ActiveEffects.Remove(effect));
        }
    }
}