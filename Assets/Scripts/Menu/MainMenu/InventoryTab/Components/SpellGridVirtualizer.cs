using Data;
using Save;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace Menu.MainMenu
{
    /// <summary>
    /// Virtualize a Grid (fixed columns) for vertical scrolling.
    /// Reads GridLayoutGroup parameters then disables it and manages positions manually.
    /// Designed for fixed cell size (ex: 200x350) and fixed columns (ex: 5).
    /// </summary>
    public class SpellGridVirtualizer : MonoBehaviour
    {
        [Header("References")]
        public ScrollRect ScrollRect;                    // assign in inspector
        public RectTransform Content;                    // assign in inspector (ScrollRect.content)
        public GameObject ItemTemplate;                  // assign TemplateSpellItemUI prefab

        [Header("Grid settings (auto-read if a GridLayoutGroup exists)")]
        public int Columns = 5;
        public Vector2 CellSize = new Vector2(200, 350);
        public Vector2 Spacing = new Vector2(10, 10);
        public RectOffset Padding = new RectOffset(40, 40, 40, 40);

        [Header("Pooling")]
        [Tooltip("Number of extra lines to keep as buffer above/below viewport")]
        public int BufferLines = 2;

        // runtime
        List<SpellData> m_Items = new List<SpellData>();
        RectTransform[] m_Pool;                  // pooled item transforms
        TemplateSpellItemUI[] m_PoolViews;       // pooled item view components
        int m_PoolSize = 0;
        int m_TotalLines = 0;
        float m_LineHeight;
        float m_LineWidth;
        int m_VisibleLines = 0;
        int m_FirstShownLine = -1;               // currently shown first line index
        int m_LastShownLine = -1;

        GridLayoutGroup m_GridLayoutGroup;

        void Awake()
        {
            if (ScrollRect == null)
                Debug.LogError("SpellGridVirtualizer requires a ScrollRect reference.");

            if (Content == null && ScrollRect != null)
                Content = ScrollRect.content;

            // try to read GridLayoutGroup if exists
            m_GridLayoutGroup = Content.GetComponent<GridLayoutGroup>();
            if (m_GridLayoutGroup != null)
            {
                Columns = Mathf.Max(1, m_GridLayoutGroup.constraintCount == 0 ? Columns : m_GridLayoutGroup.constraintCount);
                CellSize = m_GridLayoutGroup.cellSize;
                Spacing = m_GridLayoutGroup.spacing;
                // GridLayoutGroup.padding is a RectOffset
                Padding = m_GridLayoutGroup.padding;
                // disable the layout group to avoid conflicts
                m_GridLayoutGroup.enabled = false;
            }

            m_LineHeight = CellSize.y + Spacing.y;
            m_LineWidth = CellSize.x + Spacing.x;

            ScrollRect.verticalNormalizedPosition = 1f;
            ScrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }

        void OnDestroy()
        {
            if (ScrollRect != null)
                ScrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
        }

        /// <summary>
        /// Provide the full (ordered) list of SpellData to display.
        /// The virtualizer will show them and recycle pooled items.
        /// </summary>
        public void SetItems(List<SpellData> items)
        {
            m_Items = items ?? new List<SpellData>();
            RebuildContent();
        }

        /// <summary>
        /// Force re-evaluation (call after unlocking / filtering changes).
        /// </summary>
        public void Refresh()
        {
            RebuildContent(updatePositionsOnly: false);
        }

        void RebuildContent(bool updatePositionsOnly = false)
        {
            // compute number of lines
            m_TotalLines = Mathf.CeilToInt((float)m_Items.Count / (float)Columns);

            // compute content height
            float contentHeight = Padding.top + Padding.bottom;
            if (m_TotalLines > 0)
                contentHeight += m_TotalLines * CellSize.y + Mathf.Max(0, m_TotalLines - 1) * Spacing.y;
            else
                contentHeight += 0;

            Content.anchorMin = new Vector2(0, 1);
            Content.anchorMax = new Vector2(1, 1);
            Content.pivot = new Vector2(0, 1);
            Content.sizeDelta = new Vector2(Content.sizeDelta.x, contentHeight);

            // compute visible lines in viewport
            float viewportHeight = ((RectTransform)ScrollRect.viewport).rect.height;
            m_VisibleLines = Mathf.CeilToInt(viewportHeight / m_LineHeight) + 1;

            int neededLines = m_VisibleLines + (BufferLines * 2);
            m_PoolSize = Mathf.Clamp(neededLines * Columns, Columns, Mathf.Max(Columns, neededLines * Columns));

            // create pool if needed
            if (m_Pool == null || m_Pool.Length != m_PoolSize)
            {
                // destroy existing pool objects
                if (m_Pool != null)
                {
                    for (int i = 0; i < m_Pool.Length; i++)
                    {
                        if (m_Pool[i] != null)
                            Destroy(m_Pool[i].gameObject);
                    }
                }

                m_Pool = new RectTransform[m_PoolSize];
                m_PoolViews = new TemplateSpellItemUI[m_PoolSize];

                for (int i = 0; i < m_PoolSize; i++)
                {
                    var go = Instantiate(ItemTemplate, Content);
                    go.name = $"VirtualItem_{i}";
                    var rt = go.GetComponent<RectTransform>();
                    // Set pivot to top-left for easier positioning
                    rt.pivot = new Vector2(0, 1);
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(0, 1);
                    rt.sizeDelta = new Vector2(CellSize.x, CellSize.y);
                    m_Pool[i] = rt;
                    m_PoolViews[i] = go.GetComponent<TemplateSpellItemUI>();
                    // Ensure template does not have expensive components that react to enabling/disabling
                }
            }

            // initial visible range reset
            m_FirstShownLine = -1;
            m_LastShownLine = -1;

            // position pool items offscreen initially
            for (int i = 0; i < m_PoolSize; i++)
            {
                m_Pool[i].anchoredPosition = new Vector2(-9999, -9999);
            }

            // update display
            UpdateVisibleItems();
        }

        void OnScrollValueChanged(Vector2 _)
        {
            UpdateVisibleItems();
        }

        void UpdateVisibleItems()
        {
            if (m_Items == null) return;

            // compute top-of-content offset relative to viewport
            RectTransform viewport = (RectTransform)ScrollRect.viewport;
            float contentTopY = Content.anchoredPosition.y; // positive when scrolled down
            // first visible line index
            int firstVisibleLine = Mathf.FloorToInt(contentTopY / m_LineHeight);
            firstVisibleLine = Mathf.Max(0, firstVisibleLine - BufferLines);
            int lastVisibleLine = firstVisibleLine + m_VisibleLines + (BufferLines * 2);
            lastVisibleLine = Mathf.Min(m_TotalLines - 1, lastVisibleLine);

            // if nothing changed, early out
            if (firstVisibleLine == m_FirstShownLine && lastVisibleLine == m_LastShownLine)
                return;

            m_FirstShownLine = firstVisibleLine;
            m_LastShownLine = lastVisibleLine;

            int requiredCount = (lastVisibleLine - firstVisibleLine + 1) * Columns;
            requiredCount = Mathf.Min(requiredCount, m_Items.Count);

            // we'll map pool indexes to item indices deterministically to avoid heavy bookkeeping:
            // poolIndex = (lineIndex - firstVisibleLine) * Columns + column
            int poolIdx = 0;
            for (int line = firstVisibleLine; line <= lastVisibleLine; line++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    int itemIndex = line * Columns + col;
                    if (itemIndex >= m_Items.Count)
                    {
                        // off-range: move remaining pooled items offscreen
                        if (poolIdx < m_PoolSize)
                            m_Pool[poolIdx].anchoredPosition = new Vector2(-9999, -9999);
                        poolIdx++;
                        continue;
                    }

                    if (poolIdx >= m_PoolSize)
                        continue;

                    // compute position
                    float x = Padding.left + col * (CellSize.x + Spacing.x);
                    float y = -(Padding.top + line * (CellSize.y + Spacing.y));

                    // assign pool item
                    var rt = m_Pool[poolIdx];
                    rt.anchoredPosition = new Vector2(x, y);
                    // update data
                    var view = m_PoolViews[poolIdx];
                    var sd = m_Items[itemIndex];
                    bool unlocked = InventoryCloudData.Instance.GetSpell(sd.Spell).Level > 0;
                    bool inBuild = CharacterBuildsCloudData.CurrentSpells.Contains(sd.Spell);
                    view.SetData(sd, unlocked, inBuild);

                    poolIdx++;
                }
            }

            // move leftover pool entries offscreen
            for (int i = poolIdx; i < m_PoolSize; i++)
            {
                m_Pool[i].anchoredPosition = new Vector2(-9999, -9999);
            }
        }
    }
}
