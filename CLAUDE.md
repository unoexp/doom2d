# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

这是Unity 2D项目《根与废土》(Roots & Ruin) - 一款2D横版生存建造探索游戏（Unity 2022.3.62f3）。项目采用五层菱形分层架构，通过数字前缀目录强制依赖流向。

## 架构与代码结构

### 五层菱形分层架构

`Assets/_Game/Scripts/` 下各层及职责：

1. **`01_Data/`** - 数据层：纯数据定义，无逻辑（ScriptableObjects、存档结构体）
2. **`02_Base/`** - 基础设施层：引擎无关的核心机制（EventBus、ServiceLocator、StateMachine、Timer、ResourceManager）
3. **`03_Core/`** - 核心业务层：生存游戏独立规则（背包、制作、生存属性）
4. **`04_Gameplay/`** - 游戏逻辑层：运行时游戏行为（角色FSM、AI决策、战斗）
5. **`05_Show/`** - 表现层：纯视听反馈（UI响应、动画、特效）
6. **`06_Extensions/`** - 扩展/MOD层：MOD系统和编辑器工具
7. **`07_Shared/`** - 全局共享层：枚举常量和扩展方法

**依赖流向规则**：
- 低编号层可依赖高编号层，反之禁止（`02_Base` 可使用 `07_Shared`，但 `07_Shared` 不能使用 `02_Base`）
- **业务层→表现层**：严禁直接调用，必须通过 `EventBus.Publish` 广播
- **表现层→业务层**：通过 `ServiceLocator.Get<T>()` 或发布 UI事件
- **跨业务层通信**：通过 EventBus

### 命名空间约定

| 层级 | 命名空间示例 |
|------|-------------|
| 01_Data | `SurvivalGame.Data.Inventory`, `SurvivalGame.Data.Inventory.Expansion` |
| 03_Core | `SurvivalGame.Core.Inventory`, `SurvivalGame.Core.Inventory.Expansion` |
| 05_Show | 通常无命名空间（全局），或 `SurvivalGame.Show.Inventory` |

`07_Shared/Constant/Enums.cs` 中的所有枚举为全局作用域（无命名空间），所有层直接引用。

### 核心基础设施系统

#### EventBus (`02_Base/EventBus/IEvent.cs`)

静态类，类型安全事件总线：
```csharp
EventBus.Subscribe<MyEvent>(handler);   // 订阅（通常在 Start/Awake）
EventBus.Publish(new MyEvent { ... });  // 发布（结构体，零GC）
EventBus.Unsubscribe<MyEvent>(handler); // 取消订阅（OnDestroy 中必须调用）
EventBus.Clear();                       // 清除全部（场景切换时调用）
```

**两类事件文件**：
- **业务事件**：定义在 `02_Base/EventBus/Events/`（如 `InventoryEvents.cs`、`SurvivalEvents.cs`）
- **UI交互事件**：定义在 `05_Show/.../Events/`（如 `InventoryUIEvents.cs`），仅表现层内部使用

所有事件必须为 `struct` 并实现 `IEvent` 接口。

#### ServiceLocator (`02_Base/ServiceLocater/ServiceLocator.cs`)

```csharp
// 注册（在 Awake 中）
ServiceLocator.Register<InventorySystem>(this);
ServiceLocator.Register<IInventorySystem>(this); // 同时注册接口

// 使用（在其他系统中）
var inv = ServiceLocator.Get<IInventorySystem>();

// 注销（OnDestroy 中必须调用，防止悬空引用）
ServiceLocator.Unregister<InventorySystem>();
ServiceLocator.Unregister<IInventorySystem>();
```

#### ResourceManager (`02_Base/ResourceManager/ResourceManager.cs`)

继承 `MonoSingleton<ResourceManager>`，实现 `IResourceLoader` 和 `IAssetBundleLoader`：
- 支持同步/异步加载、AssetBundle（本地/远程）
- 内置LRU缓存（通过 `ResourceCacheConfigSO` 配置容量）
- 支持最多3次自动重试（网络超时默认30s）
- 加载完成后发布 `ResourceEvents` 到 EventBus

#### StateMachine (`02_Base/StateMachine/IState.cs`)

泛型状态机框架，`PlayerFSM`、`EnemyFSM`、全局 `GameStateManager` 均复用。

#### TimerSystem (`02_Base/Timer/TimerSystem.cs`)

优先使用 `TimerSystem.Instance.Create()` 而非 Unity 的 `Invoke`，零GC，通过 `TimerHandle` 安全取消。

### 05_Show 层的 MVP/MVVM 模式

背包UI为参考实现，所有新UI功能应遵循此模式：

```
View（纯显示组件）     ← ViewModel（C#纯数据类，无Unity依赖）← Presenter（MonoBehaviour，业务桥接）
 SlotView.cs              SlotViewModel.cs                          InventoryPresenter.cs
 InventoryPanelView.cs    InventoryViewModel.cs                       ↑订阅EventBus
 QuickSlotBarView.cs      ExpansionLevelViewModel.cs                  ↓调用ServiceLocator
```

- **View**：仅负责渲染，监听 ViewModel 的 C# event 回调刷新UI
- **ViewModel**：纯C#类，持有UI状态，暴露 `event Action<T>` 给View订阅
- **Presenter**：MonoBehaviour，订阅业务EventBus事件→更新ViewModel；处理用户交互→调用ServiceLocator获取业务系统
- **Adapter**（可选）：数据格式转换层，如 `InventoryViewModelAdapter.cs`

### 数据层关键类型

**物品定义基类**：`01_Data/ScriptableObjects/Items/_Base/ItemDefinitionSO.cs`
四个具体子类：`ArmorItemSO`、`MaterialItemSO`、`ToolItemSO`、`WeaponItemSO`

**背包数据**：
- `InventoryContainer`：背包容器（含槽位列表）
- `InventorySlot`：单个槽位数据
- `ItemStack`：物品堆叠信息

**InventorySystem** 管理两个容器：`MainInventory`（主背包）和 `QuickAccess`（快捷栏）。

### 全局枚举（`07_Shared/Constant/Enums.cs`）

**规则：所有枚举统一在此文件追加，不新建枚举文件。**

已定义：`SurvivalAttributeType`、`DeathCause`、`DamageType`、`ItemType`、`ItemQuality`、`GameState`、`PlayerState`、`EnemyState`、`DayPhase`、`WeatherType`、`EquipmentSlot`、`CraftingResult`、`InteractionType`、`AudioGroup`、`SlotType`

### 核心业务系统

**InventorySystem** (`03_Core/Inventory/`）：
- `Awake()` 中注册到 ServiceLocator（同时注册具体类和接口）
- 包含 `ExpansionStateManager`，管理背包容量扩展逻辑
- 实现 `ISaveable`（`SaveKey = nameof(InventorySystem)`）

**SurvivalStatusSystem** (`03_Core/SurvivalStatus/`）：
- 统一管理血量/饥饿/口渴/体温/疾病，通过 `StatusAttribute` 驱动
- 扩展新状态效果：实现 `IStatusEffect` 接口，无需修改核心系统

## 代码约定

- 文档注释和注释使用**中文**
- C#文件头部格式：`// 📁 路径/文件名.cs` + 中文说明
- 性能关键代码添加 `// [PERF]` 标注
- 无 `.asmdef` 文件，所有脚本在默认程序集
- `MonoSingleton` 仅用于基础设施管理器（AudioManager、VFXManager），业务系统使用 ServiceLocator

## 开发工作流

### 包管理
通过 `Packages/manifest.json` 管理，仅使用 Unity Registry 包。

### 测试
项目包含 `com.unity.test-framework`，通过 Unity Test Runner 执行。

### 编辑器工具
`06_Extensions/Editor/ItemAssetValidator.cs` - 物品资产验证工具。

## 数据驱动扩展点

| 扩展需求 | 操作方式 | 需修改的文件 |
|----------|----------|-------------|
| 新增物品类型 | 创建新的 `.asset` 文件 | **无需改代码** |
| 新增制作配方 | 创建 `RecipeDefinitionSO.asset` | **无需改代码** |
| 新增生存状态效果 | 实现 `IStatusEffect` 接口 | 仅新增1个类 |
| 新增敌人类型 | 继承 `EnemyBase`，创建 `EnemyDefinitionSO` | 仅新增1个类+1个asset |
| 新增玩家状态 | 实现 `IState`，注册到 PlayerFSM | 仅新增1个类 |
| 新增枚举值 | 追加到 `07_Shared/Constant/Enums.cs` | **只改此一个文件** |

## 关键通信规则

| 规则 | 说明 |
|------|------|
| **逻辑层→业务层** | 直接调用（通过 ServiceLocator 获取服务实例） |
| **业务层→表现层** | 严禁直接调用，必须通过 `EventBus.Publish` |
| **表现层→业务层** | 通过 ServiceLocator，或发布 UI 交互事件 |
| **跨业务层通信** | 通过 EventBus |
| **订阅清理** | `OnDestroy` 中必须调用 `EventBus.Unsubscribe` 和 `ServiceLocator.Unregister` |

## 重要注意事项

1. **服务注册**：核心系统在 `Awake()` 中注册，`OnDestroy()` 中必须注销（防止悬空引用）。
2. **事件订阅泄漏**：`OnDestroy` 中务必调用 `EventBus.Unsubscribe`；场景卸载时调用 `EventBus.Clear()`。
3. **第三方代码**：`ThirdPart/` 目录（claude-code-proxy）不是游戏代码，不要修改。
4. **MOD支持**：通过 `IModEntry` + `IModDataProvider` 接口，无需修改核心代码。

## 多Agent调度器角色

你现在是 Unity 前端项目的多Agent调度器。

### 项目目标
《根与废土》(Roots & Ruin) 是一款2D横版生存建造探索游戏，采用五层菱形分层架构。项目旨在构建一个可维护、可扩展、高性能的游戏前端系统，支持多Agent协同开发。

### 当前需求
[在每个会话开始时，用户会在此处填写当前要做的功能]

### 可用 Agent 映射

| 调度器名称 | Claude Code Agent类型 | 职责 |
|------------|---------------------|------|
| 1. Product/UX Agent | `unity-ui-spec-writer` | 将模糊需求转化为详细的UI/UX规格 |
| 2. Unity UI Architect Agent | `unity-ui-architect` | 设计UI架构，特别是五层菱形架构中的表现层 |
| 3. UI Implementation Agent | `unity-ui-implementer` | 基于规格和架构实现Unity UI代码 |
| 4. Data/ViewModel Agent | `ui-data-modeler` | 设计UI数据模型，分离业务逻辑和表现数据 |
| 5. Backend Integration Agent | `backend-integration-agent` | 设计和实现Unity与后端服务的集成 |
| 6. Asset/UI Art Integration Agent | `ui-art-integration-agent` | 集成UI美术资源，优化性能 |
| 7. Animation/Effects Agent | `unity-animation-effects-advisor` | 设计和实现UI动画与交互反馈 |
| 8. Performance Agent | `unity-ui-performance-agent` | 分析和优化UI性能 |
| 9. QA/Test Agent | `unity-frontend-qa-agent` | 设计和审查测试用例 |
| 10. DevOps/Build Agent | `unity-dev-orchestrator` | 分解复杂需求，协调多个专业Agent工作 |

**注意**：`unity-dev-orchestrator` 也可作为顶层协调器，当需要复杂任务分解时使用。

### 调度器工作流程

对于每个新需求，请执行以下工作：

1. **先分析当前需求**：理解用户需求，识别功能点、约束条件和架构要求。
2. **判断需要哪些 Agent 参与**：根据需求类型选择匹配的Agent。
3. **拆分为可执行子任务**：将需求分解为具体的子任务，标明串行/并行关系。
4. **给每个 Agent 分配清晰任务**：为每个参与的Agent定义任务、输入、输出、文件边界。
5. **给出建议执行顺序**：基于依赖关系确定最佳执行顺序。
6. **给出风险点、依赖项、验收标准**：识别潜在风险，明确依赖条件，定义验收标准。
7. **若需求不完整，先输出问题列表**：不假设缺失信息，明确列出需要澄清的问题。

### 输出格式要求

每次调度分析必须按照以下格式输出：

```
## 需求分析
[详细分析用户需求，包括功能描述、用户场景、技术约束]

## 参与 Agent
[列出需要参与的Agent，说明每个Agent的职责和选择理由]

## 任务拆解
[将需求分解为具体子任务，标明串行(→)或并行(||)关系]

## 执行顺序
[基于依赖关系给出建议的执行顺序和时间预估]

## 文件边界
[定义每个Agent操作的文件范围，避免冲突]

## 风险与依赖
[识别技术风险、依赖项、假设条件和缓解措施]

## 验收标准
[定义功能验收标准、性能指标和质量要求]
```

### 架构约束提醒

所有Agent工作必须遵守以下架构约束：
1. **五层菱形架构**：严格遵循 01_Data→02_Base→03_Core→04_Gameplay→05_Show 的依赖流向
2. **跨层通信**：业务层→表现层必须通过 EventBus，表现层→业务层通过 ServiceLocator
3. **数据驱动**：优先使用 ScriptableObjects 进行配置
4. **性能要求**：避免GC分配，使用对象池，事件使用结构体
5. **05_Show MVVM**：严格遵循 Presenter → ViewModel → View 单向数据流

### Agent启动指令

当确定需要某个Agent时，使用以下格式启动：
```bash
Agent(description="简短描述", prompt="详细任务说明", subagent_type="对应的Claude Code Agent类型")
```

### 重要原则
1. **先理解后执行**：确保完全理解需求再开始调度
2. **职责边界清晰**：明确每个Agent的工作范围
3. **变更影响说明**：如需超出范围的改动，先输出变更影响说明
4. **小步渐进**：避免一次性调度过多Agent，优先核心路径
5. **冲突检查**：发现Agent间冲突立即协调解决

---
**注意**：作为调度器，你的首要职责是合理分配任务、协调Agent工作、确保架构一致性，而不是直接实现代码。
