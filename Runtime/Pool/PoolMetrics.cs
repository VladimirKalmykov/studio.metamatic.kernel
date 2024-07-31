namespace Metamatic
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;

    public class PoolMetrics
    {
        public struct Stats
        {
            public int peakCount { get; set; }
            public int minCount { get; set; }
            public int recommendedCapacity { get; set; }
            public int recommendedLimit { get; set; }
        }
        private readonly int m_CheckResultInterval;
        private readonly List<int> m_Metrics = new List<int>();
        private int m_CountInPool;
        private int m_CountOutOfPool;
        private bool m_IsRunning;
        private readonly object m_Lock = new object();

        public Action<Stats> onResult { get; set; }

        public PoolMetrics(int checkResultInterval)
        {
            m_CheckResultInterval = checkResultInterval;
            m_IsRunning = true;
            AnalyzeMetricsPeriodically().Forget();
        }

        public void AddMetric(int countInPool, int countOutOfPool)
        {
            lock (m_Lock)
            {
                m_Metrics.Add(countOutOfPool);
                m_CountInPool = countInPool;
                m_CountOutOfPool = countOutOfPool;
            }
        }

        private async UniTaskVoid AnalyzeMetricsPeriodically()
        {
            while (m_IsRunning)
            {
                await UniTask.Delay(m_CheckResultInterval);

                lock (m_Lock)
                {
                    if (m_Metrics.Count == 0)
                        continue;

                    int peakCount = int.MinValue;
                    int minCount = int.MaxValue;
                    int totalCount = 0;

                    foreach (var count in m_Metrics)
                    {
                        if (count > peakCount)
                            peakCount = count;
                        if (count < minCount)
                            minCount = count;
                        totalCount += count;
                    }

                    int recommendedCapacity = (int)Math.Ceiling(totalCount / (double)m_Metrics.Count);
                    int recommendedLimit =
                        (int)Math.Ceiling(peakCount * 1.2); // Example of setting limit 20% higher than peak

                    Stats stats = new Stats
                    {
                        peakCount = peakCount,
                        minCount = minCount,
                        recommendedCapacity = recommendedCapacity,
                        recommendedLimit = recommendedLimit
                    };

                    onResult?.Invoke(stats);
                    m_Metrics.Clear();
                }
            }
        }

        public void Stop()
        {
            m_IsRunning = false;
        }
    }
}