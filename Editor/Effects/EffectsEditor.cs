using Cysharp.Threading.Tasks;
using Metamatic.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.CustomEditors
{
    [CustomEditor(typeof(Effects))]
    public class EffectsEditor: AsyncEditor
    {
        private const string k_EffectsComponentTitleImagePath =
            "Packages/studio.metamatic.kernel/Editor/Effects/Assets/EffectsComponentTitle.psd";
        private static Texture2D s_EffectsComponentTitleImage;
        private static Texture2D EffectsComponentTitleImage
        {
            get
            {
                if (s_EffectsComponentTitleImage == null)
                {
                    s_EffectsComponentTitleImage = AssetDatabase.LoadAssetAtPath<Texture2D>(k_EffectsComponentTitleImagePath);
                }

                return s_EffectsComponentTitleImage;
            }
        }
        
        protected override VisualElement CreateInspectorGUIAsync()
        {
            var root = new VisualElement();
            
            /*
             * Set the background image of the header
             */
            var headerImage = new Image();
            headerImage.image = EffectsComponentTitleImage;
            headerImage.style.height = 37;
            
            root.Add(headerImage);
            return root;
        }

        protected override UniTask OnEnableAsync(Signal cancellationSignal)
        {
            return UniTask.CompletedTask;
        }
    }
}