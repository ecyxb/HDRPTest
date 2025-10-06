using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EventFramework
{
    /// <summary>
    /// 根Canvas配置类，用于定义Canvas的渲染模式和各种属性
    /// </summary>
    public class RootCanvasConfig
    {
        /// <summary>
        /// Canvas渲染模式
        /// </summary>
        public RenderMode renderMode = RenderMode.ScreenSpaceOverlay;
        
        /// <summary>
        /// 相机预制体路径
        /// </summary>
        public string cameraPrefabPath;
        
        /// <summary>
        /// 参考分辨率
        /// </summary>
        public Vector2 referenceResolution = new Vector2(1920, 1080);
        
        /// <summary>
        /// 屏幕匹配模式
        /// </summary>
        public CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        
        /// <summary>
        /// 宽高匹配权重
        /// </summary>
        public float matchWidthOrHeight = 0.5f;
        
        /// <summary>
        /// 排序层级
        /// </summary>
        public int sortingOrder = 0;
        
        /// <summary>
        /// 排序图层
        /// </summary>
        public LayerMask sortingLayer = LayerMask.NameToLayer("UI");
    }

    /// <summary>
    /// UI管理器，负责管理所有UI面板的创建、销毁、显示隐藏等操作
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        /// <summary>
        /// 默认的Overlay模式Canvas配置
        /// </summary>
        static RootCanvasConfig RootOverlayDefaultConfig = new RootCanvasConfig
        {
            renderMode = RenderMode.ScreenSpaceOverlay,
            referenceResolution = new Vector2(1920, 1080),
        };
        
        /// <summary>
        /// 默认的ScreenSpace模式Canvas配置
        /// </summary>
        static RootCanvasConfig RootScreenSpaceDefaultConfig = new RootCanvasConfig
        {
            renderMode = RenderMode.ScreenSpaceOverlay,
            referenceResolution = new Vector2(1920, 1080),
        };
        
        /// <summary>
        /// 默认的WorldSpace模式Canvas配置
        /// </summary>
        static RootCanvasConfig RootWorldSpaceDefaultConfig = new RootCanvasConfig
        {
            renderMode = RenderMode.ScreenSpaceOverlay,
            referenceResolution = new Vector2(1920, 1080),
        };

        /// <summary>
        /// UIManager单例实例
        /// </summary>
        private static UIManager _instance;
        
        /// <summary>
        /// 获取UIManager单例实例
        /// </summary>
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UIManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("UIManager");
                        _instance = go.AddComponent<UIManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 面板字典，存储所有已创建的面板
        /// </summary>
        private Dictionary<string, PanelBase> _panels = new Dictionary<string, PanelBase>();
        
        /// <summary>
        /// 预制体缓存字典
        /// </summary>
        private Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();

        /// <summary>
        /// 通过面板名称获取面板实例
        /// </summary>
        /// <param name="panelName">面板名称</param>
        /// <returns>面板实例，未找到返回null</returns>
        public PanelBase this[string panelName] => _panels.GetValueOrDefault(panelName, null);
        
        /// <summary>
        /// LateUpdate回调函数列表
        /// </summary>
        private List<Action<float>> _lateUpdateActions = new List<Action<float>>();
        
        /// <summary>
        /// 当前LateUpdate执行索引
        /// </summary>
        private int _lateUpdateIdx = -1;
        
        /// <summary>
        /// 是否正在执行LateUpdate
        /// </summary>
        private bool _isLateUpdating = false;

        /// <summary>
        /// Unity Awake生命周期方法
        /// </summary>
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        /// <summary>
        /// 创建根Canvas
        /// </summary>
        /// <param name="config">Canvas配置</param>
        /// <returns>创建的Canvas实例</returns>
        public Canvas CreateRootCanvas(RootCanvasConfig config)
        {
            try
            {
                GameObject canvasObject = new GameObject($"Canvas_{config.renderMode}", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                // 设置Canvas属性
                canvas.renderMode = config.renderMode;

                // 设置CanvasScaler
                CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = config.referenceResolution;
                canvasScaler.screenMatchMode = config.screenMatchMode;
                canvasScaler.matchWidthOrHeight = config.matchWidthOrHeight;

                // 处理相机设置
                if (config.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    Camera uiCamera = null;

                    if (!string.IsNullOrEmpty(config.cameraPrefabPath))
                    {
                        GameObject cameraPrefab = LoadPrefab(config.cameraPrefabPath);
                        if (cameraPrefab != null)
                        {
                            GameObject cameraInstance = Instantiate(cameraPrefab);
                            uiCamera = cameraInstance.GetComponent<Camera>();

                        }
                    }

                    if (uiCamera == null)
                    {
                        // 如果没有找到预制体，创建默认相机
                        GameObject cameraObject = new GameObject($"UICamera_{config.renderMode}");
                        uiCamera = cameraObject.AddComponent<Camera>();
                        uiCamera.clearFlags = CameraClearFlags.Depth;
                        uiCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");
                    }
                    canvas.worldCamera = uiCamera;
                }
                canvas.sortingOrder = config.sortingOrder;
                canvas.sortingLayerID = config.sortingLayer;
                return canvas;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create canvas {config.renderMode}: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 确保面板存在，如果不存在则创建
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
        /// <param name="panelName">面板名称</param>
        /// <param name="canvasConfig">Canvas配置，为null时使用默认配置</param>
        /// <returns>面板实例</returns>
        public T EnsurePanel<T>(string panelName, RootCanvasConfig canvasConfig = null) where T : PanelBase, new()
        {
            try
            {
                // 检查是否已存在
                if (_panels.TryGetValue(panelName, out var existingPanel))
                {
                    return existingPanel as T;
                }
                Canvas canvas = CreateRootCanvas(canvasConfig ?? RootOverlayDefaultConfig);
                // 创建面板
                var panel = PanelBase.InitPanel<T>(panelName, canvas);
                if (panel != null)
                {
                    _panels[panelName] = panel;
                    panel.Show();
                }
                return panel;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to ensure panel {panelName}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 销毁指定名称的面板
        /// </summary>
        /// <param name="panelName">面板名称</param>
        public void DestroyPanel(string panelName)
        {
            try
            {
                if (_panels.TryGetValue(panelName, out var panel))
                {
                    panel.Destroy();
                    _panels.Remove(panelName);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to destroy panel {panelName}: {e.Message}");
            }
        }

        /// <summary>
        /// 将UI世界坐标转换为屏幕坐标
        /// </summary>
        /// <param name="position">UI世界坐标</param>
        /// <param name="canvas">目标Canvas，为null时使用默认转换</param>
        /// <returns>屏幕坐标</returns>
        public Vector2 UIWorldPos2ScreenPos(Vector2 position, Canvas canvas=null) 
        {
            try
            {
                if (canvas == null)
                {
                    return RectTransformUtility.WorldToScreenPoint(null, position);
                }

                Camera camera = null;
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
                {
                    camera = canvas.worldCamera;
                }
                return RectTransformUtility.WorldToScreenPoint(camera, position);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to convert world position to screen position: {e.Message}");
                return Vector2.zero;
            }
        }

        /// <summary>
        /// 将屏幕坐标转换为UI世界坐标
        /// </summary>
        /// <param name="screenPos">屏幕坐标</param>
        /// <param name="rectTransform">目标RectTransform</param>
        /// <param name="canvas">目标Canvas，为null时使用默认转换</param>
        /// <returns>UI世界坐标</returns>
        public Vector3 ScreenPos2UIWorldPos(Vector2 screenPos, RectTransform rectTransform, Canvas canvas=null)
        {
            try
            {
                if (canvas == null)
                {
                    return RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, screenPos, null, out Vector3 defaultWorldPos) ? defaultWorldPos : Vector3.zero;
                }

                Camera camera = null;
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
                {
                    camera = canvas.worldCamera;
                }
                return RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, screenPos, camera, out Vector3 worldPos) ? worldPos : Vector3.zero;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to convert screen position to world position: {e.Message}");
                return Vector3.zero;
            }
        }

        /// <summary>
        /// 清空预制体缓存并释放未使用的资源
        /// </summary>
        public void ClearPrefabCache()
        {
            _prefabCache.Clear();
            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// 清理所有面板
        /// </summary>
        public void ClearAllPanels()
        {
            var panelNames = new List<string>(_panels.Keys);
            foreach (var panelName in panelNames)
            {
                DestroyPanel(panelName);
            }
        }

        /// <summary>
        /// Unity OnDestroy生命周期方法
        /// </summary>
        private void OnDestroy()
        {
            try
            {
                // 清理所有面板
                ClearAllPanels();
                // 清理缓存
                _prefabCache.Clear();
                _lateUpdateActions.Clear();
                // 清理单例引用
                if (_instance == this)
                {
                    _instance = null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during UIManager destruction: {e.Message}");
            }
        }
        
        /// <summary>
        /// 注册LateUpdate回调函数
        /// </summary>
        /// <param name="action">要注册的回调函数</param>
        public void RegisterLateUpdate(Action<float> action)
        {
            if (action == null) return;
            if (!_lateUpdateActions.Contains(action))
            {
                _lateUpdateActions.Add(action);
            }
        }

        /// <summary>
        /// 注销LateUpdate回调函数
        /// </summary>
        /// <param name="action">要注销的回调函数</param>
        public void UnregisterLateUpdate(Action<float> action)
        {
            if (action == null) return;
            if(_isLateUpdating){
                int idx = _lateUpdateActions.IndexOf(action);
                if(idx != -1 && idx <= _lateUpdateIdx){
                    _lateUpdateIdx--;
                }
                _lateUpdateActions.RemoveAt(idx);
            }else{
                _lateUpdateActions.Remove(action);
            }
            
        }
        
        /// <summary>
        /// Unity LateUpdate生命周期方法
        /// </summary>
        private void LateUpdate()
        {
            _lateUpdateIdx = 0;
            _isLateUpdating = true;
            // 执行动作
            while(_lateUpdateIdx < _lateUpdateActions.Count)
            {
                try
                {
                    _lateUpdateActions[_lateUpdateIdx]?.Invoke(Time.deltaTime);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error in late update action: {e.Message}");
                }
                _lateUpdateIdx++;
            }
            _lateUpdateIdx = -1;
            _isLateUpdating = false;
        }

        /// <summary>
        /// 查找UI预制体，从UIPrefabs文件夹中加载
        /// </summary>
        /// <param name="uiName">UI名称</param>
        /// <returns>UI预制体GameObject</returns>
        public virtual GameObject FindUIPrefab(string uiName)
        {
            return LoadPrefab($"UIPrefabs/{uiName}");
        }

        /// <summary>
        /// 从Resources加载预制体，支持缓存
        /// </summary>
        /// <param name="path">预制体路径</param>
        /// <returns>预制体GameObject</returns>
        protected virtual GameObject LoadPrefab(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            if (_prefabCache.TryGetValue(path, out var prefab))
            {
                return prefab;
            }

            try
            {
                prefab = Resources.Load<GameObject>(path);
                if (prefab != null)
                {
                    _prefabCache[path] = prefab;
                }
                else
                {
                    Debug.LogWarning($"Prefab not found at path: {path}");
                }
                return prefab;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load prefab {path}: {e.Message}");
                return null;
            }
        }
    }
}
