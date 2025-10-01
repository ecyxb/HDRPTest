using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MyFrameWork
{
    public enum CanvasType
    {
        // 子canvas
        ChildCanvas = 4,
        // 根canvas-overlay
        RootOverlay = 0,
        // 根canvas-screenSpace
        RootScreenSpace = 1,
        // 根canvas-worldSpace
        RootWorldSpace = 2
    }

    [Serializable]
    public class RootCanvasConfig
    {
        public CanvasType canvasType;
        public RenderMode renderMode = RenderMode.ScreenSpaceOverlay;
        public string cameraPrefabPath;
        public Vector2 referenceResolution = new Vector2(1920, 1080);
        public CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        public float matchWidthOrHeight = 0.5f;
        public int sortingOrder = 0;
        public LayerMask sortigLayer = LayerMask.NameToLayer("UI");
    }

    [Serializable]
    public class ChildCanvasConfig
    {
        public const CanvasType canvasType = CanvasType.ChildCanvas;
        public RenderMode renderMode = RenderMode.ScreenSpaceOverlay;
        public bool overrideSorting = false;
        public int sortingOrder = 0;
        public LayerMask sortigLayer = LayerMask.NameToLayer("UI");
    }

    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;
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

        [SerializeField]
        private List<RootCanvasConfig> _rootCanvasConfigs = new List<RootCanvasConfig>
        {
            new RootCanvasConfig
            {
                canvasType = CanvasType.RootOverlay,
                renderMode = RenderMode.ScreenSpaceOverlay,
                
            },
            new RootCanvasConfig
            {
                canvasType = CanvasType.RootScreenSpace,
                renderMode = RenderMode.ScreenSpaceOverlay,
            },
            new RootCanvasConfig
            {
                canvasType = CanvasType.RootWorldSpace,
                renderMode = RenderMode.ScreenSpaceOverlay,
            },
        };

        private Dictionary<CanvasType, Canvas> _defaultCanvas = new Dictionary<CanvasType, Canvas>();
        private Dictionary<string, PanelBase> _panels = new Dictionary<string, PanelBase>();
        private Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();
        private Dictionary<Type, object> _pools = new Dictionary<Type, object>();
        private List<Action<float>> _lateUpdateActions = new List<Action<float>>();
        private List<Action<float>> _lateUpdateActionsToAdd = new List<Action<float>>();
        private List<Action<float>> _lateUpdateActionsToRemove = new List<Action<float>>();

        public Canvas this[CanvasType canvasType] => _defaultCanvas.GetValueOrDefault(canvasType, null);
        public PanelBase this[string panelName] => _panels.GetValueOrDefault(panelName, null);

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializeDefaultCanvases();
        }

        private void InitializeDefaultCanvases()
        {
            foreach (var config in _rootCanvasConfigs)
            {
                try
                {
                    var canvas = CreateRootCanvas(config);
                    if (canvas != null)
                    {
                        _defaultCanvas[config.canvasType] = canvas;
                        
                        DontDestroyOnLoad(canvas.gameObject);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to initialize canvas {config.canvasType}: {e.Message}");
                }
            }
        }

        public Canvas CreateRootCanvas(RootCanvasConfig config)
        {
            try
            {
                GameObject canvasObject = new GameObject($"Canvas_{config.canvasType}", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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
                        GameObject cameraObject = new GameObject($"UICamera_{config.canvasType}");
                        uiCamera = cameraObject.AddComponent<Camera>();
                        uiCamera.clearFlags = CameraClearFlags.Depth;
                        uiCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");
                    }
                    canvas.worldCamera = uiCamera;
                }
                canvas.sortingOrder = config.sortingOrder;
                canvas.sortingLayerID = config.sortigLayer;
                return canvas;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create canvas {config.canvasType}: {e.Message}");
                return null;
            }
        }

        public Canvas CreateChildCanvas(GameObject parent, ChildCanvasConfig childCanvasConfig)
        {
            try
            {
                GameObject canvasObject = new GameObject("ChildCanvas", typeof(Canvas), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(parent.transform, false);
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                if (childCanvasConfig.overrideSorting)
                {
                    canvas.overrideSorting = true;
                    canvas.sortingLayerID = childCanvasConfig.sortigLayer;
                    canvas.sortingOrder = childCanvasConfig.sortingOrder;
                }
                return canvas;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create child canvas: {e.Message}");
                return null;
            }
        }

        public T EnsurePanel<T>(string panelName, CanvasType canvasType = CanvasType.RootOverlay, bool createNewCanvas = false) where T : PanelBase, new()
        {
            try
            {
                // 检查是否已存在
                if (_panels.TryGetValue(panelName, out var existingPanel))
                {
                    return existingPanel as T;
                }

                // 获取或创建Canvas
                if (!_defaultCanvas.TryGetValue(canvasType, out var canvas))
                {
                    Debug.LogError($"Canvas {canvasType} not found");
                    return null;
                }

                // 创建面板
                var panel = PanelBase.InitPanel<T>(panelName, canvas, createNewCanvas);
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

        public Vector2 UIWorldPos2ScreenPos(Vector2 position, CanvasType canvasType = CanvasType.RootOverlay)
        {
            try
            {
                var canvas = _defaultCanvas.GetValueOrDefault(canvasType);
                if (canvas == null)
                {
                    Debug.LogWarning($"Canvas {canvasType} not found, using default conversion");
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

        public Vector3 ScreenPos2UIWorldPos(Vector2 screenPos, RectTransform rectTransform, CanvasType canvasType = CanvasType.RootOverlay)
        {
            try
            {
                var canvas = _defaultCanvas.GetValueOrDefault(canvasType);
                if (canvas == null)
                {
                    Debug.LogWarning($"Canvas {canvasType} not found, using default conversion");
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

        public void RegisterLateUpdate(Action<float> action)
        {
            if (action == null) return;

            lock (_lateUpdateActionsToAdd)
            {
                if (!_lateUpdateActionsToAdd.Contains(action))
                {
                    _lateUpdateActionsToAdd.Add(action);
                }
            }
        }

        public void UnregisterLateUpdate(Action<float> action)
        {
            if (action == null) return;

            lock (_lateUpdateActionsToRemove)
            {
                if (!_lateUpdateActionsToRemove.Contains(action))
                {
                    _lateUpdateActionsToRemove.Add(action);
                }
            }
        }

        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;

            try
            {
                // 添加新的动作
                lock (_lateUpdateActionsToAdd)
                {
                    foreach (var action in _lateUpdateActionsToAdd)
                    {
                        if (!_lateUpdateActions.Contains(action))
                        {
                            _lateUpdateActions.Add(action);
                        }
                    }
                    _lateUpdateActionsToAdd.Clear();
                }

                // 执行动作
                for (int i = _lateUpdateActions.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        _lateUpdateActions[i]?.Invoke(deltaTime);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error in late update action: {e.Message}");
                    }
                }

                // 移除动作
                lock (_lateUpdateActionsToRemove)
                {
                    foreach (var action in _lateUpdateActionsToRemove)
                    {
                        _lateUpdateActions.Remove(action);
                    }
                    _lateUpdateActionsToRemove.Clear();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in LateUpdate: {e.Message}");
            }
        }

        private GameObject LoadPrefab(string path)
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

        public ObjectPool<T> GetPool<T>() where T : UICommon, new()
        {
            var type = typeof(T);
            if (!_pools.TryGetValue(type, out var pool))
            {
                pool = new ObjectPool<T>(() => new T(), (obj) => true, 10);
                _pools[type] = pool;
            }
            return (ObjectPool<T>)pool;
        }

        public void ClearPrefabCache()
        {
            _prefabCache.Clear();
            Resources.UnloadUnusedAssets();
        }

        public void ClearAllPanels()
        {
            var panelNames = new List<string>(_panels.Keys);
            foreach (var panelName in panelNames)
            {
                DestroyPanel(panelName);
            }
        }

        private void OnDestroy()
        {
            try
            {
                // 清理所有面板
                ClearAllPanels();

                // 清理Canvas
                foreach (var canvas in _defaultCanvas.Values)
                {
                    if (canvas != null && canvas.gameObject != null)
                    {
                        Destroy(canvas.gameObject);
                    }
                }
                _defaultCanvas.Clear();

                // 清理缓存
                _prefabCache.Clear();
                _pools.Clear();
                _lateUpdateActions.Clear();
                _lateUpdateActionsToAdd.Clear();
                _lateUpdateActionsToRemove.Clear();

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
    }
}
