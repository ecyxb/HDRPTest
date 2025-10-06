
// /// <summary>
// /// 动态列表类，用于管理大量动态UI元素
// /// </summary>
// public class DynamicList<TItem> : UICommon where TItem : UICommon, new()
// {
//     private string _itemPrefabPath;
//     private RectTransform _container;
//     private CanvasType _canvasType;
//     private List<TItem> _items = new List<TItem>();
//     private Dictionary<int, TItem> _activeItems = new Dictionary<int, TItem>();
//     private int _visibleItemCount = 0;
//     private float _itemHeight = 0f;
//     private new bool _isInitialized = false;

//     public string ItemPrefabPath => _itemPrefabPath;
//     public RectTransform Container => _container;
//     public CanvasType CanvasType => _canvasType;
//     public List<TItem> Items => _items;
//     public Dictionary<int, TItem> ActiveItems => _activeItems;
//     public int VisibleItemCount => _visibleItemCount;
//     public int TotalItemCount => _items.Count;

//     public DynamicList(string itemPrefabPath, RectTransform container, CanvasType canvasType = CanvasType.RootOverlay)
//     {
//         _itemPrefabPath = itemPrefabPath;
//         _container = container;
//         _canvasType = canvasType;
//     }

//     public override void Show()
//     {
//         if (!_isInitialized)
//         {
//             Debug.LogWarning($"Dynamic list {gameObject?.name} is not initialized");
//             return;
//         }

//         try
//         {
//             base.Show();
//             OnShow();
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Error showing dynamic list {gameObject?.name}: {e.Message}");
//         }
//     }

//     public override void Hide()
//     {
//         if (!_isInitialized)
//         {
//             Debug.LogWarning($"Dynamic list {gameObject?.name} is not initialized");
//             return;
//         }

//         try
//         {
//             base.Hide();
//             OnHide();
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Error hiding dynamic list {gameObject?.name}: {e.Message}");
//         }
//     }

//     public override void Destroy()
//     {
//         if (!_isInitialized)
//         {
//             return;
//         }

//         try
//         {
//             // 清理所有项目
//             foreach (var item in _items)
//             {
//                 if (item != null && !item.IsUnload)
//                 {
//                     item.Destroy();
//                 }
//             }
//             _items.Clear();
//             _activeItems.Clear();

//             base.Destroy();
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Error destroying dynamic list {gameObject?.name}: {e.Message}");
//         }
//     }

//     /// <summary>
//     /// 初始化动态列表
//     /// </summary>
//     public void Initialize(int itemCount, float itemHeight = 100f, int visibleItemCount = 10)
//     {
//         try
//         {
//             if (_isInitialized)
//             {
//                 Debug.LogWarning($"Dynamic list {gameObject?.name} is already initialized");
//                 return;
//             }

//             _itemHeight = itemHeight;
//             _visibleItemCount = visibleItemCount;
//             _isInitialized = true;

//             // 创建项目池
//             for (int i = 0; i < itemCount; i++)
//             {
//                 var item = CreateItem(i);
//                 if (item != null)
//                 {
//                     _items.Add(item);
//                 }
//             }

//             // 初始显示可见项目
//             UpdateVisibleItems(0);
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Error initializing dynamic list {gameObject?.name}: {e.Message}");
//         }
//     }

//     /// <summary>
//     /// 创建列表项
//     /// </summary>
//     private TItem CreateItem(int index)
//     {
//         try
//         {
//             // 使用CreateUIInCanvas方法在指定Canvas下创建项目
//             var item = CreateUIInCanvas<TItem>(_itemPrefabPath, "", _canvasType);
//             if (item != null)
//             {
//                 item.gameObject.name = $"Item_{index}";
//                 item.transform.SetParent(_container, false);
//                 item.transform.localPosition = new Vector3(0, -index * _itemHeight, 0);
//                 item.gameObject.SetActive(false);
//             }
//             return item;
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Error creating item {index} in dynamic list {gameObject?.name}: {e.Message}");
//             return null;
//         }
//     }

//     /// <summary>
//     /// 更新可见项目
//     /// </summary>
//     public void UpdateVisibleItems(int startIndex)
//     {
//         if (!_isInitialized)
//         {
//             Debug.LogWarning($"Dynamic list {gameObject?.name} is not initialized");
//             return;
//         }

//         try
//         {
//             // 隐藏当前可见项目
//             foreach (var kvp in _activeItems)
//             {
//                 if (kvp.Value != null && !kvp.Value.IsUnload)
//                 {
//                     kvp.Value.gameObject.SetActive(false);
//                 }
//             }
//             _activeItems.Clear();

//             // 显示新的可见项目
//             int endIndex = Mathf.Min(startIndex + _visibleItemCount, _items.Count);
//             for (int i = startIndex; i < endIndex; i++)
//             {
//                 if (i >= 0 && i < _items.Count && _items[i] != null && !_items[i].IsUnload)
//                 {
//                     _items[i].gameObject.SetActive(true);
//                     _activeItems[i] = _items[i];
//                 }
//             }
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Error updating visible items in dynamic list {gameObject?.name}: {e.Message}");
//         }
//     }

//     /// <summary>
//     /// 滚动到指定位置
//     /// </summary>
//     public void ScrollTo(int index)
//     {
//         if (!_isInitialized)
//         {
//             Debug.LogWarning($"Dynamic list {gameObject?.name} is not initialized");
//             return;
//         }

//         try
//         {
//             int startIndex = Mathf.Max(0, index - _visibleItemCount / 2);
//             UpdateVisibleItems(startIndex);
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Error scrolling to position {index} in dynamic list {gameObject?.name}: {e.Message}");
//         }
//     }

//     /// <summary>
//     /// 添加项目
//     /// </summary>
//     public void AddItem(TItem item)
//     {
//         if (!_isInitialized)
//         {
//             Debug.LogWarning($"Dynamic list {gameObject?.name} is not initialized");
//             return;
//         }

//         try
//         {
//             if (item != null && !item.IsUnload)
//             {
//                 int index = _items.Count;
//                 _items.Add(item);
//                 item.transform.SetParent(_container, false);
//                 item.transform.localPosition = new Vector3(0, -index * _itemHeight, 0);
//                 item.gameObject.SetActive(false);
//             }
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Error adding item to dynamic list {gameObject?.name}: {e.Message}");
//         }
//     }

//     /// <summary>
//     /// 移除项目
//     /// </summary>
//     public void RemoveItem(int index)
//     {
//         if (!_isInitialized)
//         {
//             Debug.LogWarning($"Dynamic list {gameObject?.name} is not initialized");
//             return;
//         }

//         try
//         {
//             if (index >= 0 && index < _items.Count)
//             {
//                 var item = _items[index];
//                 if (item != null && !item.IsUnload)
//                 {
//                     item.Destroy();
//                 }
//                 _items.RemoveAt(index);

//                 // 更新剩余项目的位置
//                 for (int i = index; i < _items.Count; i++)
//                 {
//                     if (_items[i] != null && !_items[i].IsUnload)
//                     {
//                         _items[i].transform.localPosition = new Vector3(0, -i * _itemHeight, 0);
//                     }
//                 }
//             }
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Error removing item at index {index} from dynamic list {gameObject?.name}: {e.Message}");
//         }
//     }
// }