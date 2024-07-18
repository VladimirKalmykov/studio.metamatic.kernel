ProtoShape2d is not visible in Web5 build
--

You should add ProtoShape2D/Color shader to preloaded shaders in Unity. You can do this by running the following script in Unity Editor.

```csharp
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class ShaderPreload : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Add ProtoShape2D/Color Shader to Preloaded Shaders")]
    static void AddShader()
    {
        Shader shader = Shader.Find("ProtoShape2D/Color");
        if (shader != null)
        {
            var graphicsSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0];
            SerializedObject serializedObject = new SerializedObject(graphicsSettings);
            SerializedProperty alwaysIncludedShaders = serializedObject.FindProperty("m_AlwaysIncludedShaders");
            int arraySize = alwaysIncludedShaders.arraySize;
            alwaysIncludedShaders.InsertArrayElementAtIndex(arraySize);
            alwaysIncludedShaders.GetArrayElementAtIndex(arraySize).objectReferenceValue = shader;
            serializedObject.ApplyModifiedProperties();
            Debug.Log("ProtoShape2D/Color shader added to Always Included Shaders.");
        }
        else
        {
            Debug.LogError("Shader ProtoShape2D/Color not found!");
        }
    }
#endif
}
```