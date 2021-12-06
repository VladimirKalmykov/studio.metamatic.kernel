using UnityEditor;
using UnityEngine;

namespace Metamatic.Nature.Services
{
    [AddComponentMenu("Metamatic/Systems/Vfx")]
    public class VfxSystem : ServiceSystem
    {
        [MenuItem("GameObject/Metamatic/Systems/Vfx", false, 1)]
        private static void CreateObject(MenuCommand menuCommand)
        {
            var go = new GameObject("VfxSystem", new [] { typeof(VfxSystem) });
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
    }
}