using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MyFrameWork
{
    public abstract class UICommon
    {
        public virtual CanvasType CanvasType { get; } = CanvasType.ChildCanvas;


        private RectTransform _transform;
        private GameObject _gameObject;
        private bool _isDestroyed = false;

        public RectTransform transform 
        { 
            get 
            {
                if (_isDestroyed)
                {
                    Debug.LogWarning("Attempting to access transform of destroyed UICommon");
                    return null;
                }
                return _transform; 
            }
            protected set 
            {
                _transform = value;
                _gameObject = value?.gameObject;
            } 
        }

        public GameObject gameObject 
        { 
            get 
            {
                if (_isDestroyed)
                {
                    Debug.LogWarning("Attempting to access gameObject of destroyed UICommon");
                    return null;
                }
                return _gameObject; 
            } 
        }

        protected UICommon parent;
        protected HashSet<UICommon> _children = null;
        protected bool _isInitialized = false;

        protected static Dictionary<string, string> __shortcuts__ = null;
        protected virtual Dictionary<string, string> ShortCutsCache => null;
        protected virtual string[] SHORTCUT_OBJECTS => null;

        private bool isRegistered = false;

        public bool IsDestroyed => _isDestroyed;
        public bool IsInitialized => _isInitialized;
        public virtual bool IsActive 
        { 
            get 
            {
                if (_isDestroyed || gameObject == null)
                {
                    return false;
                }
                return gameObject.activeInHierarchy && gameObject.activeSelf;
            }
        }

        // 子Canvas相关属性
        protected Canvas _canvas;
        protected Dictionary<CanvasType, Canvas> _childCanvases = new Dictionary<CanvasType, Canvas>();
        protected bool _isCanvasInitialized = false;
        


        public Canvas Canvas => _canvas;
        public Dictionary<CanvasType, Canvas> ChildCanvases => _childCanvases;

        protected UICommon()
        {
        }

        public virtual void Destroy()
        {
            if (_isDestroyed)
            {
                return;
            }

            try
            {
                // 标记为已销毁
                _isDestroyed = true;

                // 注销输入
                __unregister_input__();

                // 调用卸载事件
                OnUnload();

                // 销毁所有子对象
                if (_children != null)
                {
                    var childrenCopy = _children.ToList();
                    foreach (var child in childrenCopy)
                    {
                        if (child != null && !child.IsDestroyed)
                        {
                            child.Destroy();
                        }
                    }
                    _children.Clear();
                }

                // 销毁所有子Canvas
                foreach (var childCanvas in _childCanvases.Values)
                {
                    if (childCanvas != null && childCanvas.gameObject != null)
                    {
                        GameObject.Destroy(childCanvas.gameObject);
                    }
                }
                _childCanvases.Clear();

                // 从父对象中移除
                if (parent != null && parent._children != null)
                {
                    parent._children.Remove(this);
                }

                // 清理引用
                _transform = null;
                _gameObject = null;
                parent = null;
                _children = null;
                _canvas = null;
                _isCanvasInitialized = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error destroying UICommon: {e.Message}");
            }
        }

        protected void __on_load__()
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                OnLoad();
                _isInitialized = true;
                
                // 初始化Canvas
                InitializeCanvas();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during UICommon load: {e.Message}");
            }
        }

        private void InitializeCanvas()
        {
            if (_isCanvasInitialized || _canvas == null)
            {
                return;
            }

            try
            {
                // 获取或创建子Canvas
                var childCanvas = CreateChildCanvas();
                if (childCanvas != null)
                {
                    _childCanvases[CanvasType.ChildCanvas] = childCanvas;
                }
                
                _isCanvasInitialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error initializing canvas for UICommon: {e.Message}");
            }
        }

        private Canvas CreateChildCanvas()
        {
            if (_canvas == null)
            {
                Debug.LogWarning("Parent canvas is null, cannot create child canvas");
                return null;
            }

            try
            {
                GameObject canvasObject = new GameObject("ChildCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                
                // 设置Canvas属性
                canvas.renderMode = _canvas.renderMode;
                
                // 设置父子关系
                canvas.transform.SetParent(_canvas.transform, false);
                
                // 设置CanvasScaler
                CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(1920, 1080);
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                canvasScaler.matchWidthOrHeight = 0.5f;
                
                // 处理相机设置
                if (_canvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    canvas.worldCamera = _canvas.worldCamera;
                }
                
                return canvas;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create child canvas: {e.Message}");
                return null;
            }
        }

        public virtual void Show()
        {
            if (_isDestroyed || gameObject == null)
            {
                Debug.LogWarning("Cannot show destroyed UICommon");
                return;
            }

            try
            {
                if (!gameObject.activeSelf)
                {
                    gameObject.SetActive(true);
                    __register_input__();
                    OnShow();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error showing UICommon: {e.Message}");
            }
        }

        public virtual void Hide()
        {
            if (_isDestroyed || gameObject == null)
            {
                Debug.LogWarning("Cannot hide destroyed UICommon");
                return;
            }

            try
            {
                if (gameObject.activeSelf)
                {
                    __unregister_input__();
                    OnHide();
                    gameObject.SetActive(false);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error hiding UICommon: {e.Message}");
            }
        }

        private void __register_input__()
        {
            if (isRegistered || _isDestroyed)
            {
                return;
            }

            try
            {
                isRegistered = true;
                Register();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error registering input for UICommon: {e.Message}");
                isRegistered = false;
            }
        }

        private void __unregister_input__()
        {
            if (!isRegistered || _isDestroyed)
            {
                return;
            }

            try
            {
                UnRegister();
                isRegistered = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error unregistering input for UICommon: {e.Message}");
            }
        }

        public virtual RectTransform this[string uiName]
        {
            get
            {
                if (_isDestroyed || transform == null)
                {
                    return null;
                }

                try
                {
                    string fullname = ShortCutsCache?.GetValueOrDefault(uiName, null) ?? uiName;
                    return transform.Find(fullname) as RectTransform;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error finding UI element {uiName}: {e.Message}");
                    return null;
                }
            }
        }

        // 生命周期方法 - 子类可以重写
        protected virtual void OnUnload() { }
        protected virtual void OnLoad() { }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void Register() { }
        protected virtual void UnRegister() { }

        protected T AttachUI<T>(string prefabPath, string attachPosName, AspectRatioFitter.AspectMode aspectFit = AspectRatioFitter.AspectMode.None) where T : UICommon, new()
        {
            if (_isDestroyed)
            {
                Debug.LogWarning("Cannot attach UI to destroyed UICommon");
                return null;
            }

            try
            {
                var attachPos = this[attachPosName] ?? transform.Find(attachPosName) as RectTransform;
                if (attachPos == null)
                {
                    Debug.LogError($"Attach position {attachPosName} not found in parent {gameObject?.name}.");
                    return null;
                }

                var uiInstance = DynamicAttach<T>(this, prefabPath, attachPos, aspectFit: aspectFit);
                if (uiInstance != null)
                {
                    uiInstance.gameObject.name = prefabPath;
                }
                return uiInstance;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error attaching UI {prefabPath}: {e.Message}");
                return null;
            }
        }

        public static T DynamicAttach<T>(UICommon parent, string prefabPath, RectTransform attachPos, AspectRatioFitter.AspectMode aspectFit = AspectRatioFitter.AspectMode.None) where T : UICommon, new()
        {
            if (parent == null || parent.IsDestroyed)
            {
                Debug.LogWarning("Cannot attach UI to destroyed parent");
                return null;
            }

            try
            {
                GameObject prefab = AOTHelpers.FindUIPrefab(prefabPath);
                if (prefab == null)
                {
                    Debug.LogError($"UI prefab {prefabPath} not found in Resources/UIPrefabs.");
                    return null;
                }

                return DynamicAttach<T>(parent, prefab, attachPos, aspectFit);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error dynamically attaching UI {prefabPath}: {e.Message}");
                return null;
            }
        }

        public static T DynamicAttach<T>(UICommon parent, GameObject prefabGameObject, RectTransform attachPos, AspectRatioFitter.AspectMode aspectFit = AspectRatioFitter.AspectMode.None) where T : UICommon, new()
        {
            if (parent == null || parent.IsDestroyed)
            {
                Debug.LogWarning("Cannot attach UI to destroyed parent");
                return null;
            }

            try
            {
                // 使用对象池
                var pool = UIManager.Instance.GetPool<T>();
                T uiCommon = pool.Get();

                RectTransform rectTransform = GameObject.Instantiate(prefabGameObject, attachPos)?.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    Debug.LogError($"Failed to instantiate UI prefab {prefabGameObject.name} at attach position {attachPos.name}.");
                    pool.Return(uiCommon);
                    return null;
                }

                EnsureAspectRatioFitter(rectTransform, aspectFit);
                
                uiCommon.parent = parent;
                parent._children ??= new HashSet<UICommon>();
                parent._children.Add(uiCommon);
                
                uiCommon.transform = rectTransform;
                InitUICommonShortcuts(uiCommon, prefabGameObject);
                uiCommon.__on_load__();

                return uiCommon;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error dynamically attaching UI {prefabGameObject.name}: {e.Message}");
                return null;
            }
        }

        protected static void InitUICommonShortcuts(UICommon uiCommon, GameObject uiPrefab)
        {
            if (uiCommon == null || uiCommon.IsDestroyed || uiPrefab == null)
            {
                return;
            }

            try
            {
                var shortcuts = uiCommon.SHORTCUT_OBJECTS?.Where(s => !string.IsNullOrEmpty(s)).ToArray();
                var cache = uiCommon.ShortCutsCache;
                
                if (cache != null && cache.Count == 0 && shortcuts != null && shortcuts.Length > 0)
                {
                    Stack<RectTransform> stack = new Stack<RectTransform>();
                    StringBuilder sb = new StringBuilder();
                    
                    var prefabRect = uiPrefab.GetComponent<RectTransform>();
                    if (prefabRect != null)
                    {
                        stack.Push(prefabRect);
                    }

                    while (stack.Count > 0 && cache.Count < shortcuts.Length)
                    {
                        RectTransform rect = stack.Pop();
                        if (rect == null)
                        {
                            continue;
                        }

                        if (!string.IsNullOrEmpty(rect.name) && shortcuts.Contains(rect.name))
                        {
                            string relativePath = AOTHelpers.GetGORelativePath(rect, uiCommon.transform, sb);
                            cache[rect.name] = relativePath;
                        }

                        foreach (RectTransform child in rect)
                        {
                            stack.Push(child);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error initializing UICommon shortcuts: {e.Message}");
            }
        }

        public static void EnsureAspectRatioFitter(RectTransform obj, AspectRatioFitter.AspectMode aspectFit)
        {
            if (obj == null)
            {
                return;
            }

            try
            {
                AspectRatioFitter fitter = obj.GetComponent<AspectRatioFitter>();
                
                if (aspectFit == AspectRatioFitter.AspectMode.None)
                {
                    if (fitter != null)
                    {
                        fitter.aspectMode = AspectRatioFitter.AspectMode.None;
                    }
                }
                else
                {
                    var ratio = obj.rect.width / obj.rect.height;
                    fitter = fitter ?? obj.gameObject.AddComponent<AspectRatioFitter>();
                    fitter.aspectMode = aspectFit;
                    
                    if (aspectFit == AspectRatioFitter.AspectMode.FitInParent)
                    {
                        fitter.aspectRatio = ratio;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error ensuring aspect ratio fitter: {e.Message}");
            }
        }

        /// <summary>
        /// 安全地获取组件
        /// </summary>
        public T GetComponent<T>() where T : Component
        {
            if (_isDestroyed || gameObject == null)
            {
                return null;
            }

            try
            {
                return gameObject.GetComponent<T>();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting component {typeof(T).Name}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 安全地添加组件
        /// </summary>
        public T AddComponent<T>() where T : Component
        {
            if (_isDestroyed || gameObject == null)
            {
                return null;
            }

            try
            {
                return gameObject.AddComponent<T>();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error adding component {typeof(T).Name}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 设置层级
        /// </summary>
        public void SetSiblingIndex(int index)
        {
            if (_isDestroyed || transform == null)
            {
                return;
            }

            try
            {
                transform.SetSiblingIndex(index);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error setting sibling index: {e.Message}");
            }
        }

        /// <summary>
        /// 获取层级
        /// </summary>
        public int GetSiblingIndex()
        {
            if (_isDestroyed || transform == null)
            {
                return -1;
            }

            try
            {
                return transform.GetSiblingIndex();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting sibling index: {e.Message}");
                return -1;
            }
        }

        /// <summary>
        /// 在指定Canvas类型下创建UI元素
        /// </summary>
        public T CreateUIInCanvas<T>(string prefabPath, string attachPosName, CanvasType canvasType = CanvasType.RootOverlay) where T : UICommon, new()
        {
            if (_isDestroyed || _childCanvases == null)
            {
                Debug.LogWarning("Cannot create UI in destroyed UICommon or canvas not initialized");
                return null;
            }

            try
            {
                // 获取指定类型的子Canvas
                if (!_childCanvases.TryGetValue(canvasType, out var childCanvas))
                {
                    Debug.LogWarning($"Child canvas {canvasType} not found");
                    return null;
                }

                // 查找或创建附着位置
                RectTransform attachPos = null;
                if (!string.IsNullOrEmpty(attachPosName))
                {
                    attachPos = this[attachPosName];
                    if (attachPos == null)
                    {
                        Debug.LogError($"Attach position {attachPosName} not found");
                        return null;
                    }
                }
                else
                {
                    // 如果没有指定附着位置，直接附加到子Canvas
                    attachPos = childCanvas.transform as RectTransform;
                }

                // 创建UI元素
                var uiInstance = DynamicAttach<T>(this, prefabPath, attachPos);
                if (uiInstance != null)
                {
                    uiInstance.gameObject.name = prefabPath;
                    
                    // 设置UI元素的Canvas为子Canvas
                    var panelBase = uiInstance as PanelBase;
                    if (panelBase != null)
                    {
                        panelBase.SetCanvas(childCanvas);
                    }
                }
                
                return uiInstance;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error creating UI in canvas {canvasType}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取指定类型的子Canvas
        /// </summary>
        public Canvas GetChildCanvas(CanvasType canvasType)
        {
            if (_childCanvases == null)
            {
                Debug.LogWarning("Child canvases not initialized");
                return null;
            }

            return _childCanvases.GetValueOrDefault(canvasType, null);
        }

        /// <summary>
        /// 检查是否已初始化子Canvas
        /// </summary>
        public bool IsCanvasInitialized => _isCanvasInitialized;
    }

    public class EmptyUICommon : UICommon { }
}
