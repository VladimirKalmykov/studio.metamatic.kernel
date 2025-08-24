using System;
using UnityEngine;

namespace Metamatic.SocketSystem
{
    public abstract class Module : MonoBehaviour
    {
        [SerializeField] protected bool autoConnect = true;
        [SerializeField] protected bool sticky = true;

        // Смещение ОТНОСИТЕЛЬНО ЯКОРЯ слота
        [SerializeField] protected Vector3 relativePosition;
        [SerializeField] protected Quaternion relativeRotation;

        public event Action<Slot> OnReconnect;

        protected bool m_Destroyed;
        public Slot connectedSlot { get; protected set; }

        protected void EmitReconnect(Slot slot) => OnReconnect?.Invoke(slot);

        public DisposableAction WatchReconnect(Action<Slot> callback)
        {
            OnReconnect += callback ?? throw new ArgumentNullException(nameof(callback));
            callback(connectedSlot);
            return new DisposableAction(() => OnReconnect -= callback);
        }

        public void Disconnect(Slot slot)
        {
#if DEBUG
            if (!Application.isPlaying) throw new Exception("Module can be disconnected only in play mode");
#endif
            if (connectedSlot != slot) throw new Exception($"Slot {slot} is not connected to this module");

            connectedSlot = null;
            if (slot.connectedModules.Contains(this)) slot.Disconnect(this);
            ApplyPostDisconnectionEffects();
            EmitReconnect(null);
        }

        public void Disconnect()
        {
            if (connectedSlot != null) Disconnect(connectedSlot);
        }

        /// <summary>Применяем «липучку»: локаль = anchor + relative (позиция), rot = anchor * relative.</summary>
        protected virtual void ApplyPostConnectionEffects()
        {
            // становимся дочерним к объекту слота (важно: true — сохраняем мир, потом перепишем локаль)
            transform.SetParent(connectedSlot.transform, true);
            ReapplySticky();
        }

        /// <summary>Пере-применить «липучку» (зови, когда якорь двигается/крутится).</summary>
        public void ReapplySticky()
        {
            if (connectedSlot == null || !sticky) return;

            var s = connectedSlot;

            // Работаем строго в локале слота: чистая композиция локальных TR без учёта масштабов/отражений родителя
            transform.localPosition = s.anchorPosition + (s.anchorRotation * relativePosition);
            transform.localRotation = s.anchorRotation * relativeRotation;
        }

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
        [ContextMenu("Debug/Check baked transform")]
        private void DebugCheck()
        {
            if (connectedSlot == null) return;
            var s = connectedSlot;
            var predictedLocalPos = s.anchorPosition + (s.anchorRotation * relativePosition);
            var predictedLocalRot = s.anchorRotation * relativeRotation;
            Debug.Log($"Predicted local pos: {predictedLocalPos}, current: {transform.localPosition}");
            Debug.Log($"Predicted local rot: {predictedLocalRot.eulerAngles}, current: {transform.localRotation.eulerAngles}");
        }
#endif


        protected virtual void ApplyPostDisconnectionEffects()
        {
            if (m_Destroyed) return;
            transform.SetParent(null, true);
        }

        public Action Connect(Slot slot)
        {
#if DEBUG
            if (!Application.isPlaying) throw new Exception("Module can be connected only in play mode");
#endif
            if (connectedSlot == slot) throw new Exception("Slot is already connected to this module");

            connectedSlot = slot;
            if (!slot.connectedModules.Contains(this)) slot.Connect(this);

            ApplyPostConnectionEffects();
            EmitReconnect(slot);
            return () => Disconnect(slot);
        }

        public abstract bool TryFindAppropriateSlotInParent(out Slot slot);

        #region Mono
        private void OnDestroy()
        {
            m_Destroyed = true;
            Disconnect();
        }
        #endregion
    }

    public abstract class Module<TFormFactor> : Module where TFormFactor : FormFactor
    {
        private void Start()
        {
            if (autoConnect) AutoConnect();
        }

        private void OnTransformParentChanged()
        {
            if (!autoConnect) return;
            Disconnect();
            AutoConnect();
        }

        public void AutoConnect()
        {
            if (TryFindAppropriateSlotInParent(out var slot))
            {
                Disconnect();
                Connect(slot);
            }
        }

        public override bool TryFindAppropriateSlotInParent(out Slot slot)
        {
            var parent = transform.parent;
            if (parent == null)
            {
                slot = null;
                return false;
            }

            // sticky=true — ищем ТОЛЬКО у непосредственного родителя; иначе — у любого предка
            slot = sticky
                ? parent.GetComponent<Slot<TFormFactor>>()
                : parent.GetComponentInParent<Slot<TFormFactor>>();

            return slot != null;
        }
    }
}
