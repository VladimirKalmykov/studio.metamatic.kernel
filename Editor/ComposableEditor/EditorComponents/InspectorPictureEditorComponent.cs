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

        public InspectorPictureEditorComponent(ComposableEditor composableEditor) : base(composableEditor)
        {
            var root = this;
            root.style.paddingBottom = 10;
            root.style.paddingRight = 2;

            /* Handle target attibutes */
            var target = (Component)this.target;
            var attributes = target.GetType().GetCustomAttributes(true);
            foreach (var attribute in attributes)
            {
                switch (attribute)
                {
                    case InspectorPictureAttribute inspectorImageHeaderAttribute:
                    {
                        var assetPath = inspectorImageHeaderAttribute.assetPath;
                        if (assetPath.Length > 0) {
                            object[] slicedImages = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                            if (slicedImages.Length > 0) {
                                try {   
                                    var sprite = (Sprite)slicedImages[1];
                                    root.Add(GUIHelpers.Create3SliedImageGUI(sprite));
                                } catch {
                                    // Skip
                                }
                            }
                        }

                        break;
                    }
                    case SetPicture picture:
                    {
                        var assetPath = picture.assetPath;
                        if (assetPath.Length > 0) {
                            object[] slicedImages = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                            if (slicedImages.Length > 0) {
                                var sprite = (Sprite)slicedImages[1];
                                root.Add(GUIHelpers.Create3SliedImageGUI(sprite));
                            }
                        }

                        break;
                    }
                }
            }
        }
    }
}