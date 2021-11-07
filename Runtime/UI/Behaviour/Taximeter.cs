namespace Metamatic.UI
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using Metamatic;
    using Sirenix.OdinInspector;

    /// <summary>
    /// Print float value with taximeter effect
    /// </summary>
    public class Taximeter : MonoBehaviour
    {
        public class GrowUpAutomation: Automation.Script {
            public struct Props {
                public float step;
                public float startValue;
                public float nextValue;
                public Action<float> Update;
                public Action Complete;
            }
            protected void OnRun(Automation.Runtime runtime, Props props)
            {
                var intialValue = props.startValue;
                runtime.EveryMilliseconds((int)(props.step * 1000f), () =>
                {
                    var nextValue = Mathf.Lerp(props.startValue, props.nextValue, runtime.timer.ratio);
                    Debug.Log($"Update text: {nextValue}");
                    props.Update(nextValue);
                });

                runtime.OnComplete += () => {
                    props.Update(props.nextValue);
                    props.Complete();
                };
            }
        }
        [System.Serializable]
        public class StringEvent: UnityEvent<string> {}
        public float defaultValue = 0f;
        public float defaultNextValue = 0f;
        public string format = "0.00";
        [Tooltip("String template with placeholder {n}")]
        public string template = "{n}";
        public float duration = 1f;
        public float step = 0.1f;
        public StringEvent onTextChange = new StringEvent();
        public UnityEvent OnComplete = new UnityEvent();
        public bool autoPlay = true;
        public bool realTime = true;
        private AutomationTrack track;
        private float m_Value = 0f;
        [ShowInInspector]
        public float value {
            get {
                return m_Value;
            }
            set {
                m_Value = value;
                text = template.Replace("{n}", value.ToString(format));
            }
        }
        public string text {
            set {
                onTextChange.Invoke(value);
            }
        }
        private float n_NextValue = 0f;
        public float nextValue {
            set {
                n_NextValue = value;
                Play();
            }
            get {
                return n_NextValue;
            }
        }

        private void OnEnable() {
            track = this.GetAutomationTrack("FloatClockText");
            track.SetDefaultConfig(new Automation.Script.Config {
                overlapMode = Automation.OverlapMode.Cancel,
                realTime = realTime
            });
            n_NextValue = defaultNextValue;
            if (autoPlay) {
                Play();
            }
        }

        [Button]
        private void Play() {
            track.Run<GrowUpAutomation>(new GrowUpAutomation.Props {
                step = step,
                startValue = value,
                nextValue = nextValue,
                Update = SetValue,
                Complete = SetComplete
            }, duration, default, (GrowUpAutomation script) =>
            {
                script.realTime = realTime;
            });
        }

        private void SetValue(float value) {
            Debug.Log($"Value: {value}");
            this.value = value;
        }

        private void SetComplete()
        {
            OnComplete.Invoke();
        }
    }

}
