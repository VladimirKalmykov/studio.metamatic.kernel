using System;
using UnityEngine.Localization;

namespace Metamatic
{
    public class LocalizedStringSubscription: IDisposable
    {
        private readonly LocalizedString m_LocalizedString;
        private readonly LocalizedString.ChangeHandler m_OnValueChange;
        private bool m_Disposed;
        public LocalizedStringSubscription(LocalizedString localizedString, LocalizedString.ChangeHandler onValueChange)
        {
            m_LocalizedString = localizedString;
            m_OnValueChange = onValueChange;
            localizedString.StringChanged += onValueChange;
            onValueChange.Invoke(localizedString.GetLocalizedString());
        }

        public void Dispose()
        {
            if (m_Disposed)
            {
                return;
            }
            m_Disposed = true;
            m_LocalizedString.StringChanged -= m_OnValueChange;
        }
    }
}