# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

Unity 2D项目《根与废土》(Roots & Ruin) — 2D横版生存建造探索游戏（Unity 2022.3.62f3）。采用五层菱形分层架构，通过数字前缀目录强制依赖流向。

## 当前分支状态

⚠️ **当前分支 `skeleton/infrastructure-only` 是骨架版本**，仅保留基础设施层和少量 UI 面板。以下系统已从 `main` 分支剥离，待重新实现：

**已移除的业务系统**（原 `03_Core/`）：InventorySystem、EquipmentSystem、CraftingSystem、BuildingSystem、TradingSystem、QuestSystem、CurrencySystem、DifficultySystem、SurvivalStatusSystem、SkillSystem、AchievementSystem、DiscoverySystem、NPCRelationshipSystem、InteractionSystem、CombatSystem、SpawnManager、TutorialSystem

**已移除的 UI 面板**（原 `05_Show/`）：背包、制作、建造、HUD、任务、交易、对话、死亡结算、成就、地图、技能、教程面板

**已移除的 ScriptableObject**（原 `01_Data/ScriptableObjects/`）：所有 SO 类型（ItemDefinitionSO、RecipeDefinitionSO、BuildingDefinitionSO 等），骨架版本改用 JSON 数据文件

**当前各层实际内容**：

| 层 | 实际内容 |
|----|---------|
| `01_Data/` | Inventory 数据结构（Container/Slot/Stack）、JsonData 数据类（Audio/Configs/Inventory/Items）、SaveData 框架 |
| `02_Base/` | **完整** — EventBus、ServiceLocator、StateMachine、Timer、ResourceManager、ObjectPool、CommandSystem、GameState、Audio |
| `03_Core/` | DataServices（DataLoaderSystem + JsonDataServiceBase + 2个数据服务）、Save（SaveLoadSystem + SaveSerializer + ISaveable） |
| `04_Gameplay/` | AppMain、CameraController |
| `05_Show/` | Loading、MainMenu、Notification、PauseMenu、Settings、UI/_Base（UIManager/UIPanel）、VFX |
| `06_Extensions/` | Editor（ExcelToJsonConverter）、Mod（IModEntry、ModLoader） |
| `07_Shared/` | Enums.cs、GameConst.cs、Extensions、Utils |

> **目标架构**：完整版游戏包含 ~30+ 业务系统和 ~20 个 UI 面板，详见 `docs/` 目录下的设计文档。重新实现系统时参考本文档的"完整版架构参考"部分。

## 开发环境

- **Unity版本**：2022.3.62f3
- **UI框架**：UGUI（`com.unity.ugui`），TextMeshPro
- **无 `.asmdef`**：所有脚本在默认程序集 `Assembly-CSharp`
- **无 Addressables**：资源加载使用 `Resources.Load` + 自定义 `ResourceManager`（支持 AssetBundle）
- **测试**：`com.unity.test-framework 1.1.33`，通过 Unity Test Runner 执行（目前无测试文件）
- **包管理**：`Packages/manifest.json`，Unity Registry 包 + `com.coplaydev.unity-mcp`（AI工具链）
- **无 CI/CD 配置**
- **场景**：目前仅 `Assets/Scenes/SampleScene.unity` 一个场景
- **数据资源路径**：骨架版本使用 JSON 文件存放在 `Assets/Resources/` 目录下（通过 `Resources.Load<TextAsset>` 加载），完整版使用 ScriptableObject 存放在 `Assets/GameData/`

## 架构与代码结构

### 五层菱形分层架构

`Assets/_Game/Scripts/` 下各层：

| 层 | 职责 |
|----|------|
| `01_Data/` | 纯数据定义（数据结构、JSON数据类、存档结构体） |
| `02_Base/` | 引擎无关核心机制（EventBus、ServiceLocator、StateMachine、Timer、ResourceManager、ObjectPool、CommandSystem、GameState、Audio） |
| `03_Core/` | 业务规则（数据服务、存档系统） |
| `04_Gameplay/` | 运行时游戏行为（AppMain入口、Camera） |
| `05_Show/` | 表现层（UI面板、VFX） |
| `06_Extensions/` | 编辑器工具、Mod加载框架 |
| `07_Shared/` | 枚举常量（`Enums.cs`）、全局常量（`GameConst.cs`）、扩展方法、工具类 |

**依赖流向规则**：
- 高编号层可依赖低编号层，反之禁止（`05_Show → 04_Gameplay → 03_Core → 02_Base → 01_Data`）
- **业务层→表现层**：严禁直接调用，必须通过 `EventBus.Publish`
- **表现层→业务层**：通过 `ServiceLocator.Get<T>()` 或发布 UI事件
- **跨业务层通信**：通过 EventBus

### JSON 数据加载系统（骨架版本）

骨架版本用 **JSON 文件** 替代 ScriptableObject 作为数据源。这是骨架版本引入的核心架构模式。

**数据流**：
```
JSON 文件 (Assets/Resources/)
  → JsonDataServiceBase<T>.LoadAsync()
    → Resources.Load<TextAsset>(jsonPath)
      → JsonUtility.FromJson<T>(json.text)
        → ServiceLocator 注册接口
          → 业务系统通过 ServiceLocator.Get<TInterface>() 查询
```

**关键组件**：

1. **JsonData 数据类**（`01_Data/JsonData/<Domain>/`） — 纯 C# 数据类，`[System.Serializable]`，对应 JSON 结构
2. **IDataServices 接口**（`02_Base/Interfaces/IDataServices.cs`） — **所有数据服务接口集中定义在此文件**（类似 Enums.cs 的集中管理策略）
3. **JsonDataServiceBase**（`03_Core/DataServices/JsonDataServiceBase.cs`） — 非泛型抽象基类，提供 `LoadAsync()` 协程签名和 `DataFileName` 属性。Unity 无法直接 AddComponent 泛型 MonoBehaviour，因此抽出此基类
4. **DataLoaderSystem**（`03_Core/DataServices/DataLoaderSystem.cs`） — 数据加载协调器，AppMain 首先创建它，按顺序加载所有 JSON 数据。加载完成后才创建业务系统

**当前已有的数据服务**：
- `ResourceCacheConfigDataService` → `IResourceCacheConfigDataService`
- `AudioCatalogDataService` → `IAudioCatalogDataService`

**新增数据服务步骤**：
1. 在 `01_Data/JsonData/<Domain>/` 创建 `XxxData.cs` 数据类
2. 在 `02_Base/Interfaces/IDataServices.cs` 添加接口
3. 在 `03_Core/DataServices/` 创建 `XxxDataService : JsonDataServiceBase` 实现类
4. 在 `DataLoaderSystem.CreateAllServices()` 中注册
5. 在 `AppMain.ValidateAllSystems()` 中添加验证

### 核心基础设施系统（02_Base）

#### EventBus（`02_Base/EventBus/IEvent.cs`）

```csharp
EventBus.Subscribe<MyEvent>(handler);   // 订阅
EventBus.Publish(new MyEvent { ... });  // 发布（struct，零GC）
EventBus.Unsubscribe<MyEvent>(handler); // OnDestroy 中必须调用
EventBus.Clear();                       // 场景切换时调用
```

**事件定义位置**：
- 业务事件：`02_Base/EventBus/Events/`（当前有 `GameStateEvents.cs`、`LoadingEvents.cs`、`NotificationEvents.cs`、`ResourceEvents.cs`、`SaveEvents.cs`）
- UI交互事件：`05_Show/UI/Events/`（`UIManagerEvents.cs`，仅表现层内部使用）

所有事件必须为 `struct` 并实现 `IEvent` 接口。

#### ServiceLocator（`02_Base/ServiceLocater/ServiceLocator.cs`）

```csharp
// 注册（Awake 中，同时注册具体类和接口）
ServiceLocator.Register<SomeSystem>(this);
ServiceLocator.Register<ISomeInterface>(this);

// 使用
var sys = ServiceLocator.Get<ISomeInterface>();

// 安全获取（不抛异常，用于可选依赖）
if (ServiceLocator.TryGet<ISomeInterface>(out var sys2)) { /* ... */ }

// 注销（OnDestroy 中必须调用）
ServiceLocator.Unregister<SomeSystem>();
ServiceLocator.Unregister<ISomeInterface>();
```

#### 其他基础设施

- **TimerSystem**（`MonoSingleton`）：零GC定时器，通过 `TimerHandle` 安全取消。优先使用此系统而非 Unity 的 `Invoke`
  ```csharp
  // 延迟调用（单次）
  TimerHandle handle = TimerSystem.Instance.Delay(2f, () => { /* ... */ });
  // 循环调用
  TimerHandle loop = TimerSystem.Instance.Loop(1f, () => { /* 每秒执行 */ });
  // 安全取消（句柄可能已失效）
  handle.Cancel();
  // OnDestroy 中取消所有本对象关联的定时器
  TimerSystem.Instance.CancelAllFor(this);
  ```
- **ResourceManager**（`MonoSingleton`）：同步/异步加载、AssetBundle、LRU缓存、3次重试
- **StateMachine**：泛型 `StateMachine<TStateKey> where TStateKey : Enum`
- **ObjectPoolManager**（`MonoSingleton`）：`ObjectPoolManager.Get<T>(prefab)` / `Release(go)`，实现 `IPoolable` 获取回调
- **CommandSystem**：`ICommand`（`Execute`/`Undo`），`CommandInvoker` 由 `AppMain` 注册到 ServiceLocator
- **GameStateManager**（`MonoSingleton`）：`Initializing → MainMenu → Loading → GamePlay → Paused → GameOver`
- **AudioManager**（`MonoSingleton`）：音频组（Master/Music/SFX/Ambient/UI/Voice），内部AudioSource池
- **VFXManager**（`MonoSingleton`，位于 `05_Show/VFX/VFXManager.cs`）：视觉特效管理，通过 `VFXEntry[]` 配置
- **当前接口**（`02_Base/Interfaces/`）：`ISystem`、`IDataServices`（`IResourceCacheConfigDataService`、`IAudioCatalogDataService`）

#### AppMain（`04_Gameplay/AppMain.cs`）

场景唯一入口点，Script Execution Order 设为 `-100`。**场景中仅需此脚本**，所有后端系统通过代码创建。

**核心职责**：
- 在 `Awake()` 中创建无数据依赖的 MonoSingletons
- 通过协程 `BootstrapCoroutine()` 分阶段加载数据和创建系统
- 在 `OnDestroy()` 中逆序调用 `ISystem.Shutdown()`

**初始化流程（骨架版本 — 协程分阶段）**：
```
Awake:
  Phase 1: 创建 MonoSingletons（无数据依赖）
    GameStateManager → TimerSystem → ObjectPoolManager
    → ResourceManager → AudioManager → UIManager → VFXManager

StartCoroutine(BootstrapCoroutine):
  Phase 2: 创建 DataLoaderSystem → yield return LoadAllDataAsync()
  Phase 3: 创建 Core 业务系统（目前仅 SaveLoadSystem）
  Phase 4: ValidateAllSystems() → Publish GameStateChangedEvent(GamePlay)
```

**关键辅助方法**：
- `CreateMonoSingleton<T>(name, configure)` — 创建单例管理器，挂载到 `_Singletons` 子节点
- `CreateSystem<T>(name, configure)` — 创建普通系统 MonoBehaviour，挂载到 `_Systems` 子节点
- 所有实现 `ISystem` 的系统自动加入 `_allSystems` 列表，OnDestroy 时逆序 Shutdown

**ISystem 接口**（`02_Base/Interfaces/ISystem.cs`）：
```csharp
public interface ISystem
{
    void Initialize();  // 配置注入后调用（替代 Awake 中的配置依赖逻辑）
    void Shutdown();    // 清理：ServiceLocator.Unregister、EventBus.Unsubscribe
}
```

**两阶段初始化模式**：
- `Awake()`：仅做 ServiceLocator 注册（无配置依赖）
- `Initialize()`：配置注入后的完整初始化（AppMain 设置配置后调用）
- `Shutdown()`：清理（替代 OnDestroy 逻辑）

**新增系统时**：在 AppMain 的 `CreateCoreSystems()` 中添加 `CreateSystem<T>("Name")` 调用，并在 `ValidateAllSystems()` 中添加验证。

### 05_Show 层的 MVP 模式

```
Presenter（MonoBehaviour）→ ViewModel（纯C#类）→ View（纯显示组件）
```

- **Presenter**：订阅 EventBus → 更新 ViewModel；处理用户交互 → 调用 ServiceLocator 获取业务系统。**不直接调用 View 方法**
- **ViewModel**：纯C#，持有UI状态，暴露 `event Action<T>` 给View订阅
- **View**：仅负责渲染，监听 ViewModel 的事件回调
- **UIManager**（`MonoSingleton`，`05_Show/UI/_Base/`）：栈式面板管理，所有面板继承 `UIPanel`（需 `CanvasGroup`），HUD面板单独管理

**UI面板注册流程**：`UIPanel` 在 `Awake` 中默认隐藏。各面板通过 `UIManager.RegisterPanel(this)` 注册自身，然后由 `UIManager.OpenPanel(id)` / `ClosePanel(id)` / `TogglePanel(id)` 控制。全屏面板自动暂停游戏（通过 `_pauseGameOnOpen` 配置）。HUD 面板通过 `RegisterHUD` 注册，不入栈，常驻显示。

**MonoSingleton 说明**：继承 `MonoSingleton<T>` 的类会在 `Awake` 中自动 `DontDestroyOnLoad`，场景切换时不会销毁。

**当前已有 UI 面板**：LoadingPanel、MainMenu、NotificationPanel、PauseMenu、SettingsPanel

### Mod 系统（`06_Extensions/Mod/`）

骨架版本已包含 Mod 加载框架：
- `IModEntry`（`06_Extensions/Mod/IModEntry.cs`） — Mod 入口接口
- `ModLoader`（`06_Extensions/Mod/ModLoader.cs`） — Mod 加载器

### 存档系统

- **SaveLoadSystem**（`03_Core/Save/SaveLoadSystem.cs`） — `MonoBehaviour + ISystem`，通过 `ServiceLocator` 获取。管理 `ISaveable` 注册表，支持多槽位（`slotIndex`），文件路径 `Application.persistentDataPath/save_{slotIndex}.json`
- **ISaveable**（`03_Core/Save/ISaveable.cs`） — 业务系统实现此接口，提供 `SaveKey`、`CaptureState()`、`RestoreState(string json)`
- **SaveSerializer**（`03_Core/Save/SaveSerializer.cs`） — 基于 `JsonUtility` 的序列化器（不支持 `Dictionary`）
- **存档事件**：`SaveStartedEvent`、`SaveCompletedEvent`、`LoadStartedEvent`、`LoadCompletedEvent`

### 全局枚举与常量（`07_Shared/Constant/`）

**`Enums.cs`** — 所有全局枚举统一在此文件追加，不新建枚举文件。

**`GameConst.cs`** — 全局常量，使用此类而非硬编码数字/字符串。

## 代码约定

- 文档注释和注释使用**中文**
- C#文件头部格式：`// 📁 路径/文件名.cs` + 中文说明
- 性能关键代码添加 `// [PERF]` 标注
- `MonoSingleton` 仅用于基础设施管理器，业务系统使用 ServiceLocator

### 命名规范

- 私有字段：`_camelCase`
- 常量：`ALL_CAPS`
- 系统类：`NounSystem`，接口：`INoun`
- 事件结构体：`NounEvent`
- 数据服务类：`XxxDataService`，接口：`IXxxDataService`
- JSON数据类：`XxxData`（`[System.Serializable]`）

### 禁止的模式

- `Invoke`/`InvokeRepeating` — 使用 `TimerSystem`
- `WaitForSeconds` 做延迟 — 使用 `TimerSystem`
- `GetComponent` 在 `Update` 中调用 — 在 `Awake` 中缓存
- `Resources.Load` 在热路径中 — 使用 `ResourceManager`
- 业务层内部用 `event Action<T>` 做观察者 — 使用 `EventBus`

## 实现注意事项

### 多类文件

- `IEvent.cs` → 包含 `IEvent` 接口 + `EventBus` 静态类
- `IState.cs` → 包含 `IState` 接口 + `StateMachine<TStateKey>` 泛型类

### 服务注册时序

- 核心系统在 `Awake()` 中注册到 ServiceLocator
- `Start()` 中获取其他服务的引用（因为 `Start()` 在所有 `Awake()` 之后执行）
- `OnDestroy()` 中必须注销所有注册，防止悬空引用

### 订阅泄漏防护

`OnDestroy` 中务必调用 `EventBus.Unsubscribe`；场景卸载时调用 `EventBus.Clear()`。

## 扩展点

| 扩展需求 | 操作方式 | 需修改的文件 |
|----------|----------|-------------|
| 新增 JSON 数据 | 创建 `XxxData.cs` + 对应 DataService | 4个文件（数据类、接口、DataService、DataLoaderSystem注册） |
| 新增业务系统 | 实现 `ISystem`，在 AppMain 中 CreateSystem | 2个文件（系统类、AppMain） |
| 新增 UI 面板 | 继承 `UIPanel`，实现 Presenter/ViewModel/View | 3-4个文件 |
| 新增枚举值 | 追加到 `07_Shared/Constant/Enums.cs` | 只改此一个文件 |
| 新增 Mod | 实现 `IModEntry` 接口 | 仅新增1个文件 |

## 设计文档

`docs/` 目录包含核心设计文档（中文）：
- `docs/程序/Unity 底层程序架构设计方案 v1.0.md` — 完整架构规格
- `docs/程序/代码编写规范.md` + `代码编写规范_续.md` — 编码标准
- `docs/策划/完整游戏设计文档.md` — 完整游戏设计文档
- `docs/策划/Phase 3 核心机制与数值框架.md` — 生存属性数值、难度乘数、可调参数
- `docs/策划/Phase 4 叙事与内容填充.md` — 叙事章节和触发条件
- `docs/策划/Phase 5 系统详细策划案（程序交付版）.md` — Phase 5 全量系统策划案

## 重要提醒

1. **`ThirdPart/` 目录**（claude-code-proxy）不是游戏代码，不要修改。
2. **使用 `GameConst` 中的常量**，不要硬编码数字和字符串。
3. **新增数据服务接口**统一在 `02_Base/Interfaces/IDataServices.cs` 中定义，不要分散到各文件。
4. **新增枚举**统一追加到 `07_Shared/Constant/Enums.cs`，不新建枚举文件。

## 完整版架构参考

以下内容描述 `main` 分支的完整游戏架构，作为重新实现系统时的目标参考。

### 完整版 Core 系统（`03_Core/`）

SaveLoadSystem → ItemDataService → DifficultySystem → CurrencySystem → InventorySystem → EquipmentSystem → CraftingSystem → BuildingSystem → TradingSystem → QuestSystem → InteractionSystem → SkillSystem → AchievementSystem → DiscoverySystem → NPCRelationshipSystem → SurvivalStatusSystem → ExpansionServices

### 完整版 Gameplay 系统（`04_Gameplay/`）

CombatSystem → SpawnManager → TutorialSystem → CommandInvoker（此外还有角色FSM、敌人AI、战斗、地图、昼夜/天气、NPC）

### 完整版 05_Show 面板

背包、制作、建造、HUD、任务、交易、设置、对话、通知、加载、主菜单、暂停、死亡结算、成就、地图、技能、教程

### 完整版 ScriptableObject 类型（`01_Data/ScriptableObjects/`）

- **物品**（`Items/_Base/ItemDefinitionSO`—抽象基类）：子类 `ArmorItemSO`、`MaterialItemSO`、`ToolItemSO`、`WeaponItemSO`、`ConsumableItemSO`
- **其他SO**：Building、Crafting（RecipeDefinitionSO）、Quest、Trading、Achievement、Skill、NPC、Spawning、Difficulty、Dialog、Discovery、Audio、Configs、Enemies、Map

### 完整版命名空间

| 层级 | 命名空间 |
|------|---------|
| 01_Data | `SurvivalGame.Data.Inventory`、`SurvivalGame.Data.Inventory.Expansion` |
| 03_Core | `SurvivalGame.Core.Inventory`、`SurvivalGame.Core.Inventory.Expansion` |
| 05_Show | 通常无命名空间（全局），或 `SurvivalGame.Show.Inventory` |
| 07_Shared | 无命名空间（全局枚举） |

### 完整版关键数据结构

- **`InventoryContainer`**：`struct`（值类型），传递时注意拷贝语义
- **`InventorySlot`**、**`ItemStack`**：背包槽位和堆叠数据
- **`SlotType`**：定义在 `01_Data/Inventory/InventorySlot.cs` 内部，不在 `Enums.cs` 中
- **`ItemCategory`/`ItemRarity`**：定义在 `ItemDefinitionSO.cs` 中的局部枚举，与 `07_Shared` 的 `ItemType`/`ItemQuality` 不同
- **存档结构**（`01_Data/SaveData/`）：纯数据 struct，使用 `JsonUtility` 序列化（不支持 `Dictionary`）

## 多Agent调度器角色

本项目支持多Agent协同开发。作为调度器，首要职责是合理分配任务、协调Agent工作、确保架构一致性。

### 可用 Agent 映射

| 调度器名称 | Claude Code Agent类型 | 职责 |
|------------|---------------------|------|
| Product/UX Agent | `unity-ui-spec-writer` | 将模糊需求转化为详细UI/UX规格 |
| UI Architect Agent | `unity-ui-architect` | 设计五层架构中的表现层 |
| UI Implementation Agent | `unity-ui-implementer` | 基于规格实现Unity UI代码 |
| Data/ViewModel Agent | `ui-data-modeler` | 设计UI数据模型 |
| Backend Integration Agent | `backend-integration-agent` | Unity与后端服务集成 |
| Asset Integration Agent | `ui-art-integration-agent` | UI美术资源集成 |
| Animation/Effects Agent | `unity-animation-effects-advisor` | UI动画与交互反馈 |
| Performance Agent | `unity-ui-performance-agent` | UI性能分析与优化 |
| QA/Test Agent | `unity-frontend-qa-agent` | 测试用例设计 |
| DevOps/Orchestrator | `unity-dev-orchestrator` | 复杂任务分解与协调 |

### 调度工作流

1. 分析需求 → 2. 选择Agent → 3. 拆分子任务（标明串行/并行） → 4. 分配文件边界 → 5. 确定执行顺序 → 6. 识别风险与验收标准

### 架构约束

- 严格遵循五层依赖流向
- 业务层→表现层通过 EventBus，表现层→业务层通过 ServiceLocator
- 避免GC分配，事件使用 struct
- 05_Show 遵循 Presenter → ViewModel → View 单向数据流
