using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.IO;
using System.Text.RegularExpressions;

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
            MonoScript ms = MonoScript.FromScriptableObject( scriptableObject );
            string directoryName = Path.GetDirectoryName(AssetDatabase.GetAssetPath(ms));

            return AssetDatabase.LoadAssetAtPath<T>(NormalizePath(Path.Combine(directoryName, relativePath)));
        }

        /// <summary>
        /// Load asset relative script folder
        /// </summary>
        /// <param name="scriptableObject"></param>
        /// <param name="relativePath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T LoadRelativeAsset<T>(this EditorWindow scriptableObject, string relativePath) where T: UnityEngine.Object {
            MonoScript ms = MonoScript.FromScriptableObject( scriptableObject );
            string directoryName = Path.GetDirectoryName(AssetDatabase.GetAssetPath(ms));

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
            MonoScript ms = MonoScript.FromMonoBehaviour( monoBehaviour );
            string directoryName = Path.GetDirectoryName(AssetDatabase.GetAssetPath(ms));
            var finalPath = NormalizePath(Path.Combine(directoryName, relativePath));
            return AssetDatabase.LoadAssetAtPath<T>(finalPath);
        }

        public static string NormalizePath(string path) {
            char replaceSymbol = Path.DirectorySeparatorChar == '/' ? '\\' : '/';
            return MiscUtils.PathSimplify(path.Replace(replaceSymbol, Path.DirectorySeparatorChar));
        }
        
        #endregion
    }
}