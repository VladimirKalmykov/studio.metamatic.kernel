using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic.SaveSystem;
using UnityEngine;

namespace Metamatic
{
    public abstract class Storage: IStorage
    {
        #region Static

        #region Exceptions

        public abstract class IOException : Exception
        {
            public IOException(string message) : base(message)
            {
            }
        }

        /// <summary>
        /// Connection completed successfully, but requested resource is not found
        /// </summary>
        public class NotFoundException : IOException
        {
            public NotFoundException(string message) : base(message)
            {
            }
        }

        /// <summary>
        /// Determines that requested resource is unreachable for some reasons
        /// </summary>
        public class UnreachableException : IOException
        {
            public UnreachableException(string message) : base(message)
            {
            }
        }

        /// <summary>
        /// Connection timeout
        /// </summary>
        public class TimeoutException : UnreachableException
        {
            public TimeoutException(string message) : base(message)
            {
            }
        }

        /// <summary>
        /// Storage is disabled
        /// </summary>
        public class DisabledException : UnreachableException
        {
            public DisabledException() : base("Storage is disabled")
            {
            }
        }

        /// <summary>
        /// Connection refused for some reasons
        /// </summary>
        public class ConnectionRefusedException : UnreachableException
        {
            public ConnectionRefusedException(string message) : base(message)
            {
            }
        }

        #endregion

        #endregion
        
        public const string Separator = "/";
        public bool enabled { get; set; } = true;
        public IStorage.TypeVersionControl typeVersionControl { get; private set; }
        public string nameSpace { get; private set; }

        public Storage(IStorage.TypeVersionControl typeVersionControl = IStorage.TypeVersionControl.Orchard, string nameSpace = null)
        {
            this.typeVersionControl = typeVersionControl;
            this.nameSpace = nameSpace;
        }

        public virtual async UniTask<TData> Get<TData>(string address, CancellationToken token)
        {
            if (!enabled)
            {
                throw new DisabledException();
            }
            var fullAddress = GetAddress<TData>(address);
            
            if (typeVersionControl == IStorage.TypeVersionControl.LegacyMetadataContainer)
            {
                var wrappedJson = await ReadFile(fullAddress, token);
                var (data, metadata, customMetadata) = VersionedDataTypeContainer.DeserializeJson<TData, object>(wrappedJson);
                return data;
            } else if (typeVersionControl == IStorage.TypeVersionControl.Orchard)
            {
                var multijson = await ReadFile(fullAddress, token);
                var (types, glossary, dataJson) = OrchardSerializationUtils.UnpackOrchardV1(multijson);
                return OrchardSerializationUtils.DeserializeObjectWithVersionControl<TData>(dataJson, types, glossary);
            }
            var json = await ReadFile(fullAddress, token);
            return JsonUtility.FromJson<TData>(json);
        }
        
        public virtual async UniTask<(TData data, bool exists)> TryGet<TData>(string address, IStorage.DataFactory<TData> getDefault, CancellationToken token)
        {
            try
            {
                var loadedData = await Get<TData>(address, token);
                return (loadedData, true);
            } catch (NotFoundException)
            {
                return (getDefault(), false);
            }
        }
        public virtual UniTask Set<TData>(string address, TData value, CancellationToken token)
        {
            if (!enabled)
            {
                throw new DisabledException();
            }
            var fullAddress = GetAddress<TData>(address);
            Console.Info("Storage", this.GetType().Name, "Set", fullAddress);
            if (typeVersionControl == IStorage.TypeVersionControl.LegacyMetadataContainer)
            {
                /* Wrap data with version */
                return WriteFile(fullAddress, VersionedDataTypeContainer.SerializeJson<TData, object>(value, null), token);
            } else if (typeVersionControl == IStorage.TypeVersionControl.Orchard)
            {
                var types = new List<Type>();
                var glossary = new List<string>();
                var dataJson = OrchardSerializationUtils.SerializeObjectWithVersionControl(value, types, glossary);
                var content = OrchardSerializationUtils.PackOrchardV1(types, glossary, dataJson);
                
                return WriteFile(fullAddress, content, token);
            }

            var json = JsonUtility.ToJson(value);
            return WriteFile(fullAddress, json, token);
        }

        public UniTask Delete<TData>(string address, CancellationToken token)
        {
            if (!enabled)
            {
                throw new DisabledException();
            }
            var fullAddress = GetAddress<TData>(address);
            Console.Verbose("Storage", this.GetType().Name, "Delete", fullAddress);
            return DeleteFile(fullAddress, token);
        }

        public UniTask<bool> Exists<TData>(string address, CancellationToken token)
        {
            if (!enabled)
            {
                throw new DisabledException();
            }
            var fullAddress = GetAddress<TData>(address);
            return FileExists(fullAddress, token);
        }

        protected virtual string GetAddress<TData>(string address)
        {
            return nameSpace != null ? $"{nameSpace}{Separator}{address}" : address;
        }
        
        protected abstract UniTask<string> ReadFile(string address, CancellationToken token);
        protected abstract UniTask WriteFile(string address, string value, CancellationToken token);
        protected abstract UniTask DeleteFile(string address, CancellationToken token);
        protected abstract UniTask<bool> FileExists(string address, CancellationToken token);
    }
}