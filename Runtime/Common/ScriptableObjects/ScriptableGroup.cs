/**
Allow to store objects as scriptable collection
*/

namespace Metamatic {
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public interface IGroup<T> {
        T Pull();
        T[] PullMany(int count);
    }

    public class Group<T>: IGroup<T> {
        public string description {
            get; set;
        } = "Various group";
        public Type type {
            get;
        } = typeof(T);

        /// <summary>
        /// Contains only acting representatives of the group
        /// </summary>
        protected List<T> members = new List<T>();
        public T[] samples = new T[0];
        protected Func<T, T> pullTransformer;

        private T DefaultPullTransformer(T item) {
            return item;
        }

        public Group(T[] samples): this() {
           this.samples = samples;
           this.pullTransformer = DefaultPullTransformer;
        }

        public Group(T[] samples, Func<T, T> pullTransformer): this() {
            this.samples = samples;
            this.pullTransformer = pullTransformer;
        }

        public Group() {}

        public void AddMemeber(T member) {
            members.Add(member);
        }
        public void RemoveMemeber(T member) {
            members.Remove(member);
        }
        public T[] GetMembers() {
            return members.ToArray();
        }
        public T Pull() {
            return this.pullTransformer(samples.Length == 0
                ? default(T)
                : samples[UnityEngine.Random.Range(0, samples.Length - 1)]);
        }
        public T[] PullMany(int count = 2) {
            T[] items = new T[count];
            for (int i = 0; i < count; i++) {
                items[i] = Pull();
            }
            return items;
        }
        public T[] GetSamples() {
            return samples;
        }
    }

    public class ScriptableGroup<T>: ScriptableObject, IGroup<T> {
        [SerializeField]
        public string description {
            get; set;
        } = "Various group";
        public Type type {
            get; private set;
        } = typeof(T);

        /// <summary>
        /// Contains only acting representatives of the group
        /// </summary>
        protected List<T> members = new List<T>();
        public T[] samples = new T[0];

        public void AddMemeber(T member) {
            members.Add(member);
        }
        public void RemoveMemeber(T member) {
            members.Remove(member);
        }
        public T[] GetMembers() {
            return members.ToArray();
        }
        public T Pull() {
            return samples.Length == 0
                ? default(T)
                : samples[UnityEngine.Random.Range(0, samples.Length - 1)];
        }
        public T[] PullMany(int count = 2) {
            T[] items = new T[count];
            for (int i = 0; i < count; i++) {
                items[i] = Pull();
            }
            return items;
        }
        public T[] GetSamples() {
            return samples;
        }
    }
}