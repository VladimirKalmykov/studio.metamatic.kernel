using Cysharp.Threading.Tasks;
using Metamatic.SaveSystem;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    public class TimeService: Service, ISavable
    {
        [SerializeField] private float m_DefaultTime = 0f;
        [SerializeField] private float m_DefaultTimeScale = 1f;
        
        [Version("1.0.0")]
        public struct SaveData
        {
            public float time;
            public float timeScale;
        }

        private float m_CustomTime = 0f;
        private float m_SystemTimeAtCustomTimeLastChange;
        private float m_TimeScale = 1f;
        
        public float time => m_CustomTime + (Time.time - m_SystemTimeAtCustomTimeLastChange);

        public float timeScale
        {
            get => m_TimeScale;
            set {
                m_TimeScale = value;
                Time.timeScale = value;
            }
        }

        private bool m_WasLoaded;
        
        protected override UniTask RunService(Ready ready, Signal signal)
        {
            if (!m_WasLoaded)
            {
                m_CustomTime = m_DefaultTime;
                m_SystemTimeAtCustomTimeLastChange = Time.time;
                timeScale = m_DefaultTimeScale;
            }
            return ready();
        }

        public object Save()
        {
            return new SaveData
            {
                time = time,
                timeScale = timeScale
            };
        }

        public UniTask Load(object data)
        {
            var saveData = (SaveData) data;
            m_CustomTime = saveData.time;
            m_SystemTimeAtCustomTimeLastChange = Time.time;
            timeScale = saveData.timeScale;
            m_WasLoaded = true;
            return UniTask.CompletedTask;
        }
    }
}