using System;
using Metamatic;
using UnityEngine.Localization;

namespace Extensions
{
    public static class LocalizationExtensions
    {
        public static DisposableAction Watch(this LocalizedString localizedString, LocalizedString.ChangeHandler handler)
        {
            localizedString.StringChanged += handler;
            
            /* Immediate update */
            if (localizedString.IsEmpty)
            {
                handler.Invoke("");
            }
            else
            {
                handler.Invoke(localizedString.GetLocalizedString());
            }
            
            return DisposableAction.Create(() =>
            {
                localizedString.StringChanged -= handler;
            });
        }
    }
}