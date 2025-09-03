using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.Systems;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Metamatic
{
    public class ThemeService : Service
    {
        [Serializable]
        public struct Entry
        {
            public ThemeSchema schema;
            public AssetReferenceT<ThemeBase> themeRef;
#if UNITY_EDITOR
            public ThemeBase editorTheme;
#endif
        }

        [SerializeField] public List<Entry> entries = new();

        private readonly Dictionary<ThemeSchema, Observable<ThemeBase>> _map = new();

        protected override async UniTask RunService(Ready ready, Signal signal)
        {
            foreach (var entry in entries)
            {
                var obs = new Observable<ThemeBase>();
                _map[entry.schema] = obs;

#if UNITY_EDITOR
                if (!Application.isPlaying && entry.editorTheme)
                {
                    obs.value = entry.editorTheme;
                    continue;
                }
#endif
                var summon = ResourcesSystem.Summon<ThemeBase>(entry.themeRef);
                _ = LoadOne(summon, obs, signal);
            }

            await ready();
        }

        private async UniTaskVoid LoadOne(AsyncSummon<ThemeBase> summon, Observable<ThemeBase> obs,
            Signal s)
        {
            try
            {
                using var sm = summon;
                obs.value = await sm.Wait(s);
            }
            catch
            {
                /* handle */
            }
        }

        public DisposableAction WatchTheme(ThemeSchema schema, Action<ThemeBase> action)
        {
            if (!_map.TryGetValue(schema, out var obs)) return default;
            return obs.WatchAsync(async (t, sig) =>
            {
                if (t == null) return;
                using (t.Watch(() => action(t)))
                    await sig;
            });
        }

        public async UniTask SetTheme(ThemeSchema schema, AssetReferenceT<ThemeBase> reference,
            Signal s = default)
        {
            if (!_map.TryGetValue(schema, out var obs)) return;
            using var summon = ResourcesSystem.Summon<ThemeBase>(reference);
            obs.value = await summon.Wait(s);
        }
    }
}