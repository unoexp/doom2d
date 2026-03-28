// ─────────────────────────────────────────────────────────────────────
// 📁 Assets/_Game/01_Data/SaveData/SurvivalSaveData.cs
// 生存系统的存档数据结构，纯数据，可序列化
// 注意：使用 List 而非 Dictionary，因为 JsonUtility 不支持序列化 Dictionary
// ─────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;

[Serializable]
public class SurvivalSaveData
{
    /// <summary>各属性当前值</summary>
    public List<SurvivalAttributeEntry> AttributeValues
        = new List<SurvivalAttributeEntry>();

    /// <summary>各属性当前最大值（可被装备/升级修改，需持久化）</summary>
    public List<SurvivalAttributeEntry> AttributeMaxValues
        = new List<SurvivalAttributeEntry>();

    /// <summary>需要跨存档保留的永久状态效果 ID 列表</summary>
    public List<string> PermanentEffectIds
        = new List<string>();
}

/// <summary>
/// 生存属性存档条目。替代 Dictionary&lt;SurvivalAttributeType, float&gt;。
/// </summary>
[Serializable]
public struct SurvivalAttributeEntry
{
    public SurvivalAttributeType Type;
    public float Value;

    public SurvivalAttributeEntry(SurvivalAttributeType type, float value)
    {
        Type = type;
        Value = value;
    }
}
