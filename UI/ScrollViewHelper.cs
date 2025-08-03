using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollViewHelper
{
    UICommon m_owner;
    ScrollRect m_scrollView;
    RectTransform m_content;
    RectTransform m_item; // 这个可能为null，表示子元素完全不确定
    List<RectTransform> m_Children = new List<RectTransform>();

    private bool m_isVertical = true;
    private bool m_isHorizontal = false;
    private RectOffset m_defaultMargin = new RectOffset(0, 0, 0, 0);
    public bool IsFixedPanel { get => m_scrollView != null; }
    public bool IsVertical { get => IsFixedPanel ? m_scrollView.vertical : m_isVertical; }
    public bool IsHorizontal { get => IsFixedPanel ? m_scrollView.horizontal : m_isHorizontal; }
    public int ItemCount => m_Children.Count;
    public List<RectTransform> Items { get => m_Children; }

    public ScrollViewHelper(
        UICommon owner,
        ScrollRect scrollView,
        RectTransform item,
        RectOffset defaultMargin = null
    )
    {
        m_owner = owner;
        m_item = item;
        m_scrollView = scrollView;
        m_content = scrollView.content;
        m_defaultMargin = defaultMargin ?? new RectOffset(0, 0, 0, 0);
    }

    public ScrollViewHelper(
        UICommon owner,
        RectTransform content,
        RectTransform item,
        bool isVertical = true,
        bool isHorizontal = false,
        RectOffset defaultMargin = null
    )
    {
        m_owner = owner;
        m_item = item;
        m_scrollView = null;
        m_content = content;
        m_defaultMargin = defaultMargin ?? new RectOffset(0, 0, 0, 0);
        m_isVertical = isVertical;
        m_isHorizontal = isHorizontal;
    }
    
    public void ClearItems()
    {
        foreach (RectTransform child in m_Children)
        {
            GameObject.Destroy(child.gameObject);
        }
        m_Children.Clear();
    }

    public RectTransform AddItem(RectTransform item = null, bool activeItem=true)
    {
        if (m_item == null && item == null)
        {
            return null;
        }
        var target = item ?? m_item;
        RectTransform newItem = GameObject.Instantiate(target);
        Helpers.RecursionOnLoad(newItem.gameObject);

        newItem.SetParent(m_content, false);
        newItem.gameObject.SetActive(activeItem); // Show the new item
        m_Children.Add(newItem);
        return newItem;
    }

    public void UpdateLayout(bool modifyContentSize = true)
    {   
        if (IsVertical)
        {
            float totalHeight = 0f;
            int count = 0;
            foreach (RectTransform child in m_Children)
            {
                
                child.anchoredPosition = new Vector2(child.anchoredPosition.x, -(totalHeight + m_defaultMargin.top));
                totalHeight += child.rect.height;
                totalHeight += m_defaultMargin.vertical; // Add padding between items
                count++;
            }
            if (modifyContentSize)
                m_content.sizeDelta = new Vector2(m_content.sizeDelta.x, totalHeight);
        }
        else if (IsHorizontal)
        {
            float totalWidth = 0f;
            int count = 0;
            foreach (RectTransform child in m_Children)
            {
                child.anchoredPosition = new Vector2(totalWidth + m_defaultMargin.left, child.anchoredPosition.y);
                totalWidth += child.rect.width;
                totalWidth += m_defaultMargin.horizontal; // Add padding between items
                count++;
            }
            if (modifyContentSize)
                m_content.sizeDelta = new Vector2(totalWidth, m_content.sizeDelta.y);
        }
    }
}
