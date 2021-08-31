using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;

// 支持在UI上渲染同一个图集中的美术字
public class UIArtisticText : MaskableGraphic {
    [SerializeField]
    float m_Scale = 1;
    [SerializeField]
    TextAnchor m_Alignment = TextAnchor.UpperLeft;
    [SerializeField]
    HorizontalWrapMode m_HorizontalOverflow = HorizontalWrapMode.Overflow;
    [SerializeField]
    VerticalWrapMode m_VerticalOverflow = VerticalWrapMode.Truncate;
    [SerializeField]
    float m_Spacing = 10;
    [SerializeField]
    float m_LineSpacing = 1;
    [SerializeField]
    Sprite[] m_SpriteArray;
    class Item
    {
        public bool isVisable;
        public Vector2 pos0;
        public Vector2 pos1;
        public Vector2 pos2;
        public Vector2 pos3;
        public Vector2 pixelSize;
        public Vector2[] uvArray;
    }
    struct LineData
    {
        public Vector2 startPos;
        public int itemCount;
        public float maxWidth;
        public float maxItemHeight;
    }
    Item[] m_ItemArray = null;
    Texture2D m_TexAtlas;
    bool m_IsNeedReCalPos = true;
    Vector2 m_ActualRenderSize = new Vector2(0, 0);

    const int kMaxLineCount = 100;
    static UIVertex[] st_FourVertexArray = new UIVertex[4];
    static LineData[] st_LineDataArray = new LineData[kMaxLineCount];

    public override Texture mainTexture
    {
        get
        {
            return m_TexAtlas;
        }
    }
    protected override void Awake()
    {
        base.Awake();
        SetSpriteArray(m_SpriteArray);
    }
    protected override void UpdateGeometry()
    {
        if (m_IsNeedReCalPos) {
            RecalItemListPos();
        }
        base.UpdateGeometry();
        m_IsNeedReCalPos = true;
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        int totalItemCount = m_ItemArray != null ? m_ItemArray.Length : 0;
        if(!this.isActiveAndEnabled || totalItemCount <= 0) {
            return;
        }
        // if (Debug.isDebugBuild) {
        //     Profiler.BeginSample("UIArtisticText.OnPopulateMesh", this.gameObject);
        // }
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = base.color;
        for(int idx = 0; idx < totalItemCount; ++idx) {
            Item item = m_ItemArray[idx];
            if(!item.isVisable) continue;
            vertex.position = item.pos0;
            vertex.uv0 = item.uvArray[0];
            st_FourVertexArray[0] = vertex;
            vertex.position = item.pos1;
            vertex.uv0 = item.uvArray[1];
            st_FourVertexArray[1] = vertex;
            vertex.position = item.pos2;
            vertex.uv0 = item.uvArray[2];
            st_FourVertexArray[2] = vertex;
            vertex.position = item.pos3;
            vertex.uv0 = item.uvArray[3];
            st_FourVertexArray[3] = vertex;
            vh.AddUIVertexQuad(st_FourVertexArray);
        }
        // if (Debug.isDebugBuild) {
        //     Profiler.EndSample();
        // } 
    }
    void GetRectMaxSize(out float width, out float height)
    {
        Rect curRect = base.rectTransform.rect;
        const float kMaxSize = 999999;
        width = m_HorizontalOverflow == HorizontalWrapMode.Overflow ? kMaxSize : curRect.width;
        height = m_VerticalOverflow == VerticalWrapMode.Overflow ? kMaxSize : curRect.height;
    }
    void RecalItemListPos()
    {
        int totalItemCount = m_ItemArray != null ? m_ItemArray.Length : 0;
        m_ActualRenderSize = Vector2.zero;
        if(totalItemCount <= 0) {
            return;
        }
        // if (Debug.isDebugBuild) {
        //     Profiler.BeginSample("UIArtisticText.RecalItemListPos", this.gameObject);
        // }
        float rectWidth;
        float rectHeight;
        GetRectMaxSize(out rectWidth, out rectHeight);
        //Debug.Log("lineMaxItemCount-->" + lineMaxItemCount);
        int totalLineCount = 0;
        float totalLineHeight = 0;
        float lastTotalWidth = 0 - m_Spacing;
        float lastLineMaxItemHeight = 0;
        int lineItemCount = 0;
        for (int idx = 0; idx < totalItemCount; ++idx) {
            Item item = m_ItemArray[idx];
            float itemWidth = item.pixelSize.x * m_Scale;
            float itemHeight = item.pixelSize.y * m_Scale;
            float curWidth = lastTotalWidth + m_Spacing + itemWidth;
            if(curWidth > rectWidth) {
                if(totalLineCount + 1 >= kMaxLineCount || itemWidth > rectWidth) {
                    break;
                }
                float needLineHeight = lastLineMaxItemHeight + m_LineSpacing;
                totalLineHeight += needLineHeight;
                if(totalLineHeight > rectHeight) {
                    totalLineHeight -= needLineHeight;
                    break;
                }
                else ++totalLineCount;
                st_LineDataArray[totalLineCount - 1] = new LineData() {
                    itemCount = idx == 0 ? 1 : lineItemCount,
                    maxWidth = idx == 0 ? itemWidth : lastTotalWidth,
                    maxItemHeight = idx == 0 ? itemHeight : lastLineMaxItemHeight,
                };
                curWidth = itemWidth;
                lastLineMaxItemHeight = 0;
                lineItemCount = 0;
            }
            ++lineItemCount;
            lastLineMaxItemHeight = Mathf.Max(itemHeight, lastLineMaxItemHeight);
            lastTotalWidth = curWidth;
            if (idx + 1 == totalItemCount) {
                totalLineHeight += lastLineMaxItemHeight;
                if(totalLineHeight > rectHeight) {
                    totalLineHeight -= lastLineMaxItemHeight;
                    break;
                }
                st_LineDataArray[totalLineCount++] = new LineData() {
                    itemCount = lineItemCount,
                    maxWidth = lastTotalWidth,
                    maxItemHeight = lastLineMaxItemHeight,
                };
            }
        }
        //Debug.Log("totalLineCount-->" + totalLineCount + "##" + totalLineHeight);
        SetStartPivotPos(totalLineCount, totalLineHeight, ref st_LineDataArray);
        //Vector2 kClipOffset = new Vector2(0.01f, 0.01f);
        //Vector2 clipMinPos = curRect.min - kClipOffset;
        //Vector2 clipMaxPos = curRect.max + kClipOffset;
        int curItemIndex = 0;
        for(int lineIndex = 0; lineIndex < totalLineCount; ++lineIndex) {
            LineData lineData = st_LineDataArray[lineIndex];
            float lineStartPosX = lineData.startPos.x;
            float lineStartPosY = lineData.startPos.y;
            float curOffsetX = 0;
            //Debug.Log(lineIndex + "-->" + lineData.itemCount);
            for(int idx = 0; idx < lineData.itemCount; ++idx) {
                Item item = m_ItemArray[curItemIndex++];
                float itemWidth = item.pixelSize.x * m_Scale;
                float itemHeight = item.pixelSize.y * m_Scale;
                float quadStartPosX = lineStartPosX + curOffsetX;
                curOffsetX += itemWidth + m_Spacing;
                float quadMaxX = quadStartPosX + itemWidth;
                float quadMaxY = lineStartPosY + itemHeight;
                item.isVisable = true;
                //item.isVisable = quadStartPos.x >= clipMinPos.x && quadMaxX <= clipMaxPos.x &&
                //    quadStartPos.y >= clipMinPos.y && quadMaxY <= clipMaxPos.y;
                //if(!item.isVisable) {
                //    continue;
                //}
                item.pos0 = new Vector2(quadStartPosX, lineStartPosY);
                item.pos1 = new Vector2(quadStartPosX, quadMaxY);
                item.pos2 = new Vector2(quadMaxX, quadMaxY);
                item.pos3 = new Vector2(quadMaxX, lineStartPosY);
            }
        }
        for(int index = curItemIndex; index < totalItemCount; ++index) {
            Item item = m_ItemArray[index];
            item.isVisable = false;
        }
        // if (Debug.isDebugBuild) {
        //     Profiler.EndSample();
        // }
    }

    void SetStartPivotPos(int lineCount, float totalLineHeight, ref LineData[] lineDataArray)
    {
        if(lineCount <= 0) {
            return;
        }
        Rect rect = base.rectTransform.rect;
        float lastLinePosY = 0;
        m_ActualRenderSize.y = totalLineHeight;
        for (int idx = 0; idx < lineCount; ++idx) {
            LineData lineData = lineDataArray[idx];
            float lineWidth = lineData.maxWidth;
            float lineHeight = lineData.maxItemHeight;
            m_ActualRenderSize.x = Mathf.Max(lineWidth, m_ActualRenderSize.x);
            if (m_Alignment == TextAnchor.LowerLeft) {
                lineData.startPos.x = rect.xMin;
                lastLinePosY = idx == 0 ? (rect.yMin + totalLineHeight - lineHeight) : lastLinePosY;
                lineData.startPos.y = lastLinePosY;
            }
            else if (m_Alignment == TextAnchor.MiddleLeft) {
                lineData.startPos.x = rect.xMin;
                lastLinePosY = idx == 0 ? (totalLineHeight * 0.5f - lineHeight) : lastLinePosY;
                lineData.startPos.y = lastLinePosY;
            }
            else if (m_Alignment == TextAnchor.UpperLeft) {
                lineData.startPos.x = rect.xMin;
                lastLinePosY = idx == 0 ? (rect.yMax - lineHeight) : lastLinePosY;
                lineData.startPos.y = lastLinePosY;
            }
            else if (m_Alignment == TextAnchor.LowerCenter) {
                lineData.startPos.x = 0 - lineWidth * 0.5f;
                lastLinePosY = idx == 0 ? (rect.yMin + totalLineHeight - lineHeight) : lastLinePosY;
                lineData.startPos.y = lastLinePosY;
            }
            else if (m_Alignment == TextAnchor.MiddleCenter) {
                lineData.startPos.x = 0 - lineWidth * 0.5f;
                lastLinePosY = idx == 0 ? (totalLineHeight * 0.5f - lineHeight) : lastLinePosY;
                lineData.startPos.y = lastLinePosY;
            }
            else if (m_Alignment == TextAnchor.UpperCenter) {
                lineData.startPos.x = 0 - lineWidth * 0.5f;
                lastLinePosY = idx == 0 ? (rect.yMax - lineHeight) : lastLinePosY;
                lineData.startPos.y = lastLinePosY;
            }
            else if (m_Alignment == TextAnchor.LowerRight) {
                lineData.startPos.x = rect.xMax - lineWidth;
                lastLinePosY = idx == 0 ? (rect.yMin + totalLineHeight - lineHeight) : lastLinePosY;
                lineData.startPos.y = lastLinePosY;
            }
            else if (m_Alignment == TextAnchor.MiddleRight) {
                lineData.startPos.x = rect.xMax - lineWidth;
                lastLinePosY = idx == 0 ? (totalLineHeight * 0.5f - lineHeight) : lastLinePosY;
                lineData.startPos.y = lastLinePosY;
            }
            else if (m_Alignment == TextAnchor.UpperRight) {
                lineData.startPos.x = rect.xMax - lineWidth;
                lastLinePosY = idx == 0 ? (rect.yMax - lineHeight) : lastLinePosY;
                lineData.startPos.y = lastLinePosY;
            }
            lastLinePosY -= lineHeight + m_LineSpacing;
            lineDataArray[idx] = lineData;
        }
    }


    #region//接口
    public void ForceImmediateUpdateLayout()
    {
        base.SetVerticesDirty();
        RecalItemListPos();
        m_IsNeedReCalPos = false;
    }
    public void SetSpriteArray(Sprite[] spriteArray)
    {
        m_ItemArray = null;
        m_TexAtlas = null;
        m_SpriteArray = spriteArray;
        int spriteCount = m_SpriteArray != null ? m_SpriteArray.Length : 0;
        // Debug.Log(spriteCount);
        if(spriteCount == 0) {
            return;
        }
        List<Item> itemList = new List<Item>(spriteCount);
        for(int idx = 0; idx < spriteCount; ++idx) {
            Sprite sprite = m_SpriteArray[idx];
            if(sprite == null) continue;
            // print(sprite.name);
            if(idx == 0) {
                m_TexAtlas = sprite.texture;
            }
            else if(m_TexAtlas != sprite.texture) {
                throw new System.Exception("Sprite不在同一个图集内，不能合批成一次渲染！");
            }
            Rect pixelRect = sprite.rect;
            float pixelWidth = pixelRect.width;
            float pixelHeight = pixelRect.height;
            if(pixelWidth <= 0 || pixelHeight <= 0) {
                throw new System.Exception("Sprite尺寸异常！");
            }
            float uWidth = pixelWidth / m_TexAtlas.width;
            float vHeight = pixelHeight / m_TexAtlas.height;
            Vector2 startUV = new Vector2(pixelRect.x / m_TexAtlas.width, pixelRect.y / m_TexAtlas.height);
            Vector2[] uvArray = new Vector2[] {
                startUV,
                startUV + new Vector2(0, vHeight),
                startUV + new Vector2(uWidth, vHeight),
                startUV + new Vector2(uWidth, 0),
            };
            Item item = new Item() {
                pixelSize = new Vector2(pixelWidth, pixelHeight),
                uvArray = uvArray,
            };
            itemList.Add(item);
        }
        m_ItemArray = itemList.ToArray();
        ForceImmediateUpdateLayout();
        base.SetMaterialDirty();
    }
    public float scale {
       get { return m_Scale; }
       set { m_Scale = Mathf.Max(0.1f, value);}
    }
    
    public float spacing {
       get { return m_Spacing; }
       set { m_Spacing = value;}
    }
    public float lineSpacing {
       get { return m_LineSpacing; }
       set { m_LineSpacing = value;}
    }
    public TextAnchor alignment {
       get { return m_Alignment; }
       set { m_Alignment = value;}
    }
    public HorizontalWrapMode horizontalOverflow {
       get { return m_HorizontalOverflow; }
       set { m_HorizontalOverflow = value;}
    }
    public VerticalWrapMode verticalOverflow {
       get { return m_VerticalOverflow; }
       set { m_VerticalOverflow = value;}
    }
    public Vector2 getActualRenderSize
    {
        get { return m_ActualRenderSize; }
    }
    #endregion


#if UNITY_EDITOR

    protected override void OnValidate()
    {
        m_Scale = Mathf.Max(0.1f, m_Scale);
        this.SetSpriteArray(m_SpriteArray);
        base.OnValidate();
    }
#endif
}