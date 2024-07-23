using System;
using System.Threading;

namespace Metamatic
{
    public class SmartCancellationTokenSource: IDisposable
    {
        private readonly CancellationTokenSource m_CancellationTokenSource = new ();
        public delegate bool Processor(CancellationToken cancellationToken, byte index);
        // ReSharper disable once InconsistentNaming
        public CancellationToken Token => m_CancellationTokenSource.Token;
        private CancellationTokenRegistration[] m_Registrations;
        public SmartCancellationTokenSource(Processor processor, params CancellationToken[] tokens)
        {
            m_Registrations = new CancellationTokenRegistration[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                var index = i;
                m_Registrations[i] = tokens[i].Register(() =>
                {
                    if (m_CancellationTokenSource.IsCancellationRequested) return;
                    if (processor(token, (byte)index))
                    {
                        m_CancellationTokenSource.Cancel();
                        Dispose();
                    }
                });
            }
        }

        public void Dispose()
        {
            if (m_Registrations != null) foreach (var registration in m_Registrations)
            {
                registration.Dispose();
            }
            m_Registrations = null;
            m_CancellationTokenSource.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}