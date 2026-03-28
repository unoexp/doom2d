// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/01_Data/SaveData/WorldSaveData.cs
// 世界存档数据结构。纯数据，可序列化。
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;

/// <summary>
/// 世界状态存档数据。包含昼夜、天气、已探索区域等。
/// </summary>
[Serializable]
public class WorldSaveData
{
    /// <summary>当前游戏天数</summary>
    public int DayCount = 1;

    /// <summary>当前昼夜阶段</summary>
    public DayPhase CurrentDayPhase = DayPhase.Morning;

    /// <summary>当前游戏内小时（0~23）</summary>
    public float CurrentHour;

    /// <summary>当前天气类型</summary>
    public WeatherType CurrentWeather = WeatherType.Clear;

    /// <summary>当前天气强度（0~1）</summary>
    public float WeatherIntensity;

    /// <summary>已探索的区域ID列表</summary>
    public List<string> ExploredAreaIds = new List<string>();

    /// <summary>世界中已拾取的物品ID列表（防止重复生成）</summary>
    public List<string> PickedUpItemIds = new List<string>();

    /// <summary>已击杀且不应重生的敌人ID列表</summary>
    public List<string> PermanentlyDeadEnemyIds = new List<string>();

    /// <summary>已触发的一次性事件ID列表</summary>
    public List<string> TriggeredEventIds = new List<string>();

    /// <summary>庇护所建造进度数据</summary>
    public List<ShelterModuleEntry> ShelterModules = new List<ShelterModuleEntry>();
}

/// <summary>
/// 庇护所模块存档条目。
/// </summary>
[Serializable]
public struct ShelterModuleEntry
{
    /// <summary>模块ID</summary>
    public string ModuleId;

    /// <summary>是否已建造完成</summary>
    public bool IsBuilt;

    /// <summary>建造进度（0~1，未完成时使用）</summary>
    public float BuildProgress;

    public ShelterModuleEntry(string moduleId, bool isBuilt, float buildProgress)
    {
        ModuleId = moduleId;
        IsBuilt = isBuilt;
        BuildProgress = buildProgress;
    }
}
