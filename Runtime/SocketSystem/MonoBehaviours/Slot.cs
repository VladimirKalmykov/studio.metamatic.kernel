using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic.DevTools;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Metamatic.SocketSystem
{
    public abstract class Slot : MonoBehaviour
    {
        private static readonly Color DisconnectedGizmoColor = new(0.77f, 0.21f, 0.12f);
        private static readonly Color ConnectedGizmoColor = new(0.12f, 0.77f, 0.21f);

        public readonly List<Module> connectedModules = new();
        public readonly Dictionary<Module, SignalSource> connectionSignals = new();
        public abstract Type formFactorType { get; }
        /// <summary>
        /// Determines if the slot is free to accept new connections.
        /// </summary>
        /// <returns></returns>
        public bool IsFree() => connectedModules.Count == 0 || allowMultipleConnections;

        // Виртуальный якорь в ЛОКАЛЕ слота
        [SerializeField] public Vector3 anchorPosition;
        [SerializeField] public Quaternion anchorRotation;

        [SerializeField] protected bool allowMultipleConnections = true;
        [SerializeField] protected bool dedicated; // если true — слот «занимает» единственного ребёнка
        [SerializeField] public DeviceController controller;

        public bool isSupportMultiConnections => allowMultipleConnections && !dedicated;

        public event Action OnReconnect;

        /// <summary>Сообщить модулям, что якорь сдвинулся/повернулся.</summary>
        public void NotifyAnchorChanged()
        {
            // аккуратно: копия массива, на случай что ReapplySticky триггерит события
            foreach (var m in connectedModules.ToArray())
                m?.ReapplySticky();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // в редакторе при изменении полей — обновить позиционирование подключённых модулей
            if (!Application.isPlaying) NotifyAnchorChanged();
        }
#endif

        public Action Connect(Module module)
        {
#if DEBUG
            if (!Application.isPlaying) throw new Exception("Slot can be connected only in play mode");
#endif
            if (connectedModules.Contains(module)) throw new Exception("Module is already connected to this slot");
            if (module.connectedSlot != null && module.connectedSlot != this)
                throw new Exception($"Module {module} is already connected to slot {module.connectedSlot}");

            var undo = InternalConnect(module);
            if (module.connectedSlot == null) module.Connect(this);

            OnReconnect?.Invoke();
            return () =>
            {
                Disconnect(module);
                undo?.Invoke();
            };
        }

        public void Disconnect(Module module)
        {
#if DEBUG
            if (!Application.isPlaying) throw new Exception("Slot can be disconnected only in play mode");
#endif
            if (!connectedModules.Contains(module))
                throw new Exception($"Module {module} is not connected to slot {this}");

            InternalDisconnect(module);
            if (module.connectedSlot == this) module.Disconnect(this);
            OnReconnect?.Invoke();
        }

        protected abstract Action InternalConnect(Module module);
        protected abstract void InternalDisconnect(Module module);

        /// <summary>Получить отраженный поворот с учетом отражения объекта в мировом пространстве</summary>
        private Quaternion GetReflectedRotation(Quaternion localRotation, Transform transform)
        {
            // Получаем матрицу трансформации в мировом пространстве
            Matrix4x4 worldMatrix = transform.localToWorldMatrix;

            // Определяем отражение по знаку определителя матрицы
            float determinant = worldMatrix.determinant;
            bool isReflected = determinant < 0;

            if (isReflected)
            {
                // При отражении инвертируем все компоненты кватерниона
                return new Quaternion(
                    -localRotation.x,
                    -localRotation.y,
                    -localRotation.z,
                    localRotation.w
                );
            }

            return localRotation;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var t = transform;
            var handleSize = HandleUtility.GetHandleSize(t.position);
            var radius = 0.2f * handleSize;
            var color = connectedModules.Count > 0 ? ConnectedGizmoColor : DisconnectedGizmoColor;

            Vector3    worldAnchorPosition = t.TransformPoint(anchorPosition);
            Quaternion worldAnchorRotation = t.rotation * GetReflectedRotation(anchorRotation, t);

            GizmoHelpers.DrawOrientation(t, worldAnchorPosition, worldAnchorRotation, 0.5f, color);
        }

        private void OnDrawGizmosSelected()
        {
            var color = connectedModules.Count > 0 ? ConnectedGizmoColor : DisconnectedGizmoColor;
            Vector3 worldAnchorPosition = transform.TransformPoint(anchorPosition);
            var handleSize = HandleUtility.GetHandleSize(transform.position);

            var textColor = new GUIStyle { normal = { textColor = color } };
            Handles.Label(worldAnchorPosition + (Vector3.right * 0.33f * handleSize) + (Vector3.up * 0.33f * handleSize),
                $"{GetType().Name}<{MiscUtils.GetGenericName(GetType().BaseType, true, true)}>", textColor);
        }
#endif
        
        public static Slot[] GetSlots(GameObject gameObject, Type formFactorType)
        {
            var slots = gameObject.GetComponentsInChildren<Slot>();
            var compatibleSlots = new List<Slot>();
            foreach (var slot in slots)
            {
                if (slot.formFactorType.IsAssignableFrom(formFactorType))
                    compatibleSlots.Add(slot);
            }
            return compatibleSlots.ToArray();
        }
    }

    public abstract class Slot<TFormFactor> : Slot where TFormFactor : FormFactor
    {
        public override Type formFactorType => typeof(TFormFactor);
        public virtual UniTaskVoid OnConnected(Module<TFormFactor> module, Signal disconnectSignal)
        {
            return new UniTaskVoid();
        }

        protected override Action InternalConnect(Module module)
        {
            var m = module as Module<TFormFactor>;
            if (m == null) throw new Exception($"Module {module} is not of type {typeof(TFormFactor)}");
            return InternalConnect(m);
        }

        private Action InternalConnect(Module<TFormFactor> module)
        {
            if (connectedModules.Contains(module))
            {
                Debug.LogWarning("Module is already connected to this slot");
                return () => InternalDisconnect(module);
            }

            if (!allowMultipleConnections && connectedModules.Count > 0)
            {
                Debug.LogError("Multiple connections are not allowed");
                return () => { };
            }

            if (dedicated)
            {
                InternalDisconnectAll();
                for (var i = 0; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i);
                    if (child != module.transform && !connectedModules.Contains(child.GetComponent<Module>()))
                        Destroy(child.gameObject);
                }
            }

            connectedModules.Add(module);
            var signalSource = SignalSource.Create();
            connectionSignals.Add(module, signalSource);
            OnConnected(module, signalSource.Signal).Forget();

            // при подключении — сразу прилипнуть корректно
            module.ReapplySticky();

            return () => InternalDisconnect(module);
        }

        private void InternalDisconnect(Module<TFormFactor> module)
        {
            if (!connectedModules.Contains(module))
                throw new Exception($"Module {module} is not connected to slot {this}");

            if (connectionSignals.Remove(module, out var signalSource)) signalSource.TryRaise();
            connectedModules.Remove(module);
        }

        protected override void InternalDisconnect(Module module)
        {
            var m = module as Module<TFormFactor>;
            if (m == null) throw new Exception($"Module {module} is not of type {typeof(TFormFactor)}");
            InternalDisconnect(m);
        }

        protected void InternalDisconnectAll()
        {
            foreach (var module in connectedModules.ToArray())
                InternalDisconnect(module);
        }
    }
}
