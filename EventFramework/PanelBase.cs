using System;
using UnityEngine;

namespace EventFramework
{
    /// <summary>
    /// 面板基类，继承自UICommon，提供Canvas相关的面板管理功能
    /// </summary>
    public class PanelBase : UICommon
    {
        /// <summary>
        /// 面板所属的Canvas组件
        /// </summary>
        protected Canvas _canvas{get; private set;}
        
        /// <summary>
        /// 初始化面板实例
        /// </summary>
        /// <typeparam name="T">面板类型，必须继承自PanelBase</typeparam>
        /// <param name="panelName">面板预制体名称</param>
        /// <param name="canvas">目标Canvas</param>
        /// <returns>创建的面板实例，失败返回null</returns>
        public static T InitPanel<T>(string panelName, Canvas canvas) where T : PanelBase, new()
        {
            try
            {
                if (canvas == null)
                {
                    Debug.LogError($"Canvas is null for panel {panelName}");
                    return null;
                }

                GameObject prefab = UIManager.Instance.FindUIPrefab(panelName);
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
                panel._canvas = canvas;
                rectTransform.SetParent(canvas.transform, false);
                // 设置面板属性
                panel.transform = rectTransform;                
                // 初始化快捷方式
                InitUICommonShortcuts(panel, prefab);
                // 调用加载完成事件
                panel.__on_load__();
                return panel;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize panel {panelName}: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 销毁面板，如果Canvas下只有这一个面板，则同时销毁Canvas
        /// </summary>
        public override void SelfDestroy()
        {
            bool onlyOnePanel = _canvas != null && _canvas.GetComponentsInChildren<Transform>(true).Length <= 2; // 只有Canvas和Panel两个节点
            base.SelfDestroy();
            if (onlyOnePanel && _canvas != null)
            {
                GameObject.Destroy(_canvas.gameObject);
            }
            // 清理引用
            _canvas = null;
        }
        
        /// <summary>
        /// 设置面板的排序层级
        /// </summary>
        /// <param name="order">排序层级值</param>
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
        /// 获取面板在世界空间中的位置
        /// </summary>
        /// <returns>世界空间位置坐标</returns>
        public Vector3 GetWorldPosition()
        {
            if (transform == null || _canvas == null)
            {
                return Vector3.zero;
            }

            if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Screen Space Overlay模式下的转换
                return UIManager.Instance.UIWorldPos2ScreenPos(transform.position, _canvas);
            }
            else
            {
                // 其他模式直接返回位置
                return transform.position;
            }
        }

        /// <summary>
        /// 设置面板在世界空间中的位置
        /// </summary>
        /// <param name="worldPosition">目标世界空间位置</param>
        public void SetWorldPosition(Vector3 worldPosition)
        {
            if (transform == null || _canvas == null)
            {
                return;
            }

            if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Screen Space Overlay模式下的转换
                var screenPos = UIManager.Instance.ScreenPos2UIWorldPos(worldPosition, transform, _canvas);
                transform.position = screenPos;
            }
            else
            {
                // 其他模式直接设置位置
                transform.position = worldPosition;
            }
        }
    }
}
