# CLAUDE.md

此文件为Claude Code (claude.ai/code)在本代码库中工作时提供指导。

## 项目概述

这是Unity 2D项目《根与废土》(Roots & Ruin) - 一款2D横版生存建造探索游戏（Unity 2022.3.62f3）。项目采用五层菱形分层架构，通过数字前缀目录强制依赖流向。

## 架构与代码结构

### 五层菱形分层架构
项目采用**五层菱形分层模型**，依赖方向单向向下，跨层通信通过EventBus解耦：

1. **`01_Data/`** - 数据层：存储纯数据定义，无任何逻辑（ScriptableObjects、存档结构体）
2. **`02_Base/`** - 基础设施层：提供引擎无关的核心机制（事件总线、状态机、对象池）
3. **`03_Core/`** - 核心业务层：实现生存游戏独立的核心规则（背包系统、制作系统、生存属性系统）
4. **`04_Gameplay/`** - 游戏逻辑层：处理运行时游戏行为（角色FSM、AI决策、战斗计算）
5. **`05_Show/`** - 表现层：纯粹的视听反馈（UI响应、动画状态、特效播放）

**扩展层**：
6. **`06_Extensions/`** - 扩展与MOD支持层：MOD系统和编辑器工具
7. **`07_Shared/`** - 全局共享层：常量和扩展方法

**依赖流向规则**：
- 低编号层可依赖高编号层，反之禁止（例如：`02_Base` 可使用 `07_Shared`，但 `07_Shared` 不能使用 `02_Base`）
- 业务层→表现层：严禁直接调用，必须通过 EventBus.Publish 广播
- 表现层→业务层：通过 ServiceLocator 获取服务或发布 UIEvents
- 跨业务层通信：通过 EventBus

### 核心基础设施系统

1. **EventBus** (`02_Base/EventBus/`) - 全局类型安全事件总线，基于泛型字典实现。使用结构体事件避免GC分配。
   - 所有事件定义为结构体（实现 `IEvent` 接口）
   - 事件分类：`InventoryEvents.cs`、`CombatEvents.cs`、`SurvivalEvents.cs` 等
   - 跨层通信核心机制

2. **ServiceLocator** (`02_Base/ServiceLocater/ServiceLocator.cs`) - 轻量级服务定位器，替代全局单例泛滥。
   - 核心系统在 `Awake()` 中注册自身：`ServiceLocator.Register<SurvivalStatusSystem>(this)`
   - 其他系统通过 `ServiceLocator.Get<T>()` 访问
   - 比单例更利于测试，可注入Mock实现

3. **StateMachine** (`02_Base/StateMachine/`) - 通用有限状态机框架。
   - 玩家FSM、敌人AI FSM、全局游戏状态均复用此框架
   - 定义 `IState` 接口，`StateMachine<TStateKey>` 泛型类

4. **MonoSingleton** (`02_Base/Singleton/MonoSingleton.cs`) - MonoBehaviour单例基类。
   - **仅用于**基础设施层管理器（AudioManager、VFXManager等）
   - **业务逻辑系统**优先使用 ServiceLocator

5. **TimerSystem** (`02_Base/Timer/`) - 对象池驱动计时器系统。
   - 零GC分配，支持全局/单个暂停
   - 支持时间缩放（配合昼夜/睡眠系统）
   - 支持单次/循环/有限次循环
   - 通过 TimerHandle 安全控制，句柄失效自动无效化

### 核心业务系统

1. **SurvivalStatusSystem** (`03_Core/SurvivalStatus/`) - 生存属性管理中枢。
   - 统一管理所有生存属性（血量/饥饿/口渴/体温/疾病）
   - 支持属性衰减、状态效果的挂载与Tick
   - 属性归零的后果触发
   - 依赖：EventBus、SurvivalConfigSO、IStatusEffect

2. **IStatusEffect 接口** (`03_Core/SurvivalStatus/IStatusEffect.cs`) - 状态效果接口。
   - 所有临时状态（中毒、寒冷、饥饿加速等）均实现此接口
   - 扩展性设计：新增"生病"状态只需新建一个实现类，无需修改核心系统

3. **ISaveable 接口** (`03_Core/Save/ISaveable.cs`) - 可存档接口。
   - 需要持久化的系统均实现此接口
   - `SaveLoadSystem` 在存档时遍历所有注册的 `ISaveable`
   - `SaveKey`：唯一存档ID，建议用 `nameof(类名)`

### 数据层

1. **ItemDefinitionSO 基类** (`01_Data/ScriptableObjects/Items/_Base/ItemDefinitionSO.cs`) - 所有物品的数据定义基类。
   - 纯数据，零运行时逻辑
   - 数据驱动设计核心：新增物品只需创建.asset文件，无需改代码
   - 扩展点：`OnUse()` 和 `CanUse()` 方法供子类重写

2. **SurvivalConfigSO** - 生存配置ScriptableObject（饥饿速率等）
3. **SaveData 结构体** - 存档相关数据结构（纯C#类，无MonoBehaviour）

## 数据驱动扩展点

| 扩展需求 | 操作方式 | 需修改的文件 |
|----------|----------|-------------|
| 新增物品 | 创建新的 `.asset` 文件 | **无需改代码** |
| 新增制作配方 | 创建 `RecipeDefinitionSO.asset` | **无需改代码** |
| 新增生存状态属性 | 实现 `IStatusEffect` 接口 | 仅新增1个类 |
| 新增敌人类型 | 继承 `EnemyBase`，创建EnemyDefinitionSO | 仅新增1个类+1个asset |
| 新增玩家状态 | 继承 `IState`，注册到PlayerFSM | 仅新增1个类 |
| 新增UI界面 | 继承 `UIPanel`，创建Prefab | 仅新增1个类 |
| MOD支持 | 实现 `IModEntry` + `IModDataProvider` | 仅新增MOD程序集 |

## 开发工作流

### 打开项目
- 使用 Visual Studio/Rider 打开 `doom2d.sln` 进行代码编辑
- 使用 Unity 2022.3.62f3 或兼容版本打开Unity项目

### 包管理
- 包通过 `Packages/manifest.json` 管理
- 仅使用Unity Registry包 - 未配置外部包源

### 测试
- 依赖中包含 Unity Test Framework (`com.unity.test-framework`)
- 项目结构中未找到自定义测试程序集

### 代码风格与约定
- 文档注释使用中文
- C#文件包含详细头部，标注文件路径和用途描述
- 性能优化在注释中标注（例如"[PERF]"标记）
- 性能关键系统使用对象池（TimerSystem）
- 所有事件定义为结构体，避免GC分配

## 关键设计原则

### 模块通信示例：玩家捡起苹果
```
[物理层] 玩家碰撞体 OnTriggerEnter2D → 检测到 WorldItem (苹果Prefab)
    │
    ▼
[游戏逻辑层] PlayerController.cs
    → 调用 InteractionSystem.TryInteract(worldItem)
    → InteractionSystem 检查 worldItem 实现了 IInteractable
    → 调用 worldItem.Interact(player)
    │
    ▼
[游戏逻辑层] WorldItem.cs (苹果的世界实体)
    → 通过 ServiceLocator.Get<InventorySystem>() 获取背包系统
    → 调用 inventorySystem.TryAddItem("item_apple", 1)
    │
    ▼
[核心业务层] InventorySystem.cs
    → 查找空闲槽位，执行背包逻辑
    → 成功后发布事件：
      EventBus.Publish(new ItemAddedToInventoryEvent {
          ItemId = "item_apple", Amount = 1, SlotIndex = 3
      })
    │
    ├──────────────────────────────────────────────┐
    ▼                                              ▼
[表现层] InventoryPresenter.cs               [表现层] VFXManager.cs
    → 订阅 ItemAddedToInventoryEvent          → 订阅同一事件
    → 更新 InventoryPanel 对应槽位的          → 播放拾取特效 + 音效
      图标和数量显示
    │
    ▼
[表现层] HUDPresenter.cs（可选）
    → 显示 "+苹果 x1" 的提示文本（飘字）
```

### 关键通信规则
| 规则 | 说明 |
|------|------|
| **逻辑层→业务层** | 直接调用（通过ServiceLocator解耦实例获取） |
| **业务层→表现层** | 严禁直接调用，必须通过 EventBus.Publish 广播 |
| **表现层→业务层** | 通过 ServiceLocator 获取服务，或发布 UIEvents |
| **跨业务层通信** | 通过 EventBus（如背包变化→制作系统重新验证可用配方） |

## 重要注意事项

1. **服务注册**：核心系统在 `Awake()` 方法中向ServiceLocator注册自身（参见 `SurvivalStatusSystem.Awake()` 示例）。

2. **计时器使用**：使用 `TimerSystem.Instance.Create()` 而不是 Unity 的 `Invoke`，以获得更好的控制和性能。

3. **事件通信**：使用 EventBus 进行解耦的系统通信，而不是直接引用。

4. **无程序集定义**：项目不使用 .asmdef 文件进行程序集分离 - 所有脚本都在默认程序集中。

5. **第三方代码**：`ThirdPart/` 目录包含外部工具（claude-code-proxy），不是游戏代码。

6. **无CI/CD配置**：未找到构建脚本、CI文件或自动部署配置。

7. **编辑器工具**：检查 `06_Extensions/Editor/` 获取自定义编辑器验证工具（例如 `ItemAssetValidator.cs`）。

8. **MOD支持**：架构设计支持MOD系统，通过 `IModEntry` 和 `IModDataProvider` 接口实现。

## 多Agent调度器角色

你现在是 Unity 前端项目的多Agent调度器。

### 项目目标
《根与废土》(Roots & Ruin) 是一款2D横版生存建造探索游戏，采用五层菱形分层架构。项目旨在构建一个可维护、可扩展、高性能的游戏前端系统，支持多Agent协同开发。

### 当前需求
[在每个会话开始时，用户会在此处填写当前要做的功能]

### 可用 Agent 映射
项目支持以下10个专业Agent，对应Claude Code的Agent工具：

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
1. **五层菱形架构**：严格遵循01_Data→02_Base→03_Core→04_Gameplay→05_Show的依赖流向
2. **跨层通信**：业务层→表现层必须通过EventBus，表现层→业务层通过ServiceLocator
3. **数据驱动**：优先使用ScriptableObjects进行配置
4. **性能要求**：避免GC分配，使用对象池，事件使用结构体

### Agent启动指令
当确定需要某个Agent时，使用以下格式启动：
```bash
Agent(description="简短描述", prompt="详细任务说明", subagent_type="对应的Claude Code Agent类型")
```

### 示例调度
**需求**："添加一个生存状态显示UI，实时显示玩家的血量、饥饿、口渴值"

**调度器输出**：
```
## 需求分析
需要创建一个HUD界面，实时显示玩家的生存状态...
## 参与 Agent
1. Product/UX Agent - 设计状态显示UI的布局和交互
2. Data/ViewModel Agent - 设计生存状态数据模型
3. UI Implementation Agent - 实现UI组件
4. Animation/Effects Agent - 添加状态变化的动画反馈
...
```

### 重要原则
1. **先理解后执行**：确保完全理解需求再开始调度
2. **职责边界清晰**：明确每个Agent的工作范围
3. **变更影响说明**：如需超出范围的改动，先输出变更影响说明
4. **小步渐进**：避免一次性调度过多Agent，优先核心路径
5. **冲突检查**：发现Agent间冲突立即协调解决

---
**注意**：作为调度器，你的首要职责是合理分配任务、协调Agent工作、确保架构一致性，而不是直接实现代码。