// 📁 02_Base/EventBus/Events/InventoryExpansionEvents.cs
// ⚠️ 所有事件定义为结构体，零GC分配

using System.Collections.Generic;
using SurvivalGame.Data.Inventory.Expansion;

/// <summary>扩展条件验证开始</summary>
public struct InventoryExpansionValidationStartedEvent : IEvent
{
    public string ExpansionId;              // 扩展ID
    public string ContainerId;              // 容器ID（主背包或快捷栏）
    public int CurrentCapacity;             // 当前容量
    public int TargetCapacity;              // 目标容量
}

/// <summary>扩展条件验证结果</summary>
public struct InventoryExpansionValidationResultEvent : IEvent
{
    public string ExpansionId;              // 扩展ID
    public string ContainerId;              // 容器ID
    public bool AllConditionsMet;           // 所有条件是否满足
    public int TotalConditions;             // 总条件数
    public int MetConditions;               // 满足的条件数
    public List<ExpansionConditionResult> FailedResults;  // 失败的条件结果
    public string FailureSummary;           // 失败摘要（用于UI显示）
}

/// <summary>扩展条件验证完成（单个条件）</summary>
public struct InventoryExpansionConditionValidatedEvent : IEvent
{
    public string ExpansionId;              // 扩展ID
    public string ConditionId;              // 条件ID
    public ExpansionConditionType ConditionType; // 条件类型
    public bool IsMet;                      // 是否满足
    public string ConditionName;            // 条件名称（用于UI）
    public string FailedReason;             // 失败原因（如果失败）
}

/// <summary>扩展资源消耗开始</summary>
public struct InventoryExpansionConsumptionStartedEvent : IEvent
{
    public string ExpansionId;              // 扩展ID
    public string ContainerId;              // 容器ID
    public int TotalResourcesToConsume;     // 需要消耗的资源总数
}

/// <summary>扩展资源消耗结果</summary>
public struct InventoryExpansionConsumptionResultEvent : IEvent
{
    public string ExpansionId;              // 扩展ID
    public string ContainerId;              // 容器ID
    public bool AllConsumptionsSucceeded;   // 所有消耗是否成功
    public int TotalConsumptions;           // 总消耗数
    public int SucceededConsumptions;       // 成功的消耗数
    public List<ExpansionConsumptionResult> FailedResults;  // 失败的消耗结果
    public string FailureSummary;           // 失败摘要
}

/// <summary>扩展资源消耗完成（单个资源）</summary>
public struct InventoryExpansionResourceConsumedEvent : IEvent
{
    public string ExpansionId;              // 扩展ID
    public string ItemId;                   // 物品ID
    public int AmountConsumed;              // 消耗数量
    public string ResourceName;             // 资源名称（用于UI）
    public string ContainerId;              // 从中消耗的容器ID
    public int RemainingAmount;             // 消耗后剩余数量
}

/// <summary>扩展效果应用开始</summary>
public struct InventoryExpansionEffectStartedEvent : IEvent
{
    public string ExpansionId;              // 扩展ID
    public string ContainerId;              // 容器ID
    public int CurrentCapacity;             // 当前容量
    public int NewCapacity;                 // 新容量
    public string ExpansionName;            // 扩展名称（用于UI）
}

/// <summary>扩展效果应用成功</summary>
public struct InventoryExpansionEffectAppliedEvent : IEvent
{
    public string ExpansionId;              // 扩展ID
    public string ContainerId;              // 容器ID
    public int OldCapacity;                 // 旧容量
    public int NewCapacity;                 // 新容量
    public int CapacityIncrease;            // 容量增加量
    public string ExpansionName;            // 扩展名称
    public System.DateTime CompletionTime;  // 完成时间
}

/// <summary>扩展效果应用失败</summary>
public struct InventoryExpansionEffectFailedEvent : IEvent
{
    public string ExpansionId;              // 扩展ID
    public string ContainerId;              // 容器ID
    public string FailureReason;            // 失败原因
    public bool CanRetry;                   // 是否可以重试
    public string TechnicalError;           // 技术错误详情
}

/// <summary>扩展完成（完整流程）</summary>
public struct InventoryExpansionCompletedEvent : IEvent
{
    public string ExpansionId;              // 扩展ID
    public string ContainerId;              // 容器ID
    public int OldCapacity;                 // 旧容量
    public int NewCapacity;                 // 新容量
    public string ExpansionName;            // 扩展名称
    public System.DateTime CompletionTime;  // 完成时间
    public int TotalResourcesConsumed;      // 总共消耗的资源数量
}

/// <summary>扩展状态更新（用于UI实时更新）</summary>
public struct InventoryExpansionStatusUpdatedEvent : IEvent
{
    public string ExpansionId;              // 扩展ID
    public string ContainerId;              // 容器ID
    public ExpansionStatus Status;          // 当前状态
    public float ProgressPercentage;        // 进度百分比（0-1）
    public string StatusMessage;            // 状态消息（用于UI）
    public bool IsComplete;                 // 是否完成
}

/// <summary>扩展回滚开始（撤销扩展）</summary>
public struct InventoryExpansionRollbackStartedEvent : IEvent
{
    public string ExpansionId;              // 扩展ID
    public string ContainerId;              // 容器ID
    public int CurrentCapacity;             // 当前容量
    public int TargetCapacity;              // 目标容量（回滚后的容量）
}

/// <summary>扩展回滚完成</summary>
public struct InventoryExpansionRollbackCompletedEvent : IEvent
{
    public string ExpansionId;              // 扩展ID
    public string ContainerId;              // 容器ID
    public int OldCapacity;                 // 回滚前的容量
    public int NewCapacity;                 // 回滚后的容量
    public bool ResourcesRestored;          // 资源是否已恢复
    public int ResourcesRestoredCount;      // 恢复的资源数量
}

/// <summary>扩展进度查询结果</summary>
public struct InventoryExpansionProgressQueriedEvent : IEvent
{
    public string ExpansionId;              // 扩展ID
    public string ContainerId;              // 容器ID
    public bool IsAvailable;                // 是否可用（条件满足）
    public bool IsCompleted;                // 是否已完成
    public int CompletionCount;             // 完成次数
    public List<ExpansionConditionResult> MissingConditions;  // 缺失的条件
    public string NextAvailableTime;        // 下次可用时间（用于冷却时间）
}

/// <summary>扩展解锁状态变化</summary>
public struct InventoryExpansionUnlockedEvent : IEvent
{
    public string ExpansionId;              // 扩展ID
    public string ContainerId;              // 容器ID
    public bool NewlyUnlocked;              // 是否新解锁
    public string UnlockMethod;             // 解锁方式（任务、等级、购买等）
    public System.DateTime UnlockTime;      // 解锁时间
}

/// <summary>扩展配置加载完成</summary>
public struct InventoryExpansionConfigsLoadedEvent : IEvent
{
    public int TotalConfigs;
    public int AvailableConfigs;
    public int CompletedConfigs;
    public string[] AvailableExpansionIds;
}

// ── 效果服务事件（从 DefaultExpansionEffectService.cs 移至此处）──

/// <summary>效果应用开始</summary>
public struct ExpansionEffectApplicationStartedEvent : IEvent
{
    public string ExpansionId;
    public string ContainerId;
    public System.DateTime StartTime;
    public SurvivalGame.Data.Inventory.Expansion.ExpansionType EffectType;
}

/// <summary>效果应用结果</summary>
public struct ExpansionEffectAppliedEvent : IEvent
{
    public string ExpansionId;
    public string ContainerId;
    public bool Success;
    public SurvivalGame.Data.Inventory.Expansion.ExpansionType EffectType;
    public System.DateTime ApplicationTime;
    public string FailureReason;
}

/// <summary>效果回滚开始</summary>
public struct ExpansionEffectRollbackStartedEvent : IEvent
{
    public string ExpansionId;
    public string ContainerId;
    public System.DateTime RollbackTime;
}

/// <summary>效果回滚完成</summary>
public struct ExpansionEffectRollbackCompletedEvent : IEvent
{
    public string ExpansionId;
    public string ContainerId;
    public bool Success;
    public System.DateTime RollbackTime;
    public string FailureReason;
}

// ── 消耗服务事件（从 DefaultExpansionConsumptionService.cs 移至此处）──

/// <summary>条件消耗事件</summary>
public struct ExpansionConditionConsumedEvent : IEvent
{
    public string ConditionId;
    public SurvivalGame.Data.Inventory.Expansion.ExpansionConditionType ConditionType;
    public bool Success;
    public System.DateTime Timestamp;
}

/// <summary>条件消耗失败事件</summary>
public struct ExpansionConditionConsumptionFailedEvent : IEvent
{
    public string ConditionId;
    public SurvivalGame.Data.Inventory.Expansion.ExpansionConditionType ConditionType;
    public string FailureReason;
    public System.DateTime Timestamp;
}

/// <summary>批量消耗开始</summary>
public struct ExpansionBatchConsumptionStartedEvent : IEvent
{
    public string ExpansionId;
    public int TotalConditions;
    public bool BatchMode;
}

/// <summary>批量消耗完成</summary>
public struct ExpansionBatchConsumptionCompletedEvent : IEvent
{
    public string ExpansionId;
    public int TotalConditions;
    public int SucceededConditions;
    public int FailedConditions;
    public bool BatchMode;
    public float AverageQueueTime;
}

/// <summary>消耗回滚事件</summary>
public struct ExpansionConsumptionRollbackEvent : IEvent
{
    public string ConditionId;
    public SurvivalGame.Data.Inventory.Expansion.ExpansionConditionType ConditionType;
    public SurvivalGame.Data.Inventory.Expansion.ExpansionConsumptionResult OriginalResult;
    public System.DateTime RollbackTime;
}

/// <summary>消耗队列清空事件</summary>
public struct ExpansionConsumptionQueueClearedEvent : IEvent
{
    public int ClearedCount;
    public System.DateTime Timestamp;
}

/// <summary>扩展状态枚举</summary>
public enum ExpansionStatus
{
    Idle,               // 闲置
    Validating,         // 验证中
    Consuming,          // 消耗中
    Applying,           // 应用效果中
    Complete,           // 完成
    Failed,             // 失败
    RollingBack,        // 回滚中
    Unavailable         // 不可用（条件不满足）
}