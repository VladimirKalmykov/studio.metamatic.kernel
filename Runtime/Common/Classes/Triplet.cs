using System;
using UnityEngine;

namespace Metamatic {
    public class Triplet<T> {
        public Triplet(T x, T y, T z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public Triplet() {}
        public T x;
        public T y;
        public T z;

        /// <summary>
        /// Convert Triplet to Unity Vector3 if it's of one of type: double, int, long, short, byte, decomal, float. If it will returns Vector3.zero.true 
        /// </summary>
        /// <returns></returns>
        public Vector3 ToVector3() {
            Type t = typeof(T);
            if (t == typeof(float) || t == typeof(double) || t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte) || t == typeof(decimal)) {
                return new Vector3(
                    (float)Convert.ChangeType(this.x, typeof(float)),
                    (float)Convert.ChangeType(this.y, typeof(float)),
                    (float)Convert.ChangeType(this.z, typeof(float))
                );
            }
            return Vector3.zero;
        }
        
        /// <summary>
        /// Transform each vertex of triplet with user mapper and returns new triplet
        /// </summary>
        /// <param name="mapper"></param>
        /// <returns></returns>
        public Triplet<T> Map(Func<T, T> mapper) {
            return new Triplet<T>(
                mapper(this.x),
                mapper(this.y),
                mapper(this.z)
            );
        }

        /// <summary>
        /// Convert into an array
        /// </summary>
        /// <returns></returns>
        public T[] ToArray() {
            return new T[] { this.x, this.y, this.z };
        }
    }
}