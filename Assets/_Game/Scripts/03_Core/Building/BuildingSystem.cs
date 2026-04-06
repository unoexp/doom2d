// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/03_Core/Building/BuildingSystem.cs
// 建造系统核心。管理建筑注册、条件验证、建造执行、庇护所阶段。
// ══════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 中央建造管理系统。
///
/// 核心职责：
///   · 管理所有建筑定义和解锁状态
///   · 验证建造条件（材料、前置建筑、解锁状态）
///   · 执行建造流程（消耗材料、实例化建筑）
///   · 跟踪已建造的庇护所模块，计算庇护所阶段
///   · 通过 EventBus 广播建造事件
///
/// 设计说明：
///   · 建筑定义通过 Inspector 中 BuildingDefinitionSO 数组配置
///   · 建造请求通过 EventBus（UI 发起）或直接 API 调用
///   · 庇护所阶段根据已建造的功能模块数量自动计算
/// </summary>
public class BuildingSystem : MonoBehaviour, ISaveable, IBuildingSystem
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("建筑数据")]
    [SerializeField] private BuildingDefinitionSO[] _buildingDefinitions;

    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    /// <summary>BuildingId → 定义（快速查找）</summary>
    private readonly Dictionary<string, BuildingDefinitionSO> _definitionMap
        = new Dictionary<string, BuildingDefinitionSO>();

    /// <summary>已解锁的建筑ID</summary>
    private readonly HashSet<string> _unlockedBuildings = new HashSet<string>();

    /// <summary>已建造的建筑ID</summary>
    private readonly HashSet<string> _builtBuildings = new HashSet<string>();

    /// <summary>当前庇护所阶段（1~6）</summary>
    private int _shelterStage = 0;

    private IInventorySystem _inventorySystem;

    // ══════════════════════════════════════════════════════
    // ISaveable
    // ══════════════════════════════════════════════════════

    public string SaveKey => nameof(BuildingSystem);

    // ══════════════════════════════════════════════════════
    // 属性
    // ══════════════════════════════════════════════════════

    public int ShelterStage => _shelterStage;
    public int BuiltCount => _builtBuildings.Count;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        ServiceLocator.Register<BuildingSystem>(this);
        ServiceLocator.Register<IBuildingSystem>(this);

        // 构建查找表
        if (_buildingDefinitions != null)
        {
            for (int i = 0; i < _buildingDefinitions.Length; i++)
            {
                var def = _buildingDefinitions[i];
                if (def == null || string.IsNullOrEmpty(def.BuildingId)) continue;
                _definitionMap[def.BuildingId] = def;

                if (def.UnlockedByDefault)
                    _unlockedBuildings.Add(def.BuildingId);
            }
        }
    }

    private void Start()
    {
        _inventorySystem = ServiceLocator.Get<IInventorySystem>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<BuildRequestEvent>(OnBuildRequest);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<BuildRequestEvent>(OnBuildRequest);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<BuildingSystem>();
        ServiceLocator.Unregister<IBuildingSystem>();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 查询
    // ══════════════════════════════════════════════════════

    /// <summary>获取建筑定义</summary>
    public BuildingDefinitionSO GetDefinition(string buildingId)
    {
        _definitionMap.TryGetValue(buildingId, out var def);
        return def;
    }

    /// <summary>获取所有已解锁的建筑</summary>
    public List<BuildingDefinitionSO> GetUnlockedBuildings()
    {
        var result = new List<BuildingDefinitionSO>();
        foreach (var id in _unlockedBuildings)
        {
            if (_definitionMap.TryGetValue(id, out var def))
                result.Add(def);
        }
        return result;
    }

    /// <summary>检查建筑是否已建造</summary>
    public bool IsBuilt(string buildingId) => _builtBuildings.Contains(buildingId);

    /// <summary>检查建筑是否已解锁</summary>
    public bool IsUnlocked(string buildingId) => _unlockedBuildings.Contains(buildingId);

    /// <summary>验证是否满足建造条件</summary>
    public CraftingResult ValidateBuild(string buildingId)
    {
        if (!_definitionMap.TryGetValue(buildingId, out var def))
            return CraftingResult.Failed_Unknown;

        if (!_unlockedBuildings.Contains(buildingId))
            return CraftingResult.Failed_NoUnlock;

        if (_builtBuildings.Contains(buildingId))
            return CraftingResult.Failed_Unknown; // 已建造

        // 检查前置
        if (!string.IsNullOrEmpty(def.PrerequisiteBuildingId)
            && !_builtBuildings.Contains(def.PrerequisiteBuildingId))
            return CraftingResult.Failed_NoUnlock;

        // 检查材料
        if (def.RequiredMaterials != null && _inventorySystem != null)
        {
            for (int i = 0; i < def.RequiredMaterials.Length; i++)
            {
                var mat = def.RequiredMaterials[i];
                if (mat.Item == null) continue;
                int have = _inventorySystem.GetTotalItemCount(mat.Item.ItemId);
                if (have < mat.Amount)
                    return CraftingResult.Failed_NoMaterial;
            }
        }

        return CraftingResult.Success;
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 建造与拆除
    // ══════════════════════════════════════════════════════

    /// <summary>执行建造</summary>
    public CraftingResult Build(string buildingId, Vector2 position = default)
    {
        var result = ValidateBuild(buildingId);
        if (result != CraftingResult.Success) return result;

        var def = _definitionMap[buildingId];

        // 消耗材料
        if (def.RequiredMaterials != null && _inventorySystem != null)
        {
            for (int i = 0; i < def.RequiredMaterials.Length; i++)
            {
                var mat = def.RequiredMaterials[i];
                if (mat.Item == null) continue;
                _inventorySystem.TryRemoveItem(mat.Item.ItemId, mat.Amount);
            }
        }

        // 标记已建造
        _builtBuildings.Add(buildingId);

        // 应用效果
        ApplyBuildingEffects(def);

        // 检查是否解锁新建筑
        CheckUnlocks(buildingId);

        // 更新庇护所阶段
        UpdateShelterStage();

        // 广播
        EventBus.Publish(new BuildCompletedEvent
        {
            BuildingId = buildingId,
            DisplayName = def.DisplayName,
            Position = position
        });

        return CraftingResult.Success;
    }

    /// <summary>解锁建筑</summary>
    public void UnlockBuilding(string buildingId)
    {
        if (_unlockedBuildings.Add(buildingId))
        {
            EventBus.Publish(new BuildingUnlockedEvent { BuildingId = buildingId });
        }
    }

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    private void OnBuildRequest(BuildRequestEvent evt)
    {
        Build(evt.BuildingId);
    }

    /// <summary>应用建筑功能效果</summary>
    private void ApplyBuildingEffects(BuildingDefinitionSO def)
    {
        if (def.InventoryCapacityBonus > 0)
        {
            Debug.Log($"[Building] 背包容量 +{def.InventoryCapacityBonus}");
        }

        if (!string.IsNullOrEmpty(def.UnlocksLayerId))
        {
            Debug.Log($"[Building] 解锁地层: {def.UnlocksLayerId}");
        }
    }

    /// <summary>检查是否有新建筑因前置条件满足而解锁</summary>
    private void CheckUnlocks(string justBuiltId)
    {
        foreach (var kvp in _definitionMap)
        {
            if (_unlockedBuildings.Contains(kvp.Key)) continue;
            if (kvp.Value.PrerequisiteBuildingId == justBuiltId)
            {
                UnlockBuilding(kvp.Key);
            }
        }
    }

    /// <summary>根据已建造的功能模块计算庇护所阶段</summary>
    private void UpdateShelterStage()
    {
        int functionalCount = 0;
        foreach (var id in _builtBuildings)
        {
            if (_definitionMap.TryGetValue(id, out var def)
                && def.Category == ShelterModuleCategory.Functional)
            {
                functionalCount++;
            }
        }

        // 简化阶段映射：每2个功能模块升一阶段
        int newStage = Mathf.Clamp(1 + functionalCount / 2, 1, 6);
        if (newStage != _shelterStage)
        {
            int oldStage = _shelterStage;
            _shelterStage = newStage;

            EventBus.Publish(new ShelterStageChangedEvent
            {
                OldStage = oldStage,
                NewStage = newStage
            });
        }
    }

    // ══════════════════════════════════════════════════════
    // ISaveable（占位，后续实现）
    // ══════════════════════════════════════════════════════

    public object CaptureState()
    {
        return null; // 通过 WorldSaveData.ShelterModules 保存
    }

    public void RestoreState(object state) { }
}
