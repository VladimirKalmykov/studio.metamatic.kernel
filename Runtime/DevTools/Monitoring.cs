using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.DevTools
{
    public static class Monitoring
    {
       
        public class MonitoringItem {
            public static MonitoringItem Create(string group, string key, Func<object> accessor, Type type, PlayerLoopTiming timing)
            {
                var item = ParamLessPool<MonitoringItem>.GetShared();
                item.Initialize(group, key, accessor, type, timing);
                return item;
            }
            private int m_Version;
            private CancellationSignalSource m_CancellationSignalSource;
            public string Group;
            public string Key;
            public Type Type;
            public PlayerLoopTiming Timing;
            public Func<object> Accessor;
            public object Value;
            // ReSharper disable once InconsistentNaming
            public CancellationSignal CancellationSignal => m_CancellationSignalSource.Signal;

            public MonitoringItem()
            {
                
            }
            
            private void Initialize(string group, string key, Func<object> accessor, Type type, PlayerLoopTiming timing)
            {
                Group = group;
                Key = key;
                Accessor = accessor;
                Type = type;
                Timing = timing;
                var version = m_Version;
                m_CancellationSignalSource = CancellationSignalSource.Create();
                UniTask.WaitWhile(() =>
                {
                    Value = accessor();
                    return version == m_Version;
                }, timing: timing).Forget();
            }
            
            public void Return()
            {
                m_CancellationSignalSource.TryCancel();
                unchecked
                {
                    m_Version++;
                }
                ParamLessPool<MonitoringItem>.ReturnShared(this);
            }
        }

        public class Group
        {
            public static Group Create(string name)
            {
                var group = ParamLessPool<Group>.GetShared();
                group.Initialize(name);
                return group;
            }
            public string Name;
            public readonly Dictionary<string, List<MonitoringItem>> Items = new Dictionary<string, List<MonitoringItem>>();
            private CancellationSignalSource m_CancellationSignalSource;
            public CancellationSignal CancellationSignal => m_CancellationSignalSource.Signal;
            public int count => Items.Count;
            private event Action<MonitoringItem> OnItemAdded;
            
            private void Initialize(string name)
            {
                Name = name;
                m_CancellationSignalSource = CancellationSignalSource.Create(name);
            }
            
            public DisposableAction Add<T>(string key, Func<T> accessor, PlayerLoopTiming timing = PlayerLoopTiming.Update)
            {
                if (!Items.TryGetValue(key, out var keyData))
                {
                    keyData = ParamLessPool<List<MonitoringItem>>.GetShared();
                    Items.Add(key, keyData);
                }
                
                var monitoringItem = MonitoringItem.Create(Name, key, Wrap(accessor), typeof(T), timing);
                keyData.Add(monitoringItem);
                OnItemAdded?.Invoke(monitoringItem);
                
                return DisposableAction.Create(() =>
                {
                    keyData.Remove(monitoringItem);
                    monitoringItem.Return();
                    
                    if (keyData.Count == 0)
                    {
                        ParamLessPool<List<MonitoringItem>>.ReturnShared(keyData);
                        Items.Remove(key);
                    }
                });
            }
            
            public DisposableAction ObserveItems(Func<MonitoringItem, CancellationSignal, UniTaskVoid> onItemAdded)
            {
                foreach (var keyData in Items.Values)
                {
                    foreach (var item in keyData)
                    {
                        onItemAdded(item, item.CancellationSignal);
                    }
                }

                void Handler(MonitoringItem item)
                {
                    onItemAdded(item, item.CancellationSignal);
                }
                OnItemAdded += Handler;
                return DisposableAction.Create(() =>
                {
                    OnItemAdded -= Handler;
                });
            }
            
            private Func<object> Wrap<T>(Func<T> accessor)
            {
                return () => accessor();
            }
            
            public void Return()
            {
                Debug.Log("Cancel group 12345");
                m_CancellationSignalSource.TryCancel();
                Items.Clear();
                ParamLessPool<Group>.ReturnShared(this);
            }
        }
        
        private static Dictionary<string, Group> m_MonitoringData = new Dictionary<string, Group>();
        private static event Action<Group> OnGroupAdded;
        
        public static DisposableAction Create<T>(string group, string key, Func<T> accessor, PlayerLoopTiming timing = PlayerLoopTiming.Update)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            
            InternalCreate(group, key, accessor, timing, cancellationTokenSource.Token).Forget();
            
            return DisposableAction.Create(() =>
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            });
        }
        
        private static async UniTaskVoid InternalCreate<T>(string group, string key, Func<T> accessor, PlayerLoopTiming timing, CancellationToken cancellationToken)
        {
            Action onFinally = default;
            try
            {
                await UniTask.SwitchToMainThread();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!m_MonitoringData.TryGetValue(group, out var groupData))
                {
                    groupData = Group.Create(group);
                    groupData.Name = group;
                    m_MonitoringData.Add(group, groupData);
                    OnGroupAdded?.Invoke(groupData);
                }

                onFinally += groupData.Add<T>(key, accessor, timing);
                onFinally += () =>
                {
                    if (groupData.count == 0)
                    {
                        m_MonitoringData.Remove(group);
                        groupData.Return();
                    }
                };
                await cancellationToken;
                
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            catch (Exception e)
            {
                Console.Exception("Monitoring", e);
            }
            finally
            {
                onFinally?.Invoke();
            }
        }
        
        /// <summary>
        /// Observe all groups. For each group, the callback will be called once. Also, the callback accepts a cancellation signal, that will be triggered when the group is removed.
        /// </summary>
        /// <param name="onGroupAdded"></param>
        /// <returns></returns>
        public static DisposableAction ObserveGroups(Func<Group, CancellationSignal, UniTaskVoid> onGroupAdded)
        {
            foreach (var group in m_MonitoringData.Values)
            {
                onGroupAdded(group, group.CancellationSignal);
            }

            void Handler(Group group)
            {
                onGroupAdded(group, group.CancellationSignal);
            }
            OnGroupAdded += Handler;
            return DisposableAction.Create(() =>
            {
                OnGroupAdded -= Handler;
            });
        }
    }
}