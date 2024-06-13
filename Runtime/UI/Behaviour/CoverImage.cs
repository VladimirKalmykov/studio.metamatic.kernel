namespace Metamatic.UI {
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using Sirenix.OdinInspector;
    using UnityEngine.U2D;

    /// <summary>
    /// Allows to cover rect with image (works like RawImage, but can fill full rect with keeping aspect ratio)
    /// </summary>
    public class CoverImage : MaskableGraphic
    {
        [PropertyOrder(-1)]
        public Sprite sprite;
        private bool m_Tracked = false;
        private Sprite activeSprite {
            get {
                return sprite;
            }
        }
        public float pixelsPerUnit
        {
            get
            {
                float spritePixelsPerUnit = 100;
                if (activeSprite)
                    spritePixelsPerUnit = activeSprite.pixelsPerUnit;

                if (canvas)
                    m_CachedReferencePixelsPerUnit = canvas.referencePixelsPerUnit;

                return spritePixelsPerUnit / m_CachedReferencePixelsPerUnit;
            }
        }
        private float m_CachedReferencePixelsPerUnit = 100;
        private Vector4 GetDrawingDimensions()
        {
            var padding = activeSprite == null ? Vector4.zero : UnityEngine.Sprites.DataUtility.GetPadding(activeSprite);
            var size = activeSprite == null ? Vector2.zero : new Vector2(activeSprite.rect.width, activeSprite.rect.height);

            Rect r = GetPixelAdjustedRect();

            int spriteW = Mathf.Max(Mathf.RoundToInt(size.x), 1);
            int spriteH = Mathf.Max(Mathf.RoundToInt(size.y), 1);

            var v = new Vector4(
                padding.x / spriteW,
                padding.y / spriteH,
                (spriteW - padding.z) / spriteW,
                (spriteH - padding.w) / spriteH);

            v = new Vector4(
                r.x + r.width * v.x,
                r.y + r.height * v.y,
                r.x + r.width * v.z,
                r.y + r.height * v.w
            );

            return v;
        }
        static bool s_Initialized;
        static List<CoverImage> m_TrackedTexturelessImages = new List<CoverImage>();

        private void TrackSprite()
        {
            if (activeSprite != null && activeSprite.texture == null)
            {
                TrackImage(this);
                m_Tracked = true;
            }
        }
        private static void TrackImage(CoverImage g)
        {
            if (!s_Initialized)
            {
                SpriteAtlasManager.atlasRegistered += RebuildImage;
                s_Initialized = true;
            }

            m_TrackedTexturelessImages.Add(g);
        }
        static void RebuildImage(SpriteAtlas spriteAtlas)
        {
            for (var i = m_TrackedTexturelessImages.Count - 1; i >= 0; i--)
            {
                var g = m_TrackedTexturelessImages[i];
                if (null != g.activeSprite && spriteAtlas.CanBindTo(g.activeSprite))
                {
                    g.SetAllDirty();
                    m_TrackedTexturelessImages.RemoveAt(i);
                }
            }
        }
        protected override void OnPopulateMesh(VertexHelper vh) {
            Vector4 v = GetDrawingDimensions();
            var size = activeSprite == null ? Vector2.zero : new Vector2(activeSprite.rect.width, activeSprite.rect.height);
            Rect r = GetPixelAdjustedRect();
            var uv = (activeSprite != null) ? GetUVAdjestedRect(
                UnityEngine.Sprites.DataUtility.GetOuterUV(activeSprite),
                size,
                r
            ) : Vector4.zero;

            var color32 = color;
            vh.Clear();
            vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(uv.x, uv.y));
            vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(uv.x, uv.w));
            vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(uv.z, uv.w));
            vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(uv.z, uv.y));

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        private Vector4 GetUVAdjestedRect(Vector4 uv, Vector2 spriteSize, Rect rect) {
            if (spriteSize.sqrMagnitude == 0f) {
                return new Vector4(0f, 0f, 1f, 1f);
            }
            var spriteRatio = spriteSize.x / spriteSize.y;
            var rectRatio = rect.width / rect.height;

            if (spriteRatio < rectRatio)
            {
                var oldHeight = rect.height;
                var nextHeight = rect.width * (1.0f / spriteRatio);
                var yratio = oldHeight / nextHeight;
                uv.y = (1f - yratio) / 2f;
                uv.w = 1f - ((1f - yratio) / 2f);
            }
            else
            {
                var oldWidth = rect.width;
                var nextWidth = rect.height * spriteRatio;
                var xratio = oldWidth / nextWidth;
                uv.x = (1f - xratio) / 2f;
                uv.z = 1f - ((1f - xratio) / 2f);
            }

            return uv;
        }

        private void PreserveSpriteAspectRatio(ref Rect rect, Vector2 spriteSize)
        {
            var spriteRatio = spriteSize.x / spriteSize.y;
            var rectRatio = rect.width / rect.height;

            if (spriteRatio < rectRatio)
            {
                var oldHeight = rect.height;
                rect.height = rect.width * (1.0f / spriteRatio);
                var shift = (rect.height - oldHeight) / 2f;
                rect.y += ((oldHeight - rect.height) * rectTransform.pivot.y) + shift;
            }
            else
            {
                var oldWidth = rect.width;
                rect.width = rect.height * spriteRatio;
                var shift = (rect.width - oldWidth) / 2f;
                rect.x += ((oldWidth - rect.width) * rectTransform.pivot.x) - shift;
            }
        }
        protected override void UpdateMaterial()
        {
            base.UpdateMaterial();

            // check if this sprite has an associated alpha texture (generated when splitting RGBA = RGB + A as two textures without alpha)

            if (activeSprite == null)
            {
                canvasRenderer.SetAlphaTexture(null);
                return;
            }

            Texture2D alphaTex = activeSprite.associatedAlphaSplitTexture;

            if (alphaTex != null)
            {
                canvasRenderer.SetAlphaTexture(alphaTex);
            }
        }

        /// <summary>
        /// Adjusts the image size to make it pixel-perfect.
        /// </summary>
        /// <remarks>
        /// This means setting the Images RectTransform.sizeDelta to be equal to the Sprite dimensions.
        /// </remarks>
        public override void SetNativeSize()
        {
            if (activeSprite != null)
            {
                float w = activeSprite.rect.width / pixelsPerUnit;
                float h = activeSprite.rect.height / pixelsPerUnit;
                rectTransform.anchorMax = rectTransform.anchorMin;
                rectTransform.sizeDelta = new Vector2(w, h);
                SetAllDirty();
            }
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            if (canvas == null)
            {
                m_CachedReferencePixelsPerUnit = 100;
            }
            else if (canvas.referencePixelsPerUnit != m_CachedReferencePixelsPerUnit)
            {
                m_CachedReferencePixelsPerUnit = canvas.referencePixelsPerUnit;
            }
        }

        protected override void OnDidApplyAnimationProperties()
        {
            SetMaterialDirty();
            SetVerticesDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            TrackSprite();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (m_Tracked)
                UnTrackImage(this);
        }

        private static void UnTrackImage(CoverImage g)
        {
            m_TrackedTexturelessImages.Remove(g);
        }

        static protected Material s_ETC1DefaultUI = null;

        /// <summary>
        /// Cache of the default Canvas Ericsson Texture Compression 1 (ETC1) and alpha Material.
        /// </summary>
        /// <remarks>
        /// Stores the ETC1 supported Canvas Material that is returned from GetETC1SupportedCanvasMaterial().
        /// Note: Always specify the UI/DefaultETC1 Shader in the Always Included Shader list, to use the ETC1 and alpha Material.
        /// </remarks>
        static public Material defaultETC1GraphicMaterial
        {
            get
            {
                if (s_ETC1DefaultUI == null)
                    s_ETC1DefaultUI = Canvas.GetETC1SupportedCanvasMaterial();
                return s_ETC1DefaultUI;
            }
        }

        /// <summary>
        /// The specified Material used by this Image. The default Material is used instead if one wasn't specified.
        /// </summary>
        public override Material material
        {
            get
            {
                if (m_Material != null)
                    return m_Material;
#if UNITY_EDITOR
                if (Application.isPlaying && activeSprite && activeSprite.associatedAlphaSplitTexture != null)
                    return defaultETC1GraphicMaterial;
#else

                if (activeSprite && activeSprite.associatedAlphaSplitTexture != null)
                    return defaultETC1GraphicMaterial;
#endif

                return defaultMaterial;
            }

            set
            {
                base.material = value;
            }
        }

        /// <summary>
        /// Image's texture comes from the UnityEngine.Image.
        /// </summary>
        public override Texture mainTexture
        {
            get
            {
                if (activeSprite == null)
                {
                    if (material != null && material.mainTexture != null)
                    {
                        return material.mainTexture;
                    }
                    return s_WhiteTexture;
                }

                return activeSprite.texture;
            }
        }

        Sprite m_CachedSprite = null;

        void Update() {
            if (m_CachedSprite != sprite) {
                m_CachedSprite = sprite;
                SetAllDirty();
            }
        }
    }

}