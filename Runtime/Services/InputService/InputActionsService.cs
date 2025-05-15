using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using Console = Metamatic.Console;

namespace Metamatic
{
    /// <summary>
    /// Диспоузер действия над конкретной Action Map.
    /// </summary>
    public readonly struct ActionMapUsage : IDisposable
    {
        [Flags]
        public enum ActionStage
        {
            None      = 0,
            Started   = 1 << 0,
            Performed = 1 << 1,
            Canceled  = 1 << 2,
        }

        public static ActionMapUsage Empty => new ActionMapUsage(null, null);

        private readonly InputActionMap m_ActionMap;
        private readonly Action         m_OnDispose;

        public ActionMapUsage(InputActionMap actionMap, Action onDispose)
        {
            m_ActionMap = actionMap;
            m_OnDispose = onDispose;
        }

        public InputAction FindAction(string name, bool throwIfNotFound = false)
        {
            if (m_ActionMap == null)
                throw new InvalidOperationException("Action map is null.");

            var action = m_ActionMap.FindAction(name, throwIfNotFound);
            if (action == null && throwIfNotFound)
                throw new ArgumentException($"Action '{name}' not found in map '{m_ActionMap.name}'.");

            return action;
        }

        public DisposableAction Bind(string name, ActionStage stage, Action<InputAction.CallbackContext> handler)
        {
            var action = FindAction(name, true);

            // stage is a bitmask, so we can use it to check if the action is enabled
            if ((stage & ActionStage.Started) != 0)
            {
                action.started   += handler;
            }
            if ((stage & ActionStage.Performed) != 0)
            {
                action.performed += handler;
            }
            if ((stage & ActionStage.Canceled) != 0)
            {
                action.canceled  += handler;
            }

            return DisposableAction.Create(() =>
            {
                if ((stage & ActionStage.Started) != 0)
                {
                    action.started   -= handler;
                }
                
                if ((stage & ActionStage.Performed) != 0)
                {
                    action.performed -= handler;
                }
                
                if ((stage & ActionStage.Canceled) != 0)
                {
                    action.canceled  -= handler;
                }
            });
        }

        public void Dispose() => m_OnDispose?.Invoke();
    }

    /// <summary>
    /// Сервис, управляющий активными Action Maps выбранного <see cref="inputActionAsset"/>.
    /// Позволяет временно «захватывать» карты ввода (меню, кат-сцена, диалог и т.д.),
    /// гарантируя, что в момент использования одной карты исключён ввод всех, указанных в <see cref="exclusionActionMaps"/>.
    /// </summary>
    public class InputActionsService : Service
    {
        // ──────────── CONFIG ────────────────────────────────────────────────────

        [Title("Settings")]
        [Tooltip("Ассет, содержащий все Action Maps проекта.")]
        [SerializeField] private InputActionAsset m_InputActionsAsset;
        
        #if UNITY_EDITOR
        public InputActionAsset inputActionAsset => m_InputActionsAsset;
        #endif

        [Tooltip("Список карт, которые не могут быть активны одновременно: " +
                 "при включении одной все прочие из списка будут отключены автоматически.")]
        [SerializeField] public List<string> exclusionActionMaps = new();

        // ──────────── STATE ────────────────────────────────────────────────────

        private readonly ObservableStackLike<string> m_CurrentActionMapStack = new(string.Empty);
        private readonly Dictionary<string, int>     m_ActionMapInquiry      = new(8);

        // ──────────── PUBLIC API ───────────────────────────────────────────────

        [ShowInInspector, ReadOnly]
        public string currentActionMap => m_CurrentActionMapStack.latest;

        [ShowInInspector, ReadOnly]
        public string[] actionMaps =>
            m_InputActionsAsset != null
                ? m_InputActionsAsset.actionMaps.Select(x => x.name).ToArray()
                : Array.Empty<string>();
        
        private readonly Dictionary<(PlayerInput player, string map), int>
            m_PlayerMapInquiry = new(16);

        /// <summary>
        /// Запрашивает использование карты ввода <paramref name="name"/>.
        /// Возвращает токен (<see cref="ActionMapUsage"/>) — при <see cref="IDisposable.Dispose"/> карта будет
        /// автоматически отпущена и, при необходимости, деактивирована.
        /// </summary>
        public ActionMapUsage UseActionMap(string name)
        {
            if (m_InputActionsAsset == null)
            {
                Console.Error("InputActionsService", nameof(UseActionMap),
                    "InputActionAsset is null — проверьте настройки сервиса!");
                return ActionMapUsage.Empty;
            }

            var actionMap = m_InputActionsAsset.FindActionMap(name, true);
            if (actionMap == null)
            {
                Console.Error("InputActionsService", nameof(UseActionMap),
                    $"Action Map '{name}' not found inside asset '{m_InputActionsAsset.name}'.");
                return ActionMapUsage.Empty;
            }

            // === CASE 1. Карта входит в исключающий список — работаем через стек ===
            if (exclusionActionMaps.Contains(name))
            {
                var pop = m_CurrentActionMapStack.Push(name); // заставит OnCurrentActionMapChange переключить карты
                return new ActionMapUsage(actionMap, pop);
            }

            // === CASE 2. Любая другая карта, «Reference count»-style ===
            if (m_ActionMapInquiry.TryGetValue(name, out var cnt))
                m_ActionMapInquiry[name] = cnt + 1;
            else
            {
                m_ActionMapInquiry[name] = 1;
                actionMap.Enable();
            }

            return new ActionMapUsage(actionMap, () =>
            {
                if (!m_ActionMapInquiry.TryGetValue(name, out var left)) return;

                if (left == 1)
                {
                    m_ActionMapInquiry.Remove(name);
                    actionMap.Disable();
                }
                else m_ActionMapInquiry[name] = left - 1;
            });
        }
        
        // ─────────────────────────────────────────────────────────────────────────────
//  N E W :  per-player API
// ─────────────────────────────────────────────────────────────────────────────

        public ActionMapUsage UseActionMap(PlayerInput player, string mapName)
        {
            if (player == null)
            {
                Console.Error("InputActionsService", "UseActionMap(PlayerInput)",
                    "player is null");
                return ActionMapUsage.Empty;
            }

            var map = player.actions.FindActionMap(mapName, true);
            if (map == null)
            {
                Console.Error("InputActionsService", "UseActionMap(PlayerInput)",
                    $"Map '{mapName}' not found inside '{player.actions.name}'.");
                return ActionMapUsage.Empty;
            }

            // точно так же, как в «глобальной» версии, но inquiry ведём
            // по ключу  (player, map)
            var key = (player, mapName);
            if (m_PlayerMapInquiry.TryGetValue(key, out var cnt))
                m_PlayerMapInquiry[key] = cnt + 1;
            else
            {
                m_PlayerMapInquiry[key] = 1;
                map.Enable();
            }

            return new ActionMapUsage(map, () =>
            {
                if (!m_PlayerMapInquiry.TryGetValue(key, out var left)) return;

                if (left == 1)
                {
                    m_PlayerMapInquiry.Remove(key);
                    map.Disable();
                }
                else m_PlayerMapInquiry[key] = left - 1;
            });
        }
        

        // ──────────── SERVICE LIFECYCLE ────────────────────────────────────────

        protected override async UniTask RunService(Ready ready, Signal signal)
        {
            if (m_InputActionsAsset == null)
                Console.Warning("InputActionsService", "RunService",
                    "InputActionAsset is not assigned — сервис не будет активен.");

            using var watch = m_CurrentActionMapStack.WatchLatest(OnCurrentActionMapChange);

            await ready();

            // По завершении сервиса гарантируем, что все карты выключены
            if (m_InputActionsAsset != null)
                foreach (var map in m_InputActionsAsset.actionMaps)
                    map.Disable();
        }

        // ──────────── INTERNALS ────────────────────────────────────────────────

        private void OnCurrentActionMapChange(string activeName)
        {
            if (m_InputActionsAsset == null) return;

            foreach (var map in m_InputActionsAsset.actionMaps)
            {
                if (string.IsNullOrEmpty(activeName))
                {
                    // Стек пуст: выключаем только те, что попадают под исключение
                    if (exclusionActionMaps.Contains(map.name))
                        map.Disable();
                }
                else
                {
                    // В стеке есть активная карта — включаем только её
                    if (map.name == activeName) map.Enable();
                    else                        map.Disable();
                }
            }
        }
    }
}
