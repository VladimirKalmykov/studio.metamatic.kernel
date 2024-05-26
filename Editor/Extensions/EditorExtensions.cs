using UnityEngine;
using UnityEditor;
using System.IO;

namespace Metamatic.Extensions {
    public static partial class ExtensionMethods {
        #region RelativeAsset
        /// <summary>
        /// Load asset relative script folder
        /// </summary>
        /// <param name="scriptableObject"></param>
        /// <param name="relativePath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T LoadRelativeAsset<T>(this Editor scriptableObject, string relativePath) where T: UnityEngine.Object {
            var ms = MonoScript.FromScriptableObject( scriptableObject );
            var directoryName = Path.GetDirectoryName(AssetDatabase.GetAssetPath(ms));
            var path = NormalizePath(Path.Combine(directoryName, relativePath));
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        /// <summary>
        /// Load asset relative script folder
        /// </summary>
        /// <param name="scriptableObject"></param>
        /// <param name="relativePath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T LoadRelativeAsset<T>(this EditorWindow scriptableObject, string relativePath) where T: UnityEngine.Object {
            var ms = MonoScript.FromScriptableObject( scriptableObject );
            var directoryName = Path.GetDirectoryName(AssetDatabase.GetAssetPath(ms));

            return AssetDatabase.LoadAssetAtPath<T>(NormalizePath(Path.Combine(directoryName, relativePath)));
        }

        /// <summary>
        /// Load asset relative script folder
        /// </summary>
        /// <param name="scriptableObject"></param>
        /// <param name="relativePath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T LoadRelativeAsset<T>(this MonoBehaviour monoBehaviour, string relativePath) where T: UnityEngine.Object {
            var ms = MonoScript.FromMonoBehaviour( monoBehaviour );
            var directoryName = Path.GetDirectoryName(AssetDatabase.GetAssetPath(ms));
            var finalPath = NormalizePath(Path.Combine(directoryName, relativePath));
            return AssetDatabase.LoadAssetAtPath<T>(finalPath);
        }

        public static string NormalizePath(string path) {
            var replaceSymbol = Path.DirectorySeparatorChar == '/' ? '\\' : '/';
            return MiscUtils.PathSimplify(path.Replace(replaceSymbol, Path.DirectorySeparatorChar));
        }
        
        #endregion
    }
}