# 项目记忆：根与废土 (Roots & Ruin)

## 项目概况

- **类型**：Unity 2D 横版生存建造探索游戏
- **引擎**：Unity 2022.3.62f3
- **UI**：UGUI + TextMeshPro
- **程序集**：无 `.asmdef`，全部在 `Assembly-CSharp`
- **资源加载**：`Resources.Load` + 自定义 `ResourceManager`（支持 AssetBundle）
- **无 CI/CD**，无 Addressables

---

## 架构：五层菱形分层

```
Assets/_Game/Scripts/
├── 01_Data/        纯数据（ScriptableObjects、存档结构体）
├── 02_Base/        引擎无关核心（EventBus、ServiceLocator、StateMachine、Timer、ResourceManager）
├── 03_Core/        业务规则（背包、生存、制作、装备、交易、任务等）
├── 04_Gameplay/    运行时行为（角色FSM、敌人AI、战斗、地图、世界）
├── 05_Show/        表现层（UI 面板，MVP 模式）
├── 06_Extensions/  MOD 系统 + 编辑器工具
└── 07_Shared/      全局枚举、常量、扩展方法
```

**依赖规则**：低层可依赖高层，反之禁止。业务→表现必须走 EventBus。

---

## 核心基础设施

### EventBus
- 事件必须为 `struct` + 实现 `IEvent`
- 业务事件定义在 `02_Base/EventBus/Events/`
- UI 内部事件定义在 `05_Show/.../Events/`
- `OnDestroy` 必须调用 `Unsubscribe`，场景卸载调用 `EventBus.Clear()`

### ServiceLocator
- `Awake` 中注册（同时注册具体类和接口）
- `OnDestroy` 中必须注销
- `InventorySystem.Start()` 中获取 `IItemDataService`（时序原因）

### TimerSystem
- `MonoSingleton<TimerSystem>`，同时注册到 ServiceLocator
- 零 GC，用 `TimerHandle` 取消
- 优先使用此系统而非 Unity `Invoke`

---

## 05_Show 层 MVP 模式

```
Presenter（MonoBehaviour）→ ViewModel（纯C#）→ View（纯显示）
```

- Presenter 订阅 EventBus，通过 ServiceLocator 调用业务层
- Presenter 不直接调用 View 方法，UI 反馈通过 `UIFeedbackEvent`
- ViewModel 暴露 `event Action<T>` 给 View 订阅
- 参考实现：`05_Show/Inventory/`

---

## 已实现系统清单

### 03_Core（业务层）
- `InventorySystem` + 背包扩展（`Expansion/`）
- `SurvivalStatusSystem`（死亡优先级：脱水>饥饿>低温>高温>战斗）
- `CraftingSystem` + `CraftingValidator`
- `EquipmentSystem`
- `TradingSystem`
- `QuestSystem`
- `CurrencySystem`
- `BuildingSystem`
- `SaveLoadSystem`
- `DifficultySystem`
- `InteractionSystem`
- `SpawnManager`

### 04_Gameplay（运行时）
- `PlayerController` + `PlayerStateMachine`（8个状态）
- `EnemyBase` + `EnemyStateMachine`（6个状态）
- `CombatSystem` + `DamageCalculator`
- `MapManager` + `DiggingSystem`
- `GameTimeSystem` + `DayNightCycle` + `WeatherSystem` + `TemperatureSystem`

### 05_Show（UI）
- 背包 UI（最完整的参考实现）
- HUD（生存状态条）
- 制作面板、交易面板、建造面板、任务日志
- 通知系统、对话系统、游戏结算、加载界面
- 暂停菜单、设置面板、主菜单

---

## 代码约定

- 注释和文档使用**中文**
- 文件头：`// 📁 路径/文件名.cs` + 中文说明
- 性能关键代码标注 `// [PERF]`
- `MonoSingleton` 仅用于基础设施管理器
- 所有全局枚举统一写在 `07_Shared/Constant/Enums.cs`

---

## 待办 / 未实现

- `04_Gameplay/` 最初为空，现已有基本实现但部分功能待补全
- `06_Extensions/Mod/` 接口已定义，加载器已有，但无实际 MOD
- 无测试文件，无 CI/CD
- `.claude/memory.md` ← 刚刚创建（2026-04-03）

---

## 多 Agent 调度

项目支持多 Agent 协同开发，可用 Agent 映射见 `CLAUDE.md`。
