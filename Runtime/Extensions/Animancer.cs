using Animancer;
using UnityEngine;

namespace Metamatic
{
    public static class Animancer
    {
        public static AnimancerState Replay(this AnimancerComponent animancerComponent, AnimationClip clip)
        {
            var state = animancerComponent.Play(clip);
            state.Time = 0;
            return state;
        }
        
        public static AnimancerState TryReplay(this AnimancerComponent animancerComponent, object key)
        {
            var state = animancerComponent.TryPlay(key);
            if (state == null)
            {
                return null;
            }
            animancerComponent.Play(state);
            return state;
        }
        
        public static AnimancerState TryReplay(this NamedAnimancerComponent animancerComponent, object key)
        {
            var state = animancerComponent.TryPlay(key);
            if (state == null)
            {
                return null;
            }
            state.Time = 0;
            return state;
        }
    }
}