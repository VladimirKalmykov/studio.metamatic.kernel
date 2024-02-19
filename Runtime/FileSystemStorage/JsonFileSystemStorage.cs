using System;
using System.IO;
using UnityEngine;

namespace Metamatic
{
    public class JsonFileSystemStorage
    {
        public static void Save<TData>(string address, TData data)
        {
            var filepath = GetDataFilepath(address);
            EnsureFilepathDirectoryExists(filepath);

            string jsonData = JsonUtility.ToJson(data);
            File.WriteAllText(filepath, jsonData);
            Debug.Log($"File saved to {filepath}");
        }

        public static TData Load<TData>(string address)
        {
            var filepath = GetDataFilepath(address);
            string jsonData = File.ReadAllText(filepath);

            return JsonUtility.FromJson<TData>(jsonData);
        }
        
        public static object Load(string address, Type type)
        {
            var filepath = GetDataFilepath(address);
            string jsonData = File.ReadAllText(filepath);

            return JsonUtility.FromJson(jsonData, type);
        }

        public static bool Exists(string address)
        {
            var filepath = GetDataFilepath(address);
            return File.Exists(filepath);
        }
        
        public static string GetDataFilepath(string address) 
        {
            return Path.Combine(Application.persistentDataPath, address) + ".json";
        }

        public static void EnsureFilepathDirectoryExists(string path)
        {
            var directoryName = Path.GetDirectoryName(path);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
        }
    }
}