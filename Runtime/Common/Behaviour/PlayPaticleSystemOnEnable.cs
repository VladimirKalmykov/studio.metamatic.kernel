namespace Metamatic {
    using UnityEngine;

    [RequireComponent(typeof(ParticleSystem))]
    public class PlayPaticleSystemOnEnable: MonoBehaviour {
        ParticleSystem particles; 
        void Awake() {
            particles = GetComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = false;    
        }
        void OnEnable() {
            particles.Play();
        }
    }
}