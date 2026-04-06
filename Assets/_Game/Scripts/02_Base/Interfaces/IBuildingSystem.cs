// 📁 02_Base/Interfaces/IBuildingSystem.cs
// 建造系统接口定义，供表现层通过ServiceLocator访问

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 建造系统接口，表现层通过此接口与业务层通信
/// 🏗️ 定义在02_Base层，03_Core实现，05_Show引用
/// </summary>
public interface IBuildingSystem
{
    List<BuildingDefinitionSO> GetUnlockedBuildings();
    bool IsBuilt(string buildingId);
    CraftingResult ValidateBuild(string buildingId);
    CraftingResult Build(string buildingId, Vector2 position = default);
}
