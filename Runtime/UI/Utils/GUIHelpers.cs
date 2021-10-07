using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.UI {
    public static class GUIHelpers {
        public static VisualElement Create3SliedImageGUI(Sprite sprite) {
            VisualElement root = new VisualElement();
            if (sprite != null) {
                Texture2D slicedTexture = sprite.texture;
                float leftImageWidth = sprite.border.x;
                float rightImageWidth = sprite.border.z;
                
                VisualElement imageContainer = new VisualElement();
                imageContainer.style.flexDirection = FlexDirection.Row;
                imageContainer.style.height = new StyleLength(sprite.rect.height);
                root.Add(imageContainer);

                /* Left image represents the left part of 3-sliced image */
                Image leftImage = new Image();
                leftImage.style.width = new StyleLength(leftImageWidth);
                leftImage.style.height = new StyleLength(sprite.rect.height);
                leftImage.style.flexBasis = new StyleLength(leftImageWidth);
                leftImage.style.alignSelf = Align.FlexStart;
                imageContainer.Add(leftImage);
                leftImage.image = slicedTexture;
                leftImage.scaleMode = ScaleMode.StretchToFill;
                leftImage.sourceRect = new Rect(0, 0, leftImageWidth, sprite.rect.height);

                /* Middle image */
                Image middleImage = new Image();
                middleImage.style.height = new StyleLength(sprite.rect.height);
                middleImage.style.flexGrow = new StyleFloat(3f);
                imageContainer.Add(middleImage);
                middleImage.image = slicedTexture;
                middleImage.scaleMode = ScaleMode.StretchToFill;
                middleImage.sourceRect = new Rect(leftImageWidth, 0, sprite.rect.width - leftImageWidth - rightImageWidth, sprite.rect.height);

                /* Right image */
                Image rightImage = new Image();
                rightImage.style.width = new StyleLength(rightImageWidth);
                rightImage.style.height = new StyleLength(sprite.rect.height);
                rightImage.style.flexBasis = new StyleLength(rightImageWidth);
                rightImage.style.alignSelf = Align.FlexEnd;
                imageContainer.Add(rightImage);
                rightImage.image = slicedTexture;
                rightImage.scaleMode = ScaleMode.StretchToFill;
                rightImage.sourceRect = new Rect(sprite.rect.width - rightImageWidth, 0, rightImageWidth, sprite.rect.height);
            }

            return root;
        }
    }
}