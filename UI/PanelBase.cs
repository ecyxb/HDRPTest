using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace MyFrameWork
{
    public class PanelBase : UICommon
    {
        // 默认使用根Canvas
        public override CanvasType CanvasType { get; } = CanvasType.RootOverlay;
        // 标志位，表示是否需要在新的canvas下
        protected RootCanvasConfig rootCanvasConfig = null;

        public static T InitPanel<T>(string panelName, Canvas canvas, RootCanvasConfig rootCanvasConfig = null) where T : PanelBase, new()
        {
            try
            {
                if (canvas == null)
                {
                    Debug.LogError($"Canvas is null for panel {panelName}");
                    return null;
                }

                GameObject prefab = AOTHelpers.FindUIPrefab(panelName);
                if (prefab == null)
                {
                    Debug.LogError($"UI prefab {panelName} not found in Resources/UIPrefabs.");
                    return null;
                }

                RectTransform rectTransform = GameObject.Instantiate(prefab)?.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    Debug.LogError($"Failed to instantiate UI prefab {panelName}.");
                    return null;
                }

                T panel = new T();
                panel._createNewCanvas = createNewCanvas;
                
                // 根据标志位决定Canvas的创建方式
                if (createNewCanvas)
                {
                    // 创建祖先Canvas
                    var rootCanvas = UIManager.Instance.CreateRootCanvas(new RootCanvasConfig 
                    { 
                        canvasType = panel.CanvasType,
                        renderMode = canvas.renderMode
                    });
                    if (rootCanvas != null)
                    {
                        panel._canvas = rootCanvas;
                        rectTransform.SetParent(rootCanvas.transform, false);
                    }
                    else
                    {
                        // 如果创建失败，使用默认Canvas
                        panel._canvas = canvas;
                        rectTransform.SetParent(canvas.transform, false);
                    }
                }
                else
                {
                    // 在_defaultCanvas中寻找对应的canvas
                    if (UIManager.Instance._defaultCanvas.TryGetValue(panel.CanvasType, out var defaultCanvas))
                    {
                        panel._canvas = defaultCanvas;
                        rectTransform.SetParent(defaultCanvas.transform, false);
                    }
                    else
                    {
                        // 如果找不到，使用传入的Canvas
                        panel._canvas = canvas;
                        rectTransform.SetParent(canvas.transform, false);
                    }
                }
                
                // 设置面板属性
                panel.transform = rectTransform;
                panel.gameObject.name = panelName;
                
                // 初始化快捷方式
                InitUICommonShortcuts(panel, prefab);
                
                // 调用加载完成事件
                panel.__on_load__();
                panel._isInitialized = true;

                return panel;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize panel {panelName}: {e.Message}");
                return null;
            }
        }

        public override void Show()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"Panel {gameObject?.name} is not initialized");
                return;
            }

            try
            {
                base.Show();
                OnShow();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error showing panel {gameObject?.name}: {e.Message}");
            }
        }

        public override void Hide()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"Panel {gameObject?.name} is not initialized");
                return;
            }

            try
            {
                base.Hide();
                OnHide();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error hiding panel {gameObject?.name}: {e.Message}");
            }
        }

        public override void Destroy()
        {
            if (!_isInitialized)
            {
                return;
            }

            try
            {
                // 先隐藏面板
                Hide();
                
                // 调用销毁前事件
                OnDestroy();
                
                // 销毁Unity对象
                RectTransform root = transform;
                base.Destroy();
                
                if (root != null && root.gameObject != null)
                {
                    GameObject.Destroy(root.gameObject);
                }
                
                // 清理引用
                _canvas = null;
                _isInitialized = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error destroying panel {gameObject?.name}: {e.Message}");
            }
        }

        /// <summary>
        /// 设置面板的Canvas
        /// </summary>
        public void SetCanvas(Canvas newCanvas)
        {
            if (newCanvas == null || newCanvas == _canvas)
            {
                return;
            }

            try
            {
                if (transform != null)
                {
                    transform.SetParent(newCanvas.transform, false);
                }
                _canvas = newCanvas;
                
                // 更新子Canvas的引用
                if (_childCanvases != null)
                {
                    foreach (var childCanvas in _childCanvases.Values)
                    {
                        if (childCanvas != null)
                        {
                            childCanvas.transform.SetParent(newCanvas.transform, false);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error setting canvas for panel {gameObject?.name}: {e.Message}");
            }
        }

        /// <summary>
        /// 设置面板的排序层级
        /// </summary>
        public void SetSortingOrder(int order)
        {
            try
            {
                if (transform != null)
                {
                    var canvas = transform.GetComponent<Canvas>();
                    if (canvas != null)
                    {
                        canvas.sortingOrder = order;
                    }
                    else
                    {
                        // 如果没有Canvas组件，设置siblingIndex
                        transform.SetSiblingIndex(order);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error setting sorting order for panel {gameObject?.name}: {e.Message}");
            }
        }

        /// <summary>
        /// 将面板置顶
        /// </summary>
        public void BringToFront()
        {
            try
            {
                if (transform != null)
                {
                    transform.SetAsLastSibling();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error bringing panel {gameObject?.name} to front: {e.Message}");
            }
        }

        /// <summary>
        /// 将面板置底
        /// </summary>
        public void SendToBack()
        {
            try
            {
                if (transform != null)
                {
                    transform.SetAsFirstSibling();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error sending panel {gameObject?.name} to back: {e.Message}");
            }
        }

        /// <summary>
        /// 获取面板在世界空间中的位置
        /// </summary>
        public Vector3 GetWorldPosition()
        {
            if (transform == null || _canvas == null)
            {
                return Vector3.zero;
            }

            try
            {
                if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    // Screen Space Overlay模式下的转换
                    return UIManager.Instance.UIWorldPos2ScreenPos(transform.position, CanvasType);
                }
                else
                {
                    // 其他模式直接返回位置
                    return transform.position;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting world position for panel {gameObject?.name}: {e.Message}");
                return Vector3.zero;
            }
        }

        /// <summary>
        /// 设置面板在世界空间中的位置
        /// </summary>
        public void SetWorldPosition(Vector3 worldPosition)
        {
            if (transform == null || _canvas == null)
            {
                return;
            }

            try
            {
                if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    // Screen Space Overlay模式下的转换
                    var screenPos = UIManager.Instance.ScreenPos2UIWorldPos(worldPosition, transform, CanvasType);
                    transform.position = screenPos;
                }
                else
                {
                    // 其他模式直接设置位置
                    transform.position = worldPosition;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error setting world position for panel {gameObject?.name}: {e.Message}");
            }
        }

        // 生命周期方法 - 子类可以重写
        protected override void OnShow() { }
        protected override void OnHide() { }
        protected virtual void OnDestroy() { }

        /// <summary>
        /// 安全地获取子UI组件
        /// </summary>
        public T GetUIComponent<T>(string path) where T : Component
        {
            try
            {
                var rectTransform = this[path];
                if (rectTransform != null)
                {
                    return rectTransform.GetComponent<T>();
                }
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting UI component {path} for panel {gameObject?.name}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 安全地获取子UI组件（如果不存在则添加）
        /// </summary>
        public T GetOrAddUIComponent<T>(string path) where T : Component
        {
            try
            {
                var rectTransform = this[path];
                if (rectTransform != null)
                {
                    var component = rectTransform.GetComponent<T>();
                    if (component == null)
                    {
                        component = rectTransform.gameObject.AddComponent<T>();
                    }
                    return component;
                }
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting or adding UI component {path} for panel {gameObject?.name}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 在指定Canvas类型下创建UI元素
        /// </summary>
        public new T CreateUIInCanvas<T>(string prefabPath, string attachPosName, CanvasType canvasType = CanvasType.RootOverlay) where T : UICommon, new()
        {
            return base.CreateUIInCanvas<T>(prefabPath, attachPosName, canvasType);
        }

        /// <summary>
        /// 在指定Canvas类型下创建动态列表
        /// </summary>
        public DynamicList<TItem> CreateDynamicList<TItem>(string itemPrefabPath, string containerName, CanvasType canvasType = CanvasType.RootOverlay) 
            where TItem : UICommon, new()
        {
            try
            {
                // 获取或创建容器
                var container = this[containerName];
                if (container == null)
                {
                    Debug.LogError($"Container {containerName} not found in panel {gameObject?.name}");
                    return null;
                }

                // 创建动态列表
                var dynamicList = new DynamicList<TItem>(itemPrefabPath, container as RectTransform, canvasType);
                if (dynamicList != null)
                {
                    dynamicList.transform.SetParent(container, false);
                    dynamicList.gameObject.name = $"DynamicList_{containerName}";
                }
                
                return dynamicList;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error creating dynamic list in panel {gameObject?.name}: {e.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// 动态列表类，用于管理大量动态UI元素
    /// </summary>
    public class DynamicList<TItem> : UICommon where TItem : UICommon, new()
    {
        private string _itemPrefabPath;
        private RectTransform _container;
        private CanvasType _canvasType;
        private List<TItem> _items = new List<TItem>();
        private Dictionary<int, TItem> _activeItems = new Dictionary<int, TItem>();
        private int _visibleItemCount = 0;
        private float _itemHeight = 0f;
        private new bool _isInitialized = false;

        public string ItemPrefabPath => _itemPrefabPath;
        public RectTransform Container => _container;
        public CanvasType CanvasType => _canvasType;
        public List<TItem> Items => _items;
        public Dictionary<int, TItem> ActiveItems => _activeItems;
        public int VisibleItemCount => _visibleItemCount;
        public int TotalItemCount => _items.Count;

        public DynamicList(string itemPrefabPath, RectTransform container, CanvasType canvasType = CanvasType.RootOverlay)
        {
            _itemPrefabPath = itemPrefabPath;
            _container = container;
            _canvasType = canvasType;
        }

        public override void Show()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"Dynamic list {gameObject?.name} is not initialized");
                return;
            }

            try
            {
                base.Show();
                OnShow();
            }
            catch (Exception e)
        {
                Debug.LogError($"Error showing dynamic list {gameObject?.name}: {e.Message}");
            }
        }

        public override void Hide()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"Dynamic list {gameObject?.name} is not initialized");
                return;
            }

            try
            {
                base.Hide();
                OnHide();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error hiding dynamic list {gameObject?.name}: {e.Message}");
            }
        }

        public override void Destroy()
        {
            if (!_isInitialized)
            {
                return;
            }

            try
            {
                // 清理所有项目
                foreach (var item in _items)
                {
                    if (item != null && !item.IsDestroyed)
                    {
                        item.Destroy();
                    }
                }
                _items.Clear();
                _activeItems.Clear();

                base.Destroy();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error destroying dynamic list {gameObject?.name}: {e.Message}");
            }
        }

        /// <summary>
        /// 初始化动态列表
        /// </summary>
        public void Initialize(int itemCount, float itemHeight = 100f, int visibleItemCount = 10)
        {
            try
            {
                if (_isInitialized)
                {
                    Debug.LogWarning($"Dynamic list {gameObject?.name} is already initialized");
                    return;
                }

                _itemHeight = itemHeight;
                _visibleItemCount = visibleItemCount;
                _isInitialized = true;

                // 创建项目池
                for (int i = 0; i < itemCount; i++)
                {
                    var item = CreateItem(i);
                    if (item != null)
                    {
                        _items.Add(item);
                    }
                }

                // 初始显示可见项目
                UpdateVisibleItems(0);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error initializing dynamic list {gameObject?.name}: {e.Message}");
            }
        }

        /// <summary>
        /// 创建列表项
        /// </summary>
        private TItem CreateItem(int index)
        {
            try
            {
                // 使用CreateUIInCanvas方法在指定Canvas下创建项目
                var item = CreateUIInCanvas<TItem>(_itemPrefabPath, "", _canvasType);
                if (item != null)
                {
                    item.gameObject.name = $"Item_{index}";
                    item.transform.SetParent(_container, false);
                    item.transform.localPosition = new Vector3(0, -index * _itemHeight, 0);
                    item.gameObject.SetActive(false);
                }
                return item;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error creating item {index} in dynamic list {gameObject?.name}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 更新可见项目
        /// </summary>
        public void UpdateVisibleItems(int startIndex)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"Dynamic list {gameObject?.name} is not initialized");
                return;
            }

            try
            {
                // 隐藏当前可见项目
                foreach (var kvp in _activeItems)
                {
                    if (kvp.Value != null && !kvp.Value.IsDestroyed)
                    {
                        kvp.Value.gameObject.SetActive(false);
                    }
                }
                _activeItems.Clear();

                // 显示新的可见项目
                int endIndex = Mathf.Min(startIndex + _visibleItemCount, _items.Count);
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (i >= 0 && i < _items.Count && _items[i] != null && !_items[i].IsDestroyed)
                    {
                        _items[i].gameObject.SetActive(true);
                        _activeItems[i] = _items[i];
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error updating visible items in dynamic list {gameObject?.name}: {e.Message}");
            }
        }

        /// <summary>
        /// 滚动到指定位置
        /// </summary>
        public void ScrollTo(int index)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"Dynamic list {gameObject?.name} is not initialized");
                return;
            }

            try
            {
                int startIndex = Mathf.Max(0, index - _visibleItemCount / 2);
                UpdateVisibleItems(startIndex);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error scrolling to position {index} in dynamic list {gameObject?.name}: {e.Message}");
            }
        }

        /// <summary>
        /// 添加项目
        /// </summary>
        public void AddItem(TItem item)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"Dynamic list {gameObject?.name} is not initialized");
                return;
            }

            try
            {
                if (item != null && !item.IsDestroyed)
                {
                    int index = _items.Count;
                    _items.Add(item);
                    item.transform.SetParent(_container, false);
                    item.transform.localPosition = new Vector3(0, -index * _itemHeight, 0);
                    item.gameObject.SetActive(false);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error adding item to dynamic list {gameObject?.name}: {e.Message}");
            }
        }

        /// <summary>
        /// 移除项目
        /// </summary>
        public void RemoveItem(int index)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"Dynamic list {gameObject?.name} is not initialized");
                return;
            }

            try
            {
                if (index >= 0 && index < _items.Count)
                {
                    var item = _items[index];
                    if (item != null && !item.IsDestroyed)
                    {
                        item.Destroy();
                    }
                    _items.RemoveAt(index);
                    
                    // 更新剩余项目的位置
                    for (int i = index; i < _items.Count; i++)
                    {
                        if (_items[i] != null && !_items[i].IsDestroyed)
                        {
                            _items[i].transform.localPosition = new Vector3(0, -i * _itemHeight, 0);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error removing item at index {index} from dynamic list {gameObject?.name}: {e.Message}");
            }
        }

        // 生命周期方法
        protected override void OnShow() { }
        protected override void OnHide() { }
        protected override void OnLoad() { }
        protected override void OnUnload() { }
        protected override void Register() { }
        protected override void UnRegister() { }
    }
}
