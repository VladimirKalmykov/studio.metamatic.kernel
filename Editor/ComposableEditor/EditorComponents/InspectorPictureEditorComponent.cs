using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Metamatic;
using Metamatic.UI;

namespace Metamatic {
    public class InspectorPictureEditorComponent: EditorComponent {
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        public class SetPicture: Attribute {
            public string assetPath {
                get; private set;
            } = "";
            public string extension {
                get; private set;
            }
            public SetPicture(string assetPath) {
                this.assetPath = assetPath;
            }
            public SetPicture(string assetPath, string extension) {
                this.assetPath = assetPath;
                this.extension = extension;
            }
        }
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            root.style.paddingBottom = 10;
            root.style.paddingRight = 2;

            /* Handle target attibutes */
            Component target = (Component)this.target;
            var attributes = target.GetType().GetCustomAttributes(true);
            foreach (object attribute in attributes)
            {
                InspectorPictureAttribute inspectorImageHeaderAttribute = attribute as InspectorPictureAttribute;
                if (inspectorImageHeaderAttribute != null) {
                    string assetPath = inspectorImageHeaderAttribute.assetPath;
                    if (assetPath.Length > 0) {
                        object[] slicedImages = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                        if (slicedImages.Length > 0) {
                            try {   
                                Sprite sprite = (Sprite)slicedImages[1];
                                root.Add(GUIHelpers.Create3SliedImageGUI(sprite));
                            } catch (Exception e) {
                                // Skip
                            }
                        }
                    }
                }

                SetPicture picture = attribute as SetPicture;
                if (picture != null) {
                    string assetPath = picture.assetPath;
                    if (assetPath.Length > 0) {
                        object[] slicedImages = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                        if (slicedImages.Length > 0) {
                            Sprite sprite = (Sprite)slicedImages[1];
                            root.Add(GUIHelpers.Create3SliedImageGUI(sprite));
                        }
                    }
                }
            }
            
            return root;
        }
    }
}