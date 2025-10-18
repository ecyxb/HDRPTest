using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace EventFramework
{
    /// <summary>
    /// UI通用基类，提供UI生命周期管理、层级关系管理、显示隐藏控制等功能
    /// </summary>
    public abstract class UICommon
    {
        /// <summary>
        /// 简化路径的全局缓存
        /// </summary>
        protected static Dictionary<string, string> __shortcuts__ = null;
        
        /// <summary>
        /// 当前UI的路径缓存
        /// </summary>
        protected virtual Dictionary<string, string> ShortCutsCache => null;
        
        /// <summary>
        /// 需要缓存路径的UI对象名称数组
        /// </summary>
        protected virtual string[] SHORTCUT_OBJECTS => null;
        
        /// <summary>
        /// UI的RectTransform组件
        /// </summary>
        private RectTransform _transform;
        
        /// <summary>
        /// 获取UI的GameObject
        /// </summary>
        public GameObject gameObject => _transform ?.gameObject;
        
        /// <summary>
        /// 父级UICommon
        /// </summary>
        protected UICommon _parent;
        
        /// <summary>
        /// 子级UICommon集合
        /// </summary>
        protected HashSet<UICommon> _children = null;
        
        /// <summary>
        /// 是否已卸载标记
        /// </summary>
        private bool _isUnload = false;
        
        /// <summary>
        /// 获取是否已卸载
        /// </summary>
        public bool IsUnload => _isUnload;
        
        /// <summary>
        /// 是否已加载标记
        /// </summary>
        private bool _isLoaded = false;
        
        /// <summary>
        /// 获取是否已加载
        /// </summary>
        public bool IsLoaded => _isLoaded;

        /// <summary>
        /// 脚本是否有效（已加载且未卸载）
        /// </summary>
        public bool IsScriptValid => _isLoaded && !_isUnload;
        
        /// <summary>
        /// GameObject是否有效（已加载、未卸载且GameObject不为null）
        /// </summary>
        public bool IsGOValid => _isLoaded && !_isUnload && gameObject != null;
        
        /// <summary>
        /// 是否已注册输入事件
        /// </summary>
        protected bool isRegistered = false;
        
        /// <summary>
        /// 获取UI是否激活状态
        /// </summary>
        public virtual bool IsActive => IsGOValid && gameObject.activeInHierarchy && gameObject.activeSelf;

        /// <summary>
        /// 获取或设置UI的RectTransform
        /// </summary>
        public RectTransform transform
        {
            get
            {
                if (!IsGOValid)
                {
                    Debug.LogWarning("UICommon transform is not valid");
                    return null;
                }
                return _transform;
            }
            protected set
            {
                _transform = value;
            }
        }
        
        /// <summary>
        /// 解绑子节点，不会销毁GameObject
        /// </summary>
        /// <param name="child">要移除的子UI</param>
        protected void RemoveChild(UICommon child)
        {
            if (_children != null && child != null)
            {
                if (_children.Remove(child))
                {
                    child._parent = null;
                }   
            }
        }
        
        /// <summary>
        /// 销毁自己和子孙节点，并从父节点移除，会销毁GameObject
        /// </summary>
        public virtual void SelfDestroy()
        {
            GameObject go = gameObject;
            _parent?.RemoveChild(this);
            Destroy();
            if(go != null)
            {
                GameObject.Destroy(go);
            }
        }
        
        /// <summary>
        /// 销毁自己和子孙节点，不会销毁GameObject，由祖先控制GameObject的销毁
        /// </summary>
        public virtual void Destroy()
        {
            if (!IsScriptValid)
            {
                return;
            }

            try
            {
                // 注销输入
                __unregister_input__();
                // 调用卸载事件
                OnUnload();
                // 标记为已销毁
                _isUnload = true;
                // 子UICommon回调
                if (_children != null)
                {
                    foreach (var child in _children)
                    {
                        child.Destroy();
                    }
                    _children.Clear();
                }
                // 清理引用
                _transform = null;
                _parent = null;
                _children = null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error destroying UICommon: {e.Message}");
            }
        }

        /// <summary>
        /// 内部加载方法，确保OnLoad只被调用一次
        /// </summary>
        protected void __on_load__()
        {
            if (_isLoaded)
            {
                return;
            }

            try
            {
                _isLoaded = true;
                OnLoad();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during UICommon load: {e.Message} {e.StackTrace}");
            }
        }

        /// <summary>
        /// 显示UI，激活GameObject并注册输入事件
        /// </summary>
        public virtual void Show()
        {
            if(!IsGOValid){
                Debug.LogWarning("Cannot show non-valid go UICommon");
                return;
            }
            try
            {
                __register_input__();
                if (!gameObject.activeSelf)
                {
                    gameObject.SetActive(true);
                    OnShow();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error showing UICommon: {e.Message}");
            }
        }

        /// <summary>
        /// 隐藏UI，注销输入事件并停用GameObject
        /// </summary>
        public virtual void Hide()
        {
            if (!IsGOValid)
            {
                Debug.LogWarning("Cannot hide non-valid go UICommon");
                return;
            }

            try
            {
                __unregister_input__();
                if (gameObject.activeSelf)
                {
                    OnHide();
                    gameObject.SetActive(false);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error hiding UICommon: {e.Message}");
            }
        }

        /// <summary>
        /// 内部注册输入事件方法
        /// </summary>
        private void __register_input__()
        {
            if (isRegistered || !IsScriptValid)
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
            }
        }

        /// <summary>
        /// 内部注销输入事件方法
        /// </summary>
        private void __unregister_input__()
        {
            if (!isRegistered || !IsScriptValid)
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

        /// <summary>
        /// 通过UI名称获取子UI的RectTransform，支持路径缓存
        /// </summary>
        /// <param name="uiName">UI名称或路径</param>
        /// <returns>找到的RectTransform，未找到返回null</returns>
        public virtual RectTransform this[string uiName]
        {
            get
            {
                if(!IsGOValid)
                {
                    Debug.LogWarning("UICommon indexer is not valid");
                    return null;
                }
                string fullname = ShortCutsCache?.GetValueOrDefault(uiName, null) ?? uiName;
                return transform.Find(fullname) as RectTransform;
            }
        }

        /// <summary>
        /// 被销毁前调用，父亲的OnUnload在子对象之前调用
        /// </summary>
        protected virtual void OnUnload() { }
        
        /// <summary>
        /// 被初始化时调用，父亲的OnLoad用来绑定子对象
        /// </summary>
        protected virtual void OnLoad() { }
        
        /// <summary>
        /// UI显示时回调
        /// </summary>
        protected virtual void OnShow() { }
        
        /// <summary>
        /// UI隐藏时回调
        /// </summary>
        protected virtual void OnHide() { }
        
        /// <summary>
        /// 注册事件和输入，在被隐藏或销毁后不回调
        /// </summary>
        protected virtual void Register() { }
        
        /// <summary>
        /// 注销事件和输入
        /// </summary>
        protected virtual void UnRegister() { }

        /// <summary>
        /// 在指定位置附加UI
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <param name="prefabPath">预制体路径</param>
        /// <param name="attachPosName">附加位置名称</param>
        /// <param name="aspectFit">纵横比适配模式</param>
        /// <returns>创建的UI实例</returns>
        protected T AttachUI<T>(string prefabPath, string attachPosName, AspectRatioFitter.AspectMode aspectFit = AspectRatioFitter.AspectMode.None) where T : UICommon, new()
        {
            if (!IsGOValid)
            {
                Debug.LogWarning("Cannot attach UI to invalid parent");
                return null;
            }

            var attachPos = this[attachPosName];
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

        /// <summary>
        /// 动态附加UI到指定父对象和位置
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <param name="parent">父UI对象</param>
        /// <param name="prefabPath">预制体路径</param>
        /// <param name="attachPos">附加位置</param>
        /// <param name="aspectFit">纵横比适配模式</param>
        /// <returns>创建的UI实例</returns>
        public static T DynamicAttach<T>(UICommon parent, string prefabPath, RectTransform attachPos, AspectRatioFitter.AspectMode aspectFit = AspectRatioFitter.AspectMode.None) where T : UICommon, new()
        {
            if (parent == null || !parent.IsGOValid)
            {
                Debug.LogWarning("Cannot attach UI to invalid parent");
                return null;
            }

            GameObject prefab = UIManager.Instance.FindUIPrefab(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"UI prefab {prefabPath} not found");
                return null;
            }

            return DynamicAttach<T>(parent, prefab, attachPos, aspectFit);
        }

        /// <summary>
        /// 动态附加UI到指定父对象和位置（使用GameObject）
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <param name="parent">父UI对象</param>
        /// <param name="prefabGameObject">预制体GameObject</param>
        /// <param name="attachPos">附加位置</param>
        /// <param name="aspectFit">纵横比适配模式</param>
        /// <returns>创建的UI实例</returns>
        public static T DynamicAttach<T>(UICommon parent, GameObject prefabGameObject, RectTransform attachPos, AspectRatioFitter.AspectMode aspectFit = AspectRatioFitter.AspectMode.None) where T : UICommon, new()
        {
            if (parent == null || parent.IsUnload)
            {
                Debug.LogWarning("Cannot attach UI to destroyed parent");
                return null;
            }
            RectTransform rectTransform = GameObject.Instantiate(prefabGameObject, attachPos)?.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Debug.LogError($"Failed to instantiate UI prefab {prefabGameObject.name} at attach position {attachPos.name}.");
                return null;
            }
            T uiCommon = new T();
            EnsureAspectRatioFitter(rectTransform, aspectFit);

            uiCommon._parent = parent;
            parent._children ??= new HashSet<UICommon>();
            parent._children.Add(uiCommon);

            uiCommon.transform = rectTransform;
            InitUICommonShortcuts(uiCommon, prefabGameObject);
            
            uiCommon.__on_load__();
            return uiCommon;
        }
        
        /// <summary>
        /// 初始化UICommon的快捷路径缓存
        /// </summary>
        /// <param name="uiCommon">要初始化的UI对象</param>
        /// <param name="uiPrefab">UI预制体</param>
        protected static void InitUICommonShortcuts(UICommon uiCommon, GameObject uiPrefab)
        {
            if (uiCommon == null || uiCommon.IsUnload || uiPrefab == null)
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
                            string relativePath = UIHelpers.GetGORelativePath(rect, uiPrefab.transform, sb);
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

        /// <summary>
        /// 确保UI对象具有正确的纵横比适配器配置
        /// </summary>
        /// <param name="obj">目标RectTransform</param>
        /// <param name="aspectFit">纵横比适配模式</param>
        public static void EnsureAspectRatioFitter(RectTransform obj, AspectRatioFitter.AspectMode aspectFit)
        {
            if (obj == null)
            {
                return;
            }

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

        /// <summary>
        /// 安全地获取组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件实例，未找到返回null</returns>
        public T GetComponent<T>() where T : Component
        {
            if (!IsGOValid)
            {
                return null;
            }

            return gameObject.GetComponent<T>();
        }

        /// <summary>
        /// 安全地添加组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>添加的组件实例</returns>
        public T AddComponent<T>() where T : Component
        {
            if (!IsGOValid)
            {
                return null;
            }
            return gameObject.AddComponent<T>();
        }

        /// <summary>
        /// 设置UI在父级中的层级索引
        /// </summary>
        /// <param name="index">层级索引</param>
        public void SetSiblingIndex(int index)
        {
            if (!IsGOValid)
            {
                return;
            }
            transform.SetSiblingIndex(index);
        }

        /// <summary>
        /// 获取UI在父级中的层级索引
        /// </summary>
        /// <returns>层级索引，无效时返回-1</returns>
        public int GetSiblingIndex()
        {
            if (!IsGOValid)
            {
                return -1;
            }
            return transform.GetSiblingIndex();
        }
    }

    /// <summary>
    /// 空的UICommon实现，用于占位或简单场景
    /// </summary>
    public class EmptyUICommon : UICommon { }
}
