using UnityEditor;
using UnityEngine;

namespace Metamatic.EditorTools
{
    public static class Startup
    {
        static Startup()
        {
            EditorApplication.update += ShowSetupWindowOnFirstRun;
        }
        
        private static void ShowSetupWindowOnFirstRun()
        {
            Debug.Log("Opening setup window");
            return;
            EditorApplication.update -= ShowSetupWindowOnFirstRun;

            // Проверьте, был ли пакет уже настроен (например, проверка наличия файла настроек)
            if (!EditorPrefs.HasKey("MetamaticPackageSetupComplete"))
            {
                // Если пакет не был настроен, откройте окно настройки
                EditorPrefs.SetBool("MetamaticPackageSetupComplete", true);
            }
        }
    }
}