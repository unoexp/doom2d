// ══════════════════════════════════════════════════════════════════════
// 📁 03_Core/Save/SaveLoadSystem.cs
// 存档/读档核心系统，管理所有 ISaveable 的持久化
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 存档/读档核心系统。
///
/// 核心职责：
///   · 维护 ISaveable 注册表
///   · 存档时遍历所有 ISaveable，调用 CaptureState() 采集数据
///   · 读档时反序列化数据，调用 RestoreState() 恢复各系统状态
///   · 通过 EventBus 发布存档/读档生命周期事件
///
/// 使用方式：
///   · 各业务系统在初始化时调用 Register(this)
///   · 在销毁时调用 Unregister(this)
///   · 上层通过 ServiceLocator.Get&lt;SaveLoadSystem&gt;() 获取实例
/// </summary>
public class SaveLoadSystem : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // 常量
    // ══════════════════════════════════════════════════════

    /// <summary>存档文件名前缀</summary>
    private const string SAVE_FILE_PREFIX = "save_";

    /// <summary>存档文件扩展名</summary>
    private const string SAVE_FILE_EXTENSION = ".json";

    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    /// <summary>所有已注册的可存档系统</summary>
    private readonly List<ISaveable> _saveables = new List<ISaveable>();

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        ServiceLocator.Register<SaveLoadSystem>(this);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<SaveLoadSystem>();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 注册 / 注销
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 注册一个可存档系统。
    /// 通常在各业务系统的 Awake/Start 中调用。
    /// </summary>
    public void Register(ISaveable saveable)
    {
        if (saveable == null)
        {
            Debug.LogWarning("[SaveLoadSystem] 尝试注册 null ISaveable，已忽略");
            return;
        }

        if (_saveables.Contains(saveable))
        {
            Debug.LogWarning($"[SaveLoadSystem] ISaveable '{saveable.SaveKey}' 已注册，跳过重复注册");
            return;
        }

        _saveables.Add(saveable);
    }

    /// <summary>
    /// 注销一个可存档系统。
    /// 通常在各业务系统的 OnDestroy 中调用。
    /// </summary>
    public void Unregister(ISaveable saveable)
    {
        if (saveable == null) return;
        _saveables.Remove(saveable);
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 存档
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 将所有已注册 ISaveable 的状态保存到指定槽位。
    /// </summary>
    /// <param name="slotIndex">存档槽位索引（默认 0）</param>
    public void Save(int slotIndex = 0)
    {
        EventBus.Publish(new SaveStartedEvent { SlotIndex = slotIndex });

        try
        {
            // 采集所有 ISaveable 的状态数据
            var stateMap = new Dictionary<string, object>(_saveables.Count);
            for (int i = 0; i < _saveables.Count; i++)
            {
                var saveable = _saveables[i];
                try
                {
                    stateMap[saveable.SaveKey] = saveable.CaptureState();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveLoadSystem] 采集 '{saveable.SaveKey}' 状态失败：{e}");
                }
            }

            // 序列化为 JSON
            string json = SaveSerializer.Serialize(stateMap);

            // 写入文件（确保目录存在）
            string filePath = GetSaveFilePath(slotIndex);
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, json);

            Debug.Log($"[SaveLoadSystem] 存档成功 → {filePath}（{_saveables.Count} 个系统）");
            EventBus.Publish(new SaveCompletedEvent { SlotIndex = slotIndex, Success = true });
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoadSystem] 存档失败（槽位 {slotIndex}）：{e}");
            EventBus.Publish(new SaveCompletedEvent { SlotIndex = slotIndex, Success = false });
        }
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 读档
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 从指定槽位读取存档并恢复所有已注册 ISaveable 的状态。
    /// </summary>
    /// <param name="slotIndex">存档槽位索引（默认 0）</param>
    public void Load(int slotIndex = 0)
    {
        EventBus.Publish(new LoadStartedEvent { SlotIndex = slotIndex });

        try
        {
            string filePath = GetSaveFilePath(slotIndex);

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[SaveLoadSystem] 存档文件不存在：{filePath}");
                EventBus.Publish(new LoadCompletedEvent { SlotIndex = slotIndex, Success = false });
                return;
            }

            // 读取并反序列化
            string json = File.ReadAllText(filePath);
            var stateMap = SaveSerializer.Deserialize(json);

            // 恢复各 ISaveable 的状态
            for (int i = 0; i < _saveables.Count; i++)
            {
                var saveable = _saveables[i];

                if (!stateMap.TryGetValue(saveable.SaveKey, out string jsonData))
                {
                    Debug.LogWarning($"[SaveLoadSystem] 存档中未找到 '{saveable.SaveKey}' 的数据，跳过恢复");
                    continue;
                }

                try
                {
                    saveable.RestoreState(jsonData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveLoadSystem] 恢复 '{saveable.SaveKey}' 状态失败：{e}");
                }
            }

            Debug.Log($"[SaveLoadSystem] 读档成功 ← {filePath}（{_saveables.Count} 个系统）");
            EventBus.Publish(new LoadCompletedEvent { SlotIndex = slotIndex, Success = true });
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoadSystem] 读档失败（槽位 {slotIndex}）：{e}");
            EventBus.Publish(new LoadCompletedEvent { SlotIndex = slotIndex, Success = false });
        }
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 查询与管理
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 检查指定槽位是否存在存档文件。
    /// </summary>
    public bool HasSaveData(int slotIndex = 0)
    {
        return File.Exists(GetSaveFilePath(slotIndex));
    }

    /// <summary>
    /// 删除指定槽位的存档文件。
    /// </summary>
    public void DeleteSave(int slotIndex = 0)
    {
        string filePath = GetSaveFilePath(slotIndex);

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"[SaveLoadSystem] 已删除存档：{filePath}");
            }
            else
            {
                Debug.LogWarning($"[SaveLoadSystem] 删除失败，存档文件不存在：{filePath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoadSystem] 删除存档失败（槽位 {slotIndex}）：{e}");
        }
    }

    /// <summary>
    /// 获取指定槽位的存档文件完整路径。
    /// </summary>
    public string GetSaveFilePath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath,
            $"{SAVE_FILE_PREFIX}{slotIndex}{SAVE_FILE_EXTENSION}");
    }
}
