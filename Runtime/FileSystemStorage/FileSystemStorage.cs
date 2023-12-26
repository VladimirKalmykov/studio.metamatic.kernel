using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Metamatic
{
    public class FileSystemStorage
    {
        public static void Save<TData>(string address, TData data)
        {
            var filepath = GetDataFilepath(address);
            EnsureFilepathDirectoryExists(filepath);
            
            using (FileStream file = File.Create(filepath))
            {
                new BinaryFormatter().Serialize(file, data);
            }
        }

        public static TData Load<TData>(string address)
        {
            var filepath = GetDataFilepath(address);

            using (FileStream file = File.Open(filepath, FileMode.Open))
            {
                object loadedData = new BinaryFormatter().Deserialize(file);
                return (TData)loadedData;
            }
        }

        public static bool Exists<TData>(string address)
        {
            var filepath = GetDataFilepath(address);
            return File.Exists(filepath);
        }
        
        public static string GetDataFilepath(string address) {
            return Path.Join(Application.persistentDataPath, address) + ".dat";
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