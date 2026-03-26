# 背包系统UI性能优化方案

## 1. 性能目标
- **背包打开延迟** < 100ms (95%分位数)
- **内存占用** < 50MB (包含所有图标缓存)
- **GC分配** < 1KB/帧 (稳定状态下)
- **CPU占用** < 2ms/帧 (60FPS下)

## 2. 优化策略概览

### 2.1 内存优化
| 组件 | 优化策略 | 预期节省 | 风险 |
|------|----------|----------|------|
| 图标资源 | 异步加载 + LRU缓存 | 30-50%内存 | 首次加载延迟 |
| 预制体 | 对象池 + 按需加载 | 70-80%内存 | 池管理复杂度 |
| 事件系统 | 结构体事件 + 对象池 | 零GC分配 | 生命周期管理 |

### 2.2 CPU优化
| 操作 | 优化策略 | 预期提升 | 实现复杂度 |
|------|----------|----------|------------|
| 界面打开 | 分帧初始化 | 50ms → 16ms | 中 |
| 滚动渲染 | 虚拟化 + 视口裁剪 | 90%渲染节省 | 高 |
| 事件处理 | 批处理 + 节流 | 60% CPU节省 | 低 |

### 2.3 GPU优化
| 瓶颈 | 优化策略 | 适用场景 |
|------|----------|----------|
| 填充率 | 合批 + 图集 | 大量小图标 |
| 过度绘制 | 层级合并 + 模板测试 | 复杂UI叠加 |
| 透明混合 | 预乘Alpha + 不透明分离 | 半透明效果 |

## 3. 详细实现方案

### 3.1 异步资源加载 (AsyncIconLoader.cs)
```csharp
public class AsyncIconLoader : MonoBehaviour
{
    // LRU缓存（最大100个图标）
    private class IconCacheEntry
    {
        public string IconId;
        public Sprite Sprite;
        public DateTime LastAccessTime;
        public int AccessCount;
    }

    private readonly LRUCache<string, Sprite> _iconCache =
        new LRUCache<string, Sprite>(maxSize: 100);

    private readonly Queue<LoadRequest> _loadQueue = new Queue<LoadRequest>();
    private readonly Dictionary<string, List<Action<Sprite>>> _pendingCallbacks =
        new Dictionary<string, List<Action<Sprite>>>();

    private const int MAX_CONCURRENT_LOADS = 3;
    private int _currentLoads = 0;

    public void LoadIcon(string iconPath, Action<Sprite> callback)
    {
        // 1. 检查缓存
        if (_iconCache.TryGet(iconPath, out var cached))
        {
            callback?.Invoke(cached);
            return;
        }

        // 2. 合并相同路径的请求
        if (_pendingCallbacks.TryGetValue(iconPath, out var callbacks))
        {
            callbacks.Add(callback);
            return;
        }

        // 3. 新请求入队
        _pendingCallbacks[iconPath] = new List<Action<Sprite>> { callback };
        _loadQueue.Enqueue(new LoadRequest { IconPath = iconPath });

        // 4. 触发加载
        ProcessQueue();
    }

    private async void ProcessQueue()
    {
        while (_currentLoads < MAX_CONCURRENT_LOADS && _loadQueue.Count > 0)
        {
            var request = _loadQueue.Dequeue();
            _currentLoads++;

            // 使用Addressables异步加载
            var handle = Addressables.LoadAssetAsync<Sprite>(request.IconPath);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var sprite = handle.Result;
                _iconCache.Set(request.IconPath, sprite);

                // 通知所有等待的回调
                if (_pendingCallbacks.TryGetValue(request.IconPath, out var callbacks))
                {
                    foreach (var callback in callbacks)
                    {
                        callback?.Invoke(sprite);
                    }
                    _pendingCallbacks.Remove(request.IconPath);
                }
            }

            _currentLoads--;
            Addressables.Release(handle);
        }
    }
}
```

### 3.2 槽位对象池 (UIPoolManager.cs)
```csharp
public class UIPoolManager : MonoBehaviour
{
    [System.Serializable]
    public class PoolConfig
    {
        public GameObject Prefab;
        public int InitialSize = 10;
        public int MaxSize = 50;
        public bool Expandable = true;
    }

    [SerializeField] private PoolConfig _slotPoolConfig;
    [SerializeField] private PoolConfig _tooltipPoolConfig;

    private Dictionary<string, ObjectPool> _pools = new Dictionary<string, ObjectPool>();

    private class ObjectPool
    {
        private readonly Queue<GameObject> _inactive = new Queue<GameObject>();
        private readonly GameObject _prefab;
        private readonly Transform _poolParent;
        private readonly int _maxSize;
        private readonly bool _expandable;

        public int TotalCount { get; private set; }
        public int ActiveCount => TotalCount - _inactive.Count;

        public ObjectPool(GameObject prefab, Transform parent, int initialSize, int maxSize, bool expandable)
        {
            _prefab = prefab;
            _poolParent = parent;
            _maxSize = maxSize;
            _expandable = expandable;

            // 预热
            for (int i = 0; i < initialSize; i++)
            {
                var obj = CreateNew();
                obj.SetActive(false);
                _inactive.Enqueue(obj);
            }
        }

        public GameObject Get()
        {
            if (_inactive.Count > 0)
            {
                var obj = _inactive.Dequeue();
                obj.SetActive(true);
                return obj;
            }

            if (_expandable && TotalCount < _maxSize)
            {
                var obj = CreateNew();
                obj.SetActive(true);
                return obj;
            }

            // 池已满且不可扩展，复用最旧的活跃对象
            return null;
        }

        public void Return(GameObject obj)
        {
            if (obj == null) return;

            obj.SetActive(false);
            obj.transform.SetParent(_poolParent);
            _inactive.Enqueue(obj);
        }

        private GameObject CreateNew()
        {
            var obj = Instantiate(_prefab, _poolParent);
            TotalCount++;
            return obj;
        }
    }

    public GameObject GetSlot()
    {
        return GetFromPool("Slot");
    }

    public void ReturnSlot(GameObject slot)
    {
        ReturnToPool("Slot", slot);
    }

    private GameObject GetFromPool(string poolName)
    {
        if (_pools.TryGetValue(poolName, out var pool))
        {
            return pool.Get();
        }
        return null;
    }

    private void ReturnToPool(string poolName, GameObject obj)
    {
        if (_pools.TryGetValue(poolName, out var pool))
        {
            pool.Return(obj);
        }
    }
}
```

### 3.3 虚拟化滚动 (VirtualizedScrollView.cs)
```csharp
public class VirtualizedScrollView : MonoBehaviour
{
    [SerializeField] private RectTransform _viewport;
    [SerializeField] private RectTransform _content;
    [SerializeField] private float _slotHeight = 80f;
    [SerializeField] private float _slotSpacing = 10f;

    private readonly List<GameObject> _visibleSlots = new List<GameObject>();
    private readonly Dictionary<int, SlotViewModel> _dataSource = new Dictionary<int, SlotViewModel>();

    private int _totalItems = 0;
    private int _firstVisibleIndex = 0;
    private int _lastVisibleIndex = 0;

    private void Update()
    {
        // 检查视口变化，更新可见项
        UpdateVisibleItems();
    }

    private void UpdateVisibleItems()
    {
        // 计算可见范围
        var viewportTop = -_viewport.anchoredPosition.y;
        var viewportBottom = viewportTop - _viewport.rect.height;

        int newFirstIndex = Mathf.FloorToInt(viewportTop / (_slotHeight + _slotSpacing));
        int newLastIndex = Mathf.CeilToInt(viewportBottom / (_slotHeight + _slotSpacing));

        newFirstIndex = Mathf.Max(0, newFirstIndex);
        newLastIndex = Mathf.Min(_totalItems - 1, newLastIndex);

        // 更新可见项
        if (newFirstIndex != _firstVisibleIndex || newLastIndex != _lastVisibleIndex)
        {
            RecycleInvisibleSlots(newFirstIndex, newLastIndex);
            CreateVisibleSlots(newFirstIndex, newLastIndex);

            _firstVisibleIndex = newFirstIndex;
            _lastVisibleIndex = newLastIndex;
        }
    }

    private void RecycleInvisibleSlots(int newFirst, int newLast)
    {
        for (int i = _visibleSlots.Count - 1; i >= 0; i--)
        {
            var slot = _visibleSlots[i];
            var slotIndex = (int)slot.GetComponent<InventorySlotView>().SlotIndex;

            if (slotIndex < newFirst || slotIndex > newLast)
            {
                // 回收不可见槽位
                _visibleSlots.RemoveAt(i);
                ReturnSlotToPool(slot);
            }
        }
    }

    private void CreateVisibleSlots(int firstIndex, int lastIndex)
    {
        for (int i = firstIndex; i <= lastIndex; i++)
        {
            if (!IsSlotVisible(i))
            {
                var slot = GetSlotFromPool();
                ConfigureSlot(slot, i);
                _visibleSlots.Add(slot);
            }
        }
    }

    private bool IsSlotVisible(int index)
    {
        foreach (var slot in _visibleSlots)
        {
            if ((int)slot.GetComponent<InventorySlotView>().SlotIndex == index)
                return true;
        }
        return false;
    }
}
```

### 3.4 事件批处理系统
```csharp
public class EventBatchProcessor : MonoBehaviour
{
    private class BatchQueue<T> where T : struct, IEvent
    {
        private readonly List<T> _events = new List<T>();
        private readonly Action<List<T>> _batchHandler;
        private readonly int _maxBatchSize;
        private readonly float _maxBatchTime;

        private float _lastProcessTime;

        public BatchQueue(Action<List<T>> handler, int maxSize = 10, float maxTime = 0.1f)
        {
            _batchHandler = handler;
            _maxBatchSize = maxSize;
            _maxBatchTime = maxTime;
        }

        public void Add(T evt)
        {
            _events.Add(evt);

            // 检查触发条件
            if (_events.Count >= _maxBatchSize ||
                Time.time - _lastProcessTime >= _maxBatchTime)
            {
                ProcessBatch();
            }
        }

        private void ProcessBatch()
        {
            if (_events.Count == 0) return;

            var batch = new List<T>(_events);
            _events.Clear();

            _batchHandler?.Invoke(batch);
            _lastProcessTime = Time.time;
        }
    }

    // 使用示例：背包更新事件批处理
    private BatchQueue<SlotUpdatedEvent> _slotUpdateQueue;

    private void Start()
    {
        _slotUpdateQueue = new BatchQueue<SlotUpdatedEvent>(ProcessSlotUpdates);

        // 订阅高频事件
        EventBus.Subscribe<SlotUpdatedEvent>(evt => _slotUpdateQueue.Add(evt));
    }

    private void ProcessSlotUpdates(List<SlotUpdatedEvent> batch)
    {
        // 批量更新UI
        foreach (var evt in batch)
        {
            // 合并相同槽位的更新
        }
    }
}
```

## 4. 性能监控与调试

### 4.1 性能指标收集
```csharp
public class PerformanceMonitor : MonoBehaviour
{
    public struct PerformanceMetrics
    {
        public float OpenTimeMs;
        public float MemoryMB;
        public float GCAllocKB;
        public float CPUMs;
        public int FrameDrops;
    }

    private PerformanceMetrics _current;
    private PerformanceMetrics _peak;
    private readonly List<PerformanceMetrics> _history = new List<PerformanceMetrics>();

    private void Update()
    {
        // 收集当前帧数据
        _current.CPUMs = Time.deltaTime * 1000;
        _current.GCAllocKB = GetGCAllocatedKB();

        // 检测掉帧
        if (Time.deltaTime > 0.0167f) // 60FPS阈值
            _current.FrameDrops++;

        // 更新峰值
        if (_current.CPUMs > _peak.CPUMs) _peak.CPUMs = _current.CPUMs;
        if (_current.MemoryMB > _peak.MemoryMB) _peak.MemoryMB = _current.MemoryMB;
    }

    public void RecordInventoryOpen(float openTimeMs)
    {
        _current.OpenTimeMs = openTimeMs;
        _history.Add(_current);

        // 触发性能警报
        if (openTimeMs > 100f)
        {
            Debug.LogWarning($"背包打开延迟过高: {openTimeMs}ms");
        }
    }
}
```

### 4.2 编辑器性能分析工具
```csharp
#if UNITY_EDITOR
public class InventoryPerformanceProfiler : EditorWindow
{
    [MenuItem("Tools/Inventory/Performance Profiler")]
    public static void ShowWindow()
    {
        GetWindow<InventoryPerformanceProfiler>("Inventory Profiler");
    }

    private void OnGUI()
    {
        // 显示实时性能指标
        GUILayout.Label("实时指标", EditorStyles.boldLabel);
        GUILayout.Label($"打开延迟: {PerformanceMonitor.Current.OpenTimeMs:F1}ms");
        GUILayout.Label($"内存占用: {PerformanceMonitor.Current.MemoryMB:F1}MB");
        GUILayout.Label($"GC分配: {PerformanceMonitor.Current.GCAllocKB:F1}KB/帧");

        // 性能测试按钮
        if (GUILayout.Button("运行性能测试"))
        {
            RunPerformanceTest();
        }
    }

    private void RunPerformanceTest()
    {
        // 模拟1000次物品添加/移除
        // 记录性能数据
    }
}
#endif
```

## 5. 平台特定优化

### 5.1 移动端优化
| 优化项 | 策略 | 效果 |
|--------|------|------|
| 内存 | 降低图标分辨率 (512→256) | 节省75%内存 |
| CPU | 简化动画，减少Update调用 | 降低30%CPU |
| GPU | 禁用高级特效，减少DrawCall | 降低渲染压力 |

### 5.2 PC端优化
| 优化项 | 策略 | 效果 |
|--------|------|------|
| 加载 | 预加载常用资源 | 减少卡顿 |
| 渲染 | 启用GPU实例化 | 提升渲染效率 |
| 多线程 | 使用JobSystem处理数据 | 利用多核CPU |

## 6. 风险评估与缓解

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 异步加载延迟 | 中 | 高 | 预加载 + 占位符 |
| 对象池泄漏 | 低 | 中 | 引用计数 + 自动清理 |
| 内存溢出 | 低 | 高 | 资源卸载 + 监控 |
| 事件竞争 | 中 | 中 | 队列 + 锁机制 |

## 7. 验收标准

### 7.1 性能指标
- [ ] 背包打开延迟 < 100ms (95%分位数)
- [ ] 稳定状态下GC分配 < 1KB/帧
- [ ] 内存峰值 < 50MB
- [ ] 60FPS下CPU占用 < 2ms

### 7.2 功能完整性
- [ ] 所有交互功能正常
- [ ] 动画流畅无卡顿
- [ ] 错误处理完善
- [ ] 平台兼容性验证

### 7.3 代码质量
- [ ] 无内存泄漏
- [ ] 事件订阅/取消订阅正确
- [ ] 异步操作异常处理
- [ ] 单元测试覆盖率 > 80%