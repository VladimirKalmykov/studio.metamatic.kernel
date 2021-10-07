namespace Metamatic {
    using System.Linq;
    using UnityEngine;

    /* Template is a object object's provider, that uses pool pattern and can reproduce the same object meny times without intantiation */
    [CreateAssetMenu(fileName = "ScriptableAudioClipFactory", menuName = "Metamatic/Factory/Audio Clip", order = 1)]
    public class ScriptableAudioClipFactory: ScriptableObject, IGroup<AudioClip> {
        public AudioClip[] clips = new AudioClip[0];
        public virtual AudioClip Pull() {
            return clips.Length > 0 ? clips[UnityEngine.Random.Range(0, clips.Length)] : null;
        }

        public virtual AudioClip[] PullMany(int count) {
            return (new AudioClip[count]).Select(item => Pull()).ToArray();
        }
    }
}