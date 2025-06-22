using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

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

            if ((stage & ActionStage.Started) != 0)
                action.started   += handler;
            if ((stage & ActionStage.Performed) != 0)
                action.performed += handler;
            if ((stage & ActionStage.Canceled) != 0)
                action.canceled  += handler;

            return DisposableAction.Create(() =>
            {
                if ((stage & ActionStage.Started) != 0)
                    action.started   -= handler;
                if ((stage & ActionStage.Performed) != 0)
                    action.performed -= handler;
                if ((stage & ActionStage.Canceled) != 0)
                    action.canceled  -= handler;
            });
        }

        public void Dispose() => m_OnDispose?.Invoke();
    }

    /// <summary>
    /// Сервис, управляющий активными Action Maps выбранного <see cref="inputActionAsset"/>.
    /// Позволяет временно «захватывать» карты ввода (меню, кат-сцена, диалог и т.д.),
    /// гарантируя, что в момент использования одной карты исключён ввод всех, указанных в <see cref="exclusionActionMaps"/>.
    /// Также предоставляет уведомление о смене схемы управления и позволяет получить отображаемую строку бинда по имени карты и действия.
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

        // Для подписки на смены схемы
        private readonly HashSet<PlayerInput>       m_TrackedPlayers        = new();

        /// <summary>
        /// Событие смены схемы управления. Параметры: PlayerInput и название текущей схемы.
        /// </summary>
        public event Action<PlayerInput, string> OnControlSchemeChanged;

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

            if (exclusionActionMaps.Contains(name))
            {
                var pop = m_CurrentActionMapStack.Push(name);
                return new ActionMapUsage(actionMap, pop);
            }

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
        
        /// <summary>
        /// Запрашивает использование карты ввода <paramref name="mapName"/> для конкретного игрока.
        /// Подписывается на событие смены схемы управления этого игрока.
        /// </summary>
        public ActionMapUsage UseActionMap(PlayerInput player, string mapName)
        {
            if (player == null)
            {
                Console.Error("InputActionsService", "UseActionMap(PlayerInput)",
                    "player is null");
                return ActionMapUsage.Empty;
            }

            RegisterPlayerForSchemeEvents(player);

            var map = player.actions.FindActionMap(mapName, true);
            if (map == null)
            {
                Console.Error("InputActionsService", "UseActionMap(PlayerInput)",
                    $"Map '{mapName}' not found inside '{player.actions.name}'.");
                return ActionMapUsage.Empty;
            }

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

        /// <summary>
        /// Возвращает отображаемую строку бинда для указанной карты, действия и схемы.
        /// </summary>
        public string GetBindingDisplayString(string mapName, string actionName, string controlSchemeName)
        {
            if (m_InputActionsAsset == null)
                throw new InvalidOperationException("InputActionAsset is null.");

            var map = m_InputActionsAsset.FindActionMap(mapName, true);
            var action = map.FindAction(actionName, true);

            int bindingIndex = action.bindings
                .Select((b, i) => new { b, i })
                .FirstOrDefault(x => x.b.groups.Split(';').Contains(controlSchemeName))
                ?.i ?? -1;

            return bindingIndex >= 0 ? action.GetBindingDisplayString(bindingIndex) : string.Empty;
        }

        /// <summary>
        /// Перегрузка: для указанного PlayerInput берём текущую схему.
        /// </summary>
        public string GetBindingDisplayString(PlayerInput player, string mapName, string actionName)
            => GetBindingDisplayString(mapName, actionName, player.currentControlScheme);

        // ──────────── SERVICE LIFECYCLE ────────────────────────────────────────

        protected override async UniTask RunService(Ready ready, Signal signal)
        {
            if (m_InputActionsAsset == null)
                Console.Warning("InputActionsService", "RunService",
                    "InputActionAsset is not assigned — сервис не будет активен.");

            
            InputUser.onChange += (user, change, device) =>
            {
                if (user.controlScheme.HasValue)
                {
                    Console.Debug("InputActionsService", user.controlScheme.Value.name);
                }
            };
            
            using var watch = m_CurrentActionMapStack.WatchLatest(OnCurrentActionMapChange);

            await ready();

            if (m_InputActionsAsset != null)
                foreach (var map in m_InputActionsAsset.actionMaps)
                    map.Disable();
        }

        // ─────────────────────────────────────────────────────────────────────────────
        private void OnCurrentActionMapChange(string activeName)
        {
            if (m_InputActionsAsset == null) return;

            foreach (var map in m_InputActionsAsset.actionMaps)
            {
                if (string.IsNullOrEmpty(activeName))
                {
                    if (exclusionActionMaps.Contains(map.name))
                        map.Disable();
                }
                else
                {
                    if (map.name == activeName) map.Enable();
                    else                        map.Disable();
                }
            }
        }

        /// <summary>
        /// Подписываем игрока на событие смены схемы.
        /// </summary>
        private void RegisterPlayerForSchemeEvents(PlayerInput player)
        {
            if (m_TrackedPlayers.Add(player))
            {
                player.onControlsChanged += pi =>
                    OnControlSchemeChanged?.Invoke(pi, pi.currentControlScheme);
            }
        }
    }
}
