using System;

namespace Metamatic {
    [Serializable]
    public struct MinMaxValue {
        public enum Mode {
            Constant,
            Range
        };
        public Mode mode;
        public float constant;
        public float constantMin;
        public float constantMax;

        public MinMaxValue(float min, float max) {
            this.mode = Mode.Range;
            this.constant = min;
            this.constantMin = min;
            this.constantMax = max;
        }

        public MinMaxValue(float value) {
            this.mode = Mode.Constant;
            this.constant = value;
            this.constantMin = value;
            this.constantMax = value;
        }

        public float GetValue() {
            if (mode == Mode.Constant) {
                return constant;
            } else if (mode == Mode.Range) {
                return UnityEngine.Random.Range(constantMin, constantMax);
            }
            return 0f;
        }

        public float GetMaxValue() {
            if (mode == Mode.Constant) {
                return constant;
            } else if (mode == Mode.Range) {
                return constantMax;
            }
            return 0f;
        }

        public float GetMinValue() {
            if (mode == Mode.Constant) {
                return constant;
            } else if (mode == Mode.Range) {
                return constantMin;
            }
            return 0f;
        }
    }
}