using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

[ExecuteAlways]
public class DoubleImage : Image
{
    [SerializeField] private Sprite m_SecondSprite;
    [SerializeField] private Color m_FirstColor;
    [SerializeField] private Color m_SecondColor;
    private Material m_DoubleImageMaterial;

    public Sprite secondSprite
    {
        get => m_SecondSprite;
        set
        {
            m_SecondSprite = value;
            UpdateMaterialProperties();
            SetAllDirty();
        }
    }

    public new Color color
    {
        get => m_FirstColor;
        set
        {
            m_FirstColor = value;
            UpdateMaterialProperties();
        }
    }

    public Color secondColor
    {
        get => m_SecondColor;
        set
        {
            m_SecondColor = value;
            UpdateMaterialProperties();
        }
    }

    protected override void Start()
    {
        base.Start();
        InitMaterial();
        UpdateMaterialProperties();
    }

    private void InitMaterial()
    {
        Shader shader = Shader.Find("Shader Graphs/DoubleImage");
        if (shader != null)
        {
            m_DoubleImageMaterial = new Material(shader);
            material = m_DoubleImageMaterial;
        }
        else
        {
            Debug.LogError("Shader Graphs/DoubleImage not found");
        }
    }

    private void UpdateMaterialProperties()
    {
        if (m_DoubleImageMaterial != null)
        {
            if (secondSprite != null)
            {
                m_DoubleImageMaterial.SetTexture("_SecondTex", secondSprite.texture);
                m_DoubleImageMaterial.SetColor("_Color", m_FirstColor);
                m_DoubleImageMaterial.SetColor("_SecondColor", m_SecondColor);
            }
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (!Application.isPlaying && m_DoubleImageMaterial != null)
        {
            UpdateMaterialProperties();
        }
    }
#endif

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        if (overrideSprite == null || m_SecondSprite == null)
        {
            base.OnPopulateMesh(vh);
            return;
        }

        Vector4 outerUV1 = DataUtility.GetOuterUV(overrideSprite);
        Vector4 outerUV2 = DataUtility.GetOuterUV(m_SecondSprite);

        Rect r = GetPixelAdjustedRect();
        Vector4 padding1 = DataUtility.GetPadding(overrideSprite);
        Vector4 padding2 = DataUtility.GetPadding(m_SecondSprite);

        Vector2 spriteSize1 = new Vector2(overrideSprite.rect.width, overrideSprite.rect.height);
        Vector2 spriteSize2 = new Vector2(m_SecondSprite.rect.width, m_SecondSprite.rect.height);

        Vector2 minSize = new Vector2(
            Mathf.Max(spriteSize1.x - padding1.x - padding1.z, spriteSize2.x - padding2.x - padding2.z),
            Mathf.Max(spriteSize1.y - padding1.y - padding1.w, spriteSize2.y - padding2.y - padding2.w));
        
        float width = minSize.x / pixelsPerUnit;
        float height = minSize.y / pixelsPerUnit;

        Vector2 size = new Vector2(width, height);
        Vector4 v = GetDrawingDimensions(false);

        Vector4 outer = new Vector4(outerUV1.x, outerUV1.y, outerUV2.z, outerUV2.w);

        vh.Clear();

        Color32 color32 = Color.white;
        vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(outer.x, outer.y));
        vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(outer.x, outer.w));
        vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(outer.z, outer.w));
        vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(outer.z, outer.y));

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }

    private Vector4 GetDrawingDimensions(bool shouldPreserveAspect)
    {
        Vector4 padding = overrideSprite == null ? Vector4.zero : DataUtility.GetPadding(overrideSprite);
        Vector2 size = overrideSprite == null ? Vector2.zero : new Vector2(overrideSprite.rect.width, overrideSprite.rect.height);

        Rect r = GetPixelAdjustedRect();

        int spriteW = Mathf.RoundToInt(size.x);
        int spriteH = Mathf.RoundToInt(size.y);

        Vector4 v = new Vector4(
            padding.x / spriteW,
            padding.y / spriteH,
            (spriteW - padding.z) / spriteW,
            (spriteH - padding.w) / spriteH);

        if (shouldPreserveAspect && size.sqrMagnitude > 0.0f)
        {
            PreserveSpriteAspectRatio(ref r, size);
        }

        v = new Vector4(
            r.x + r.width * v.x,
            r.y + r.height * v.y,
            r.x + r.width * v.z,
            r.y + r.height * v.w
        );

        return v;
    }

    private void PreserveSpriteAspectRatio(ref Rect rect, Vector2 spriteSize)
    {
        float spriteRatio = spriteSize.x / spriteSize.y;
        float rectRatio = rect.width / rect.height;

        if (spriteRatio > rectRatio)
        {
            float oldHeight = rect.height;
            rect.height = rect.width * (1.0f / spriteRatio);
            rect.y += (oldHeight - rect.height) * rectTransform.pivot.y;
        }
        else
        {
            float oldWidth = rect.width;
            rect.width = rect.height * spriteRatio;
            rect.x += (oldWidth - rect.width) * rectTransform.pivot.x;
        }
    }
}
