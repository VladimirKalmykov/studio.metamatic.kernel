using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;

namespace Metamatic
{
    public class StoreReader<T>: AsyncBehaviour, ISerializationCallbackReceiver
        where T: struct
    {
        #region Fields
        [SerializeField] private string defaultStoreName;
        /// <summary>
        /// Determines that pin must be nullable. If store is not found or data is not found in store, pin will be set to null
        /// </summary>
        [SerializeField] private bool isNullable;
        /// <summary>
        /// Pin for for non-nullable mode
        /// </summary>
        [SerializeField] private Pin<T> pin;
        /// <summary>
        /// Pin for nullable mode
        /// </summary>
        [SerializeField] private Pin<T?> nullablePin;
        #endregion

        private delegate void WriteDataToPinDelegate(T? value);
        private ObservableWritableValue<string> m_StoreName = new();
        
        /// <summary>
        /// Dynamically set store name. If store name is not set, default store name will be used
        /// </summary>
        /// <param name="storeName"></param>
        public void SetStoreName(string storeName)
        {
            m_StoreName.value = storeName;
        }

        public void OnBeforeSerialize()
        {
            
        }

        public void OnAfterDeserialize()
        {
            m_StoreName.value = defaultStoreName;
        }
        
        protected override async void RunAsync(CancellationToken cancellationToken)
        {
            #if DEBUG
            /* Validate pins */
            if (isNullable && nullablePin == null)
            {
                Debug.LogError($"Nullable pin is not set for {nameof(StoreReader<T>)}");
                return;
            }
            if (!isNullable && pin == null)
            {
                Debug.LogError($"Pin is not set for {nameof(StoreReader<T>)}");
                return;
            }
            #endif
            using var nullablePinWriter = isNullable ? nullablePin.CreateWriter(this) : null;
            using var pinWriter = isNullable ? null : pin.CreateWriter(this);
            WriteDataToPinDelegate writeDataToPin = isNullable
                ? (T? value) =>
                {
                    // ReSharper disable once PossibleNullReferenceException
                    nullablePinWriter.SetData(value);
                }
                : (T? value) =>
                {
                    pinWriter.SetData(value.Value);
                };
            using var storeNameHandler = new SequentialHandler<string, WriteDataToPinDelegate>(OnStoreNameChanged, default, writeDataToPin);
            using (new DisposableCallback(m_StoreName.Subscribe(storeNameHandler.Handle)))
            {
                await cancellationToken;
            }
        }
        
        private async void OnStoreNameChanged(string storeName, CancellationToken cancellationToken, WriteDataToPinDelegate writeToPin)
        {
            if (string.IsNullOrEmpty(storeName))
            {
                return;
            }
            using var store = RepositoryLegacy.GetStore<T>(storeName);
            using (new DisposableCallback(store.Subscribe(value => { writeToPin(value); })))
            {
                await cancellationToken;
            }
        }
    }
}