# 背包系统UI架构设计方案

## 1. 架构设计目标

### 1.1 核心原则
- **五层菱形架构合规**: 严格遵循 `05_Show` → `03_Core` 的依赖流向
- **跨层通信解耦**: 业务层→表现层通过EventBus，表现层→业务层通过ServiceLocator
- **数据驱动配置**: 所有UI参数通过ScriptableObject配置，支持非代码修改
- **性能优先**: 背包打开延迟 < 100ms，GC分配接近零

### 1.2 技术栈选择
- **UI模式**: MVP (Model-View-Presenter) 变体，增强数据绑定支持
- **数据绑定**: 基于ViewModel的手动绑定，避免反射开销
- **资源管理**: Unity Addressables + 对象池 + 异步加载
- **事件系统**: 现有EventBus扩展，支持结构体事件

## 2. 目录结构实现

```
05_Show/Inventory/
├── 📁 Managers/           # 管理器层
│   ├── InventoryUIManager.cs      (UI总协调器)
│   └── InventoryUIManager.prefab
├── 📁 Presenters/         # Presenter层
│   ├── InventoryPresenter.cs      (主业务逻辑)
│   └── QuickSlotPresenter.cs      (快捷栏逻辑)
├── 📁 ViewModels/         # ViewModel层
│   ├── InventoryViewModel.cs      (背包数据模型)
│   └── SlotViewModel.cs           (槽位数据模型)
├── 📁 Views/              # View层
│   ├── Components/
│   │   ├── InventoryPanelView.cs
│   │   ├── InventorySlotView.cs
│   │   ├── QuickSlotBarView.cs
│   │   └── ItemTooltipView.cs
│   ├── Controls/
│   │   ├── InventoryDragHandler.cs
│   │   └── SlotClickHandler.cs
│   └── Layouts/
│       ├── GridLayout.cs
│       └── VirtualizedScrollView.cs
├── 📁 Events/             # UI事件定义
│   ├── InventoryUIEvents.cs
│   └── UIInteractionEvents.cs
├── 📁 Configs/            # 配置系统
│   ├── InventoryUIConfigSO.cs     (UI参数配置)
│   └── UIAssetConfigSO.cs         (资产引用配置)
├── 📁 Utils/              # 工具类
│   ├── UIPoolManager.cs           (对象池管理)
│   ├── AsyncIconLoader.cs         (异步加载)
│   └── EventBatchProcessor.cs     (事件批处理)
└── 📁 Prefabs/           # 预制体资源
    ├── InventoryPanel.prefab
    ├── InventorySlot.prefab
    ├── QuickSlotBar.prefab
    └── ItemTooltip.prefab
```

## 3. 核心类设计

### 3.1 类关系图 (UML简化)
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   InventoryUI   │    │   Inventory     │    │    Inventory    │
│    Manager      │◄───┤   Presenter     │◄───┤   ViewModel     │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                      │                      │
         ▼                      ▼                      ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   View组件      │    │   EventBus      │    │ ServiceLocator  │
│   (Panel/Slot)  │───►│   (通信总线)    │◄───┤   (服务定位)    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                              │                      │
                              ▼                      ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   ConfigSO      │    │   业务系统      │    │   数据层        │
│   (UI配置)      │    │   (03_Core)     │    │   (01_Data)     │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### 3.2 关键组件职责

| 组件 | 层 | 职责 | 依赖 |
|------|----|------|------|
| `InventoryUIManager` | Manager | UI生命周期协调，全局状态管理 | Config, Presenter |
| `InventoryPresenter` | Presenter | 业务-UI转换，事件路由 | ViewModel, EventBus, ServiceLocator |
| `InventoryViewModel` | ViewModel | UI数据持有，状态封装 | 纯C#，无Unity依赖 |
| `InventorySlotView` | View | 视觉表现，用户输入接收 | ViewModel, UI事件 |
| `InventoryUIConfigSO` | Config | 数据驱动配置 | ScriptableObject |

## 4. 事件通信机制

### 4.1 事件分类
```
📁 事件定义层级：
├── 业务事件 (02_Base/EventBus/Events/)
│   ├── ItemAddedToInventoryEvent
│   ├── ItemRemovedFromInventoryEvent
│   └── InventoryFullEvent
├── UI事件 (05_Show/Inventory/Events/)
│   ├── SlotClickedEvent
│   ├── SlotDragStartedEvent
│   ├── SlotDragEndedEvent
│   └── InventoryToggleEvent
└── 反馈事件 (Presenter内部)
    ├── UIFeedbackEvent
    ├── UINotificationEvent
    └── UIContextMenuEvent
```

### 4.2 通信流程图
```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   用户操作   │ → │   View组件   │ → │   UI事件    │
└─────────────┘    └─────────────┘    └─────────────┘
        │                                    │
        ▼                                    ▼
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│  View更新    │ ← │  Presenter   │ ← │  EventBus    │
└─────────────┘    └─────────────┘    └─────────────┘
        ▲                                    │
        │                                    ▼
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│ ViewModel    │ ← │   业务调用   │ ← │ServiceLocator│
└─────────────┘    └─────────────┘    └─────────────┘
        ▲                                    │
        │                                    ▼
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│ 数据绑定    │ ← │ 业务事件     │ ← │  业务系统    │
└─────────────┘    └─────────────┘    └─────────────┘
```

## 5. 性能优化架构

### 5.1 四级优化策略
| 层级 | 优化技术 | 目标 | 实现类 |
|------|----------|------|--------|
| **内存** | 对象池 + LRU缓存 | 减少70%内存 | `UIPoolManager`, `AsyncIconLoader` |
| **CPU** | 虚拟化 + 事件批处理 | <2ms/帧 | `VirtualizedScrollView`, `EventBatchProcessor` |
| **加载** | 异步分帧 + 预加载 | <100ms打开 | `AsyncIconLoader`, 分帧初始化 |
| **渲染** | 合批 + 视口裁剪 | 减少90%DrawCall | 图集打包，层级优化 |

### 5.2 关键性能指标
```csharp
// 性能验收标准
public class PerformanceTargets
{
    // 打开延迟: 95%分位数 < 100ms
    public const float OPEN_LATENCY_MS = 100f;

    // 内存占用: 峰值 < 50MB
    public const float MAX_MEMORY_MB = 50f;

    // GC分配: 稳定状态 < 1KB/帧
    public const float MAX_GC_ALLOC_KB = 1f;

    // CPU占用: 60FPS下 < 2ms
    public const float MAX_CPU_MS = 2f;
}
```

## 6. 预制体架构

### 6.1 预制体层级设计
```
InventoryPanel.prefab
├── Canvas (World Space)
├── EventSystem
└── InventoryRoot
    ├── Background (9-slice)
    ├── HeaderPanel
    │   ├── TitleText
    │   ├── CloseButton
    │   └── FilterDropdown
    ├── SlotContainer (GridLayoutGroup)
    │   ├── Slot_0 (InventorySlot.prefab)
    │   ├── Slot_1
    │   └── ...Slot_23
    ├── QuickSlotBar
    │   ├── QuickSlot_0 (QuickSlot.prefab)
    │   └── ...QuickSlot_9
    └── FooterPanel
        ├── WeightText
        ├── GoldText
        └── SortButton

InventorySlot.prefab (可复用)
├── SlotRoot
├── Background (Image)
├── ItemIcon (Image) - 默认隐藏
├── ItemCount (Text) - 默认隐藏
├── SelectionIndicator - 默认隐藏
└── DragVisual (CanvasGroup)
```

### 6.2 资源管理策略
```csharp
// Addressable资产分组策略
public class AssetGroups
{
    // 核心UI (必须预加载)
    public const string UI_INVENTORY_CORE = "UI_Inventory_Core";

    // 图标资源 (按需加载)
    public const string UI_ICONS = "UI_Icons";

    // 音效资源 (延迟加载)
    public const string UI_SOUNDS = "UI_Sounds";

    // MOD资源 (动态加载)
    public const string MOD_UI = "Mods/UI";
}
```

## 7. 扩展性设计

### 7.1 MOD支持接口
```csharp
public interface IInventoryUIProvider
{
    // MOD可以提供的扩展
    GameObject GetCustomSlotPrefab();
    Sprite GetItemIconOverride(string itemId);
    InventorySlotConfigSO GetSlotConfigOverride();

    // 事件钩子
    void OnSlotClicked(int slotIndex, SlotType slotType);
    void OnItemDragged(int sourceIndex, int targetIndex);
}

// 默认实现
public class DefaultInventoryUIProvider : IInventoryUIProvider
{
    public GameObject GetCustomSlotPrefab() => null; // 返回null使用默认
    // ...
}
```

### 7.2 配置覆盖机制
- **优先级**: MOD配置 > 用户配置 > 默认配置
- **热重载**: 配置修改实时生效（编辑器模式）
- **多语言**: 通过ScriptableObject支持本地化

## 8. 与现有系统集成

### 8.1 依赖现有系统
```csharp
// 项目已有的基础设施
public class ExistingSystems
{
    // 事件总线 (02_Base/EventBus/)
    public static EventBus EventBus { get; } // 结构体事件，零GC

    // 服务定位器 (02_Base/ServiceLocator/)
    public static ServiceLocator Services { get; } // 轻量级服务注册

    // 计时器系统 (02_Base/Timer/)
    public static TimerSystem Timer { get; } // 对象池驱动

    // 单例基类 (02_Base/Singleton/)
    public class MonoSingleton<T> { } // 仅用于基础设施管理器
}
```

### 8.2 集成点
1. **业务系统注册**: 背包系统在Awake中注册到ServiceLocator
2. **事件订阅**: Presenter订阅业务层事件，更新UI
3. **配置加载**: 通过Resources或Addressables加载ConfigSO
4. **存档集成**: 通过ISaveable接口支持UI状态保存

## 9. 文件级修改建议

### 9.1 需要创建的文件 (已创建)
```
✅ 05_Show/Inventory/Events/InventoryUIEvents.cs
✅ 05_Show/Inventory/ViewModels/InventoryViewModel.cs
✅ 05_Show/Inventory/ViewModels/SlotViewModel.cs
✅ 05_Show/Inventory/Presenters/InventoryPresenter.cs
✅ 05_Show/Inventory/Configs/InventoryUIConfigSO.cs
```

### 9.2 需要创建的文件 (待实现)
```
📝 05_Show/Inventory/Managers/InventoryUIManager.cs
📝 05_Show/Inventory/Views/Components/InventorySlotView.cs
📝 05_Show/Inventory/Views/Components/InventoryPanelView.cs
📝 05_Show/Inventory/Utils/UIPoolManager.cs
📝 05_Show/Inventory/Utils/AsyncIconLoader.cs
```

### 9.3 需要修改的现有文件
```
🔧 02_Base/EventBus/Events/InventoryEvents.cs
   - 添加ItemMovedEvent、SlotSelectedEvent等

🔧 03_Core/ (业务层)
   - 实现IInventorySystem接口
   - 注册到ServiceLocator
```

## 10. 风险与缓解措施

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| **异步加载延迟** | 中 | 高 | 预加载关键资源 + 占位符动画 |
| **对象池泄漏** | 低 | 中 | 引用计数 + 自动清理定时器 |
| **事件竞争条件** | 中 | 中 | 事件队列 + 主线程调度 |
| **MOD兼容性问题** | 高 | 中 | 接口版本控制 + 向后兼容 |
| **性能达标困难** | 中 | 高 | 分级回退 + 性能监控 |

## 11. 验收标准

### 11.1 功能完整性
- [ ] 24个主槽位 + 10个快捷栏正常显示
- [ ] 物品拖拽、点击、右键菜单功能正常
- [ ] 背包开关动画流畅
- [ ] 物品提示框显示正确信息

### 11.2 性能指标
- [ ] 背包打开延迟 < 100ms (95%分位数)
- [ ] 稳定状态下GC分配 < 1KB/帧
- [ ] 内存峰值 < 50MB
- [ ] 60FPS下CPU占用 < 2ms

### 11.3 架构合规
- [ ] 符合五层菱形架构依赖规则
- [ ] 跨层通信使用EventBus/ServiceLocator
- [ ] 无业务逻辑泄漏到表现层
- [ ] 配置文件驱动，支持非代码修改

### 11.4 扩展性验证
- [ ] MOD可以替换槽位预制体
- [ ] MOD可以添加新的物品图标
- [ ] 配置修改实时生效（编辑器）
- [ ] 支持多分辨率适配

## 12. 实施路线图

### 阶段1: 核心架构 (2-3天)
1. ✅ 创建目录结构和基础类
2. ✅ 实现ViewModel和Presenter
3. ✅ 创建ConfigSO配置系统
4. 📝 集成现有EventBus和ServiceLocator

### 阶段2: UI实现 (3-4天)
1. 📝 创建预制体和View组件
2. 📝 实现拖拽和点击交互
3. 📝 集成异步资源加载
4. 📝 实现虚拟化滚动

### 阶段3: 性能优化 (2-3天)
1. 📝 实现对象池系统
2. 📝 添加事件批处理
3. 📝 集成性能监控
4. 📝 平台特定优化

### 阶段4: 集成测试 (1-2天)
1. 📝 与业务系统集成
2. 📝 跨平台测试
3. 📝 性能指标验证
4. 📝 文档完善

---

**设计者**: Unity UI Architect Agent
**日期**: 2026-03-26
**版本**: 1.0
**状态**: 架构设计完成，准备实施