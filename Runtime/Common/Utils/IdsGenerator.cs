using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Metamatic
{
    public static class IdsGenerator
    {
        public enum IdType
        {
            Guid,
            ShortGuid
        }
        public static string GenerateGuid()
        {
            return Guid.NewGuid().ToString();
        }
        
        public static string GenerateShortGuid(int length = 8)
        {
            if (length <= 0 || length > 32)
                throw new ArgumentException("Length must be between 1 and 32");

            Guid guid = Guid.NewGuid();

            // Создаем MD5 хеш от Guid
            using (var md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(guid.ToByteArray());
                return ByteArrayToString(hashBytes).Substring(0, length);
            }
        }
        
        public static string GenerateId(IdType idType)
        {
            switch (idType)
            {
                case IdType.Guid:
                    return GenerateGuid();
                case IdType.ShortGuid:
                    return GenerateShortGuid();
                default:
                    throw new ArgumentOutOfRangeException(nameof(idType), idType, null);
            }
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// Generate unique id of specified type for component of specified type.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="idType"></param>
        /// <param name="matchId">Function that compares provided id with provided component's id</param>
        /// <param name="id"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool GenerateUniqueIdForComponent<T>(T component, IdType idType, Func<T, string, bool> matchId, out string id) where T : UnityEngine.Component
        {
            var nextId = GenerateId(idType);
            // Find all EntitySpawnAreas in the scene
            var spawnAreas = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            // Check if there are any other spawn areas with the same id
            int tries = 0;
            while (tries < 1000 && spawnAreas.Any(other => other != component && matchId(other, nextId)))
            {
                // If there are, generate a new id
                nextId = GenerateId(idType);
                tries++;
            }
            {
                // If there are, generate a new id
                nextId = GenerateId(idType);
                tries++;
            }
            if (tries >= 1000)
            {
                Debug.LogError("Failed to generate unique id for EntitySpawnArea");
                id = null;
                return false;
            }
            id = nextId;
            return true;
        }
        #endif
        
        private static string ByteArrayToString(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

    }
}