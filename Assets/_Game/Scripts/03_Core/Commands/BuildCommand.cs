// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/03_Core/Commands/BuildCommand.cs
// 建造命令。封装建造操作的执行与撤销逻辑。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// 可撤销的建造命令。
///
/// 核心职责：
///   · Execute: 调用 BuildingSystem.Build 消耗材料并建造建筑
///   · Undo: 归还消耗的材料并移除已建造的建筑
///
/// 设计说明：
///   · 撤销时归还材料但不恢复庇护所阶段（阶段由 BuildingSystem 自动计算）
///   · 建筑的拆除通过发布 BuildingDemolishedEvent 通知其他系统
/// </summary>
public class BuildCommand : ICommand
{
    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    private readonly string _buildingId;
    private readonly Vector2 _position;

    /// <summary>建造是否成功执行过</summary>
    private bool _executed;

    /// <summary>消耗的材料快照（撤销时归还）</summary>
    private BuildingMaterial[] _consumedMaterials;

    // ══════════════════════════════════════════════════════
    // ICommand
    // ══════════════════════════════════════════════════════

    public string Description { get; private set; }

    // ══════════════════════════════════════════════════════
    // 构造
    // ══════════════════════════════════════════════════════

    /// <param name="buildingId">建筑ID</param>
    /// <param name="position">建造位置</param>
    public BuildCommand(string buildingId, Vector2 position = default)
    {
        _buildingId = buildingId;
        _position = position;
        Description = $"建造 {buildingId}";
    }

    // ══════════════════════════════════════════════════════
    // 执行
    // ══════════════════════════════════════════════════════

    public void Execute()
    {
        var buildingSystem = ServiceLocator.Get<BuildingSystem>();
        if (buildingSystem == null)
        {
            Debug.LogWarning("[BuildCommand] BuildingSystem 未注册");
            return;
        }

        // 记录材料快照用于撤销
        var def = buildingSystem.GetDefinition(_buildingId);
        if (def == null)
        {
            Debug.LogWarning($"[BuildCommand] 建筑定义不存在: {_buildingId}");
            return;
        }

        _consumedMaterials = def.RequiredMaterials;
        Description = $"建造 {def.DisplayName}";

        var result = buildingSystem.Build(_buildingId, _position);
        _executed = result == CraftingResult.Success;
    }

    public void Undo()
    {
        if (!_executed) return;

        var inventory = ServiceLocator.Get<IInventorySystem>();
        if (inventory == null) return;

        // 归还消耗的材料
        if (_consumedMaterials != null)
        {
            for (int i = 0; i < _consumedMaterials.Length; i++)
            {
                var mat = _consumedMaterials[i];
                if (mat.Item == null) continue;
                inventory.TryAddItem(mat.Item.ItemId, mat.Amount);
            }
        }

        // 发布拆除事件（BuildingSystem 可订阅此事件处理状态回滚）
        EventBus.Publish(new BuildingDemolishedEvent
        {
            BuildingId = _buildingId,
            Position = _position
        });

        _executed = false;
        Debug.Log($"[BuildCommand] 已撤销建造: {Description}");
    }
}
