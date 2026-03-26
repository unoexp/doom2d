# 背包容量扩展系统完善计划
**版本**: 1.0
**日期**: 2026-03-26
**状态**: 规划阶段

## 1. 当前系统状态总结

### ✅ 已完成的核心组件
| 组件 | 位置 | 状态 | 备注 |
|------|------|------|------|
| **数据层** | 01_Data | 完成 | CapacityExpansionConfigSO, ExpansionDefinitionSO |
| **事件层** | 02_Base | 完成 | InventoryExpansionEvents.cs |
| **核心业务层** | 03_Core | 完成 | InventorySystem扩展API, ExpansionStateManager |
| **服务层** | 03_Core | 完成 | 验证、消耗、效果服务实现 |
| **ViewModel层** | 05_Show | 完成 | 支持动态槽位的InventoryViewModel |

### ⚠️ 缺失的集成组件
| 组件 | 位置 | 状态 | 优先级 |
|------|------|------|--------|
| **扩展UI组件** | 05_Show/Views/Components/ | 缺失 | 高 |
| **扩展UI管理器** | 05_Show/Managers/ | 缺失 | 高 |
| **事件处理集成** | 05_Show/Presenters/ | 部分 | 中 |
| **资源加载机制** | 03_Core/InventorySystem | 缺失 | 中 |
| **测试配置** | Resources/Expansions/ | 缺失 | 低 |

## 2. 详细实施计划

### 阶段1：创建扩展UI组件 (预计: 1天)
#### 目标：提供玩家交互界面
**文件创建清单：**
1. `05_Show/Inventory/Views/Components/ExpansionPanelView.cs`
   - 扩展面板主UI组件
   - 显示可用扩展列表
   - 显示扩展详情和条件

2. `05_Show/Inventory/Views/Components/ExpansionSlotView.cs`
   - 扩展槽位显示组件
   - 显示扩展图标和状态
   - 交互反馈（悬停、点击）

3. `05_Show/Inventory/Views/Components/ExpansionProgressView.cs`
   - 扩展进度显示组件
   - 进度条和剩余时间显示

**预制体需求：**
- `Prefabs/ExpansionPanel.prefab` - 扩展面板
- `Prefabs/ExpansionSlot.prefab` - 扩展槽位

### 阶段2：创建扩展UI管理器 (预计: 0.5天)
#### 目标：协调扩展UI生命周期
**文件创建清单：**
1. `05_Show/Inventory/Managers/ExpansionUIManager.cs`
   - 管理扩展面板的打开/关闭
   - 协调多个扩展UI组件
   - 处理UI状态同步

### 阶段3：完善InventoryPresenter扩展事件处理 (预计: 1天)
#### 目标：集成扩展系统到现有UI流程
**修改清单：**
1. **订阅扩展事件**（添加到`SubscribeToBusinessEvents()`）：
   ```csharp
   EventBus.Subscribe<InventoryExpansionValidationStartedEvent>(OnExpansionValidationStarted);
   EventBus.Subscribe<InventoryExpansionValidationResultEvent>(OnExpansionValidationResult);
   EventBus.Subscribe<InventoryExpansionEffectAppliedEvent>(OnExpansionEffectApplied);
   EventBus.Subscribe<InventoryExpansionStatusUpdatedEvent>(OnExpansionStatusUpdated);
   ```

2. **实现事件处理方法**：
   - 更新ViewModel状态
   - 触发UI动画反馈
   - 显示扩展结果通知

3. **添加快捷访问方法**：
   - 打开扩展面板
   - 执行扩展操作
   - 检查扩展状态

### 阶段4：完善扩展配置加载机制 (预计: 0.5天)
#### 目标：提供扩展配置的动态加载
**修改清单：**
1. 在`InventorySystem.InitializeExpansionSystem()`中添加：
   ```csharp
   private void LoadExpansionConfigs()
   {
       var configs = Resources.LoadAll<ExpansionDefinitionSO>("Expansions/");
       foreach (var config in configs)
       {
           RegisterExpansion(config);
       }
   }
   ```

2. 创建测试配置目录：
   - `Resources/Expansions/` - 存放测试扩展配置
   - 创建示例`.asset`文件

### 阶段5：测试和验证 (预计: 1天)
#### 目标：确保完整流程正常工作
**测试清单：**
1. **单元测试**：
   - 扩展条件验证
   - 扩展效果应用
   - 动态槽位更新

2. **集成测试**：
   - 完整扩展流程（验证→消耗→应用）
   - UI事件响应
   - 状态持久化

3. **性能测试**：
   - 扩展系统内存使用
   - UI响应时间
   - 事件处理性能

## 3. 文件依赖关系图

```
扩展系统集成依赖关系
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Extension     │    │   Inventory     │    │   Expansion     │
│    PanelView    │←───┤   Presenter     │←───┤  UIManager      │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  ExpansionSlot  │    │   EventBus      │    │  ServiceLocator │
│      View       │───►│   (扩展事件)    │◄───┤   (获取服务)    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                              │                       │
                              ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  ViewModel      │    │   Inventory     │    │   扩展服务      │
│  (动态槽位)     │◄───┤    System       │───►│   (验证/消耗)   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## 4. 实施顺序建议

### 推荐执行顺序：
1. **先创建UI组件**（阶段1）
   - 原因：UI组件独立，不依赖其他系统修改
   - 产出：可视化界面，便于后续测试

2. **然后创建UI管理器**（阶段2）
   - 原因：依赖UI组件，为Presenter提供统一接口
   - 产出：UI生命周期管理

3. **接着完善Presenter**（阶段3）
   - 原因：依赖UI管理器和事件系统
   - 产出：完整的业务-UI集成

4. **最后完善加载机制**（阶段4）并测试（阶段5）
   - 原因：依赖前面所有组件
   - 产出：完整可用的扩展系统

### 并行开发建议：
- **UI组件**和**配置加载**可以并行开发
- **测试配置创建**可以与UI开发并行
- **单元测试**可以与实现并行编写

## 5. 风险与缓解措施

| 风险 | 可能性 | 影响 | 缓解措施 |
|------|--------|------|----------|
| **UI组件性能问题** | 中 | 中 | 使用对象池，限制同时显示的扩展数量 |
| **事件竞争条件** | 低 | 高 | 使用事件队列，主线程调度 |
| **扩展状态同步延迟** | 中 | 中 | 添加状态同步机制，定期刷新 |
| **资源加载失败** | 低 | 中 | 添加回退机制，使用默认配置 |
| **内存泄漏** | 低 | 高 | 使用WeakReference，添加清理机制 |

## 6. 验收标准

### 功能验收
- [ ] 扩展面板正常打开/关闭
- [ ] 可用扩展列表正确显示
- [ ] 扩展条件验证反馈正确
- [ ] 扩展效果应用后槽位动态增加
- [ ] 扩展状态实时更新
- [ ] 扩展历史记录可查看

### 性能验收
- [ ] 扩展面板打开延迟 < 200ms
- [ ] 扩展操作响应时间 < 100ms
- [ ] 内存使用增量 < 10MB
- [ ] 60FPS下CPU占用 < 1ms

### 架构验收
- [ ] 符合五层菱形架构
- [ ] 跨层通信使用EventBus
- [ ] 无业务逻辑泄漏到表现层
- [ ] 配置文件驱动，支持非代码修改

## 7. 资源需求

### 美术资源
- 扩展图标（至少3种）
- 进度条UI元素
- 状态指示器图标

### 配置资源
- 测试扩展配置（至少2个）
- UI布局配置
- 本地化文本（可选）

### 时间预估
- **总时间**: 4天
- **缓冲时间**: 1天
- **总周期**: 5天

## 8. 下一步行动

**立即行动：**
1. 创建扩展UI组件（ExpansionPanelView）
2. 创建扩展UI管理器
3. 开始完善InventoryPresenter

**后续行动：**
4. 实现扩展配置加载
5. 创建测试配置
6. 进行全面测试

---
**计划制定者**: Claude Code
**最后更新**: 2026-03-26
**状态**: 等待执行