using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct FakeScrollRect
{
    public RectTransform content;
    public bool vertical;
    public bool horizontal;
}
namespace EventFramework
{
    public class ScrollViewHelper
    {
        UICommon m_owner = null;
        ScrollRect m_scrollView = null;
        FakeScrollRect m_fakeScrollView = new FakeScrollRect();
        RectTransform m_item; // 这个可能为null，表示子元素完全不确定
        List<UICommon> m_Children = new List<UICommon>();
        List<RectTransform> m_DirectItems = new List<RectTransform>();

        private RectOffset m_defaultMargin = new RectOffset(0, 0, 0, 0);
        public bool IsFixedPanel { get => m_scrollView != null; }
        public bool IsVertical { get => IsFixedPanel ? m_scrollView.vertical : m_fakeScrollView.vertical; }
        public bool IsHorizontal { get => IsFixedPanel ? m_scrollView.horizontal : m_fakeScrollView.horizontal; }
        RectTransform Content => m_scrollView != null ? m_scrollView.content : m_fakeScrollView.content;
        public int ItemCount => m_Children.Count;

        public List<UICommon> Items { get => m_Children; }
        public List<RectTransform> DirectItems { get => m_DirectItems; }

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
            m_fakeScrollView.content = content;
            m_fakeScrollView.vertical = isVertical;
            m_fakeScrollView.horizontal = isHorizontal;
            m_defaultMargin = defaultMargin ?? new RectOffset(0, 0, 0, 0);
        }

        public void ClearItems()
        {
            foreach (var child in m_Children)
            {
                child.Destroy();
            }
            m_Children.Clear();
            foreach (var directItem in m_DirectItems)
            {
                GameObject.Destroy(directItem.gameObject);
            }
            m_DirectItems.Clear();
        }

        public RectTransform AddItem(RectTransform item = null)
        {
            return AddItem<EmptyUICommon>(item).transform;
        }

        public T AddItem<T>(RectTransform item = null) where T : UICommon, new()
        {
            if (m_item == null && item == null)
            {
                return null;
            }
            var target = item ?? m_item;
            var newItem = UICommon.DynamicAttach<T>(m_owner, target.gameObject, Content);
            if (newItem != null)
            {
                m_Children.Add(newItem);
                m_DirectItems.Add(newItem.transform);
            }
            return newItem;
        }

        public T AddItemAsUICommonFather<T>(string uiName, RectTransform item = null, AspectRatioFitter.AspectMode aspectFit = AspectRatioFitter.AspectMode.None) where T : UICommon, new()
        {
            if (m_item == null && item == null)
            {
                return null;
            }
            var target = item ?? m_item;
            var newObject = GameObject.Instantiate(target.gameObject, Content);
            var newItem = UICommon.DynamicAttach<T>(m_owner, uiName, newObject.GetComponent<RectTransform>(), aspectFit: aspectFit);
            if (newItem != null)
            {
                m_Children.Add(newItem);
                m_DirectItems.Add(newObject.GetComponent<RectTransform>());
            }
            return newItem;
        }


        public void UpdateLayout(bool modifyContentSize = true)
        {
            if (IsVertical)
            {
                float totalHeight = 0f;
                int count = 0;
                foreach (var child in m_DirectItems)
                {
                    child.anchoredPosition = new Vector2(child.anchoredPosition.x, -(totalHeight + m_defaultMargin.top));
                    totalHeight += child.rect.height;
                    totalHeight += m_defaultMargin.vertical; // Add padding between items
                    count++;
                }
                if (modifyContentSize)
                    Content.sizeDelta = new Vector2(Content.sizeDelta.x, totalHeight);
            }
            else if (IsHorizontal)
            {
                float totalWidth = 0f;
                int count = 0;
                foreach (var child in m_DirectItems)
                {
                    child.anchoredPosition = new Vector2(totalWidth + m_defaultMargin.left, child.anchoredPosition.y);
                    totalWidth += child.rect.width;
                    totalWidth += m_defaultMargin.horizontal; // Add padding between items
                    count++;
                }
                if (modifyContentSize)
                    Content.sizeDelta = new Vector2(totalWidth, Content.sizeDelta.y);
            }
        }
    }
}