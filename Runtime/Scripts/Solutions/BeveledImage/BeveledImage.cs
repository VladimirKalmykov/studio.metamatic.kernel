namespace Metamatic
{
    using UnityEngine;
    using UnityEngine.UI;
    using System.Collections.Generic;

    [RequireComponent(typeof(CanvasRenderer))]
    public class BeveledImage : MaskableGraphic
    {
        [Header("Chamfer (Bevel) Size: TL, TR, BR, BL")]
        public Vector4 bevelSize = new Vector4(20, 20, 20, 20);

        [Header("Sprite Background (optional)")]
        public Sprite backgroundSprite;

        public override Texture mainTexture => backgroundSprite ? backgroundSprite.texture : s_WhiteTexture;

        [Header("Border Settings")]
        public float borderWidth = 0f;
        public Color borderColor = Color.black;

        [Header("Outline Only Mode")]
        [Tooltip("Если true, рисуем только ободок (border), без заливки; цвет ободка берётся из основного color.")]
        public bool outlineOnly = false;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;

            // Внешний контур
            var outer = GenerateBeveledPath(r, bevelSize);

            // Решаем, как рисовать: всегда рисуем ring, если есть borderWidth;
            // заливку — только если outlineOnly == false
            bool hasBorder = borderWidth > 0f;
            // Цвет ободка: если режим outlineOnly — берём главный цвет, иначе — borderColor
            Color ringColor = outlineOnly ? color : borderColor;

            if (hasBorder)
            {
                // внутренний контур для кольца
                Rect innerRect = new Rect(
                    r.xMin + borderWidth, r.yMin + borderWidth,
                    r.width - 2 * borderWidth, r.height - 2 * borderWidth);

                Vector4 innerBevel = new Vector4(
                    Mathf.Max(0, bevelSize.x - borderWidth),
                    Mathf.Max(0, bevelSize.y - borderWidth),
                    Mathf.Max(0, bevelSize.z - borderWidth),
                    Mathf.Max(0, bevelSize.w - borderWidth));

                var inner = GenerateBeveledPath(innerRect, innerBevel);

                // рисуем ободок
                AddRing(vh, outer, inner, ringColor);

                // и только если не в режиме outlineOnly — рисуем заливку
                if (!outlineOnly)
                {
                    var uvsInner = GetUVs(inner, innerRect, backgroundSprite);
                    AddFill(vh, inner, color, uvsInner);
                }
            }
            else
            {
                // нет borderWidth — рисуем заливку только если не outlineOnly
                if (!outlineOnly)
                {
                    var uvsOuter = GetUVs(outer, r, backgroundSprite);
                    AddFill(vh, outer, color, uvsOuter);
                }
            }
        }

        // Простая фаска без скруглений: 8 точек
        List<Vector2> GenerateBeveledPath(Rect r, Vector4 b)
        {
            return new List<Vector2>
            {
                new Vector2(r.xMin + b.x, r.yMax),
                new Vector2(r.xMax - b.y, r.yMax),
                new Vector2(r.xMax, r.yMax - b.y),
                new Vector2(r.xMax, r.yMin + b.z),
                new Vector2(r.xMax - b.z, r.yMin),
                new Vector2(r.xMin + b.w, r.yMin),
                new Vector2(r.xMin, r.yMin + b.w),
                new Vector2(r.xMin, r.yMax - b.x),
            };
        }

        // Заливка полигона
        void AddFill(VertexHelper vh, List<Vector2> pts, Color col, List<Vector2> uvs)
        {
            int start = vh.currentVertCount;
            for (int i = 0; i < pts.Count; i++)
                vh.AddVert(pts[i], col, uvs[i]);

            for (int i = 1; i < pts.Count - 1; i++)
                vh.AddTriangle(start + 0, start + i, start + i + 1);
        }

        // Ободок между outer и inner путями
        void AddRing(VertexHelper vh, List<Vector2> outer, List<Vector2> inner, Color col)
        {
            int n = Mathf.Min(outer.Count, inner.Count);
            int start = vh.currentVertCount;
            for (int i = 0; i < n; i++)
            {
                vh.AddVert(outer[i], col, Vector2.zero);
                vh.AddVert(inner[i], col, Vector2.zero);
            }

            for (int i = 0; i < n; i++)
            {
                int o0 = start + 2 * i, i0 = o0 + 1;
                int o1 = start + (2 * ((i + 1) % n)), i1 = o1 + 1;
                vh.AddTriangle(o0, o1, i1);
                vh.AddTriangle(o0, i1, i0);
            }
        }

        // Простой UV для списков точек
        List<Vector2> GetUVs(List<Vector2> pts, Rect r, Sprite s = null)
        {
            Vector2 uvMin = new Vector2(0, 0), uvMax = new Vector2(1, 1);
            if (s != null)
            {
                uvMin = new Vector2(s.rect.xMin / s.texture.width, s.rect.yMin / s.texture.height);
                uvMax = new Vector2(s.rect.xMax / s.texture.width, s.rect.yMax / s.texture.height);
            }

            var uvs = new List<Vector2>(pts.Count);
            for (int i = 0; i < pts.Count; i++)
            {
                var p = pts[i];
                float nx = Mathf.InverseLerp(r.xMin, r.xMax, p.x);
                float ny = Mathf.InverseLerp(r.yMin, r.yMax, p.y);
                uvs.Add(new Vector2(
                    Mathf.Lerp(uvMin.x, uvMax.x, nx),
                    Mathf.Lerp(uvMin.y, uvMax.y, ny)
                ));
            }

            return uvs;
        }
    }
}
