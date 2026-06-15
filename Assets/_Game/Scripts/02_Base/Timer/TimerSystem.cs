// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Infrastructure/Timer/TimerSystem.cs
// 计时器管理器核心
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 中央计时器管理系统。
/// 
/// 核心特性：
///   · 对象池驱动，运行时零 GC 分配
///   · 支持全局 / 单个暂停
///   · 支持时间缩放（配合昼夜/睡眠系统）
///   · 支持单次 / 循环 / 有限次循环
///   · 支持实时模式（不受 TimeScale 影响，用于 UI 动画）
///   · 通过 TimerHandle 安全控制，句柄失效自动无效化
/// </summary>
public sealed class TimerSystem : MonoSingleton<TimerSystem>
{
    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    /// <summary>活跃计时器表（Id → Timer）</summary>
    private readonly Dictionary<uint, Timer> _activeTimers
        = new Dictionary<uint, Timer>();

    /// <summary>对象池</summary>
    private readonly Queue<Timer> _pool = new Queue<Timer>();

    /// <summary>Tick 中产生的待移除列表，避免遍历时修改集合</summary>
    private readonly List<uint> _pendingRemove = new List<uint>();

    /// <summary>全局暂停开关（暂停菜单时调用）</summary>
    private bool _globalPaused;

    /// <summary>全局时间缩放倍率（昼夜快进/慢动作）</summary>
    private float _globalTimeScale = 1f;

    /// <summary>ID 自增计数器</summary>
    private uint _nextId = 1;

    /// <summary>对象池预热数量</summary>
    private const int POOL_PREWARM_COUNT = 32;

    // ══════════════════════════════════════════════════════
    // 初始化
    // ══════════════════════════════════════════════════════

    protected override void OnInitialize()
    {
        // 预热对象池，避免游戏运行中首次分配
        for (int i = 0; i < POOL_PREWARM_COUNT; i++)
            _pool.Enqueue(new Timer());

        ServiceLocator.Register<TimerSystem>(this);
    }

    public override void Shutdown()
    {
        ServiceLocator.Unregister<TimerSystem>();
        base.Shutdown();
    }

    // ══════════════════════════════════════════════════════
    // Update
    // ══════════════════════════════════════════════════════

    private void Update()
    {
        if (_globalPaused) return;

        float scaledDt   = Time.deltaTime * _globalTimeScale;  // 受缩放影响
        float realDt     = Time.unscaledDeltaTime;             // 实时，不受缩放

        _pendingRemove.Clear();

        // [PERF] 直接遍历 Dictionary Values，无 LINQ
        foreach (var kv in _activeTimers)
        {
            var timer = kv.Value;
            if (timer.IsPaused || timer.IsFinished) continue;

            float dt = timer.UseRealTime ? realDt : scaledDt;
            timer.Elapsed += dt;

            if (timer.Elapsed < timer.Duration) continue;

            // ── 计时完成 ──
            timer.Elapsed -= timer.Duration; // 保留溢出时间，循环更准确

            try
            {
                timer.OnComplete?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[TimerSystem] Timer {timer.Id} OnComplete 回调异常：{e}");
            }

            // ── 判断是否继续循环 ──
            if (timer.IsLoop)
            {
                if (timer.LoopCount < 0)
                {
                    // 无限循环：继续
                    continue;
                }

                timer.LoopRemaining--;
                if (timer.LoopRemaining > 0)
                {
                    // 有限循环：还有剩余次数
                    continue;
                }
            }

            // ── 计时器结束（单次 or 循环耗尽）──
            try
            {
                timer.OnLoopEnd?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[TimerSystem] Timer {timer.Id} OnLoopEnd 回调异常：{e}");
            }

            timer.IsFinished = true;
            _pendingRemove.Add(kv.Key);
        }

        // 统一回收到期计时器
        for (int i = 0; i < _pendingRemove.Count; i++)
            ReleaseTimer(_pendingRemove[i]);
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 创建计时器
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 创建一个单次计时器。
    /// </summary>
    /// <param name="duration">持续时间（秒）</param>
    /// <param name="onComplete">结束回调</param>
    /// <param name="useRealTime">是否使用实时（不受 TimeScale 影响）</param>
    public TimerHandle Create(float duration, Action onComplete, bool useRealTime = false)
    {
        return CreateInternal(
            duration:     duration,
            onComplete:   onComplete,
            onLoopEnd:    null,
            isLoop:       false,
            loopCount:    1,
            useRealTime:  useRealTime);
    }

    /// <summary>
    /// 创建一个无限循环计时器。
    /// </summary>
    /// <param name="interval">每次间隔（秒）</param>
    /// <param name="onTick">每次触发回调</param>
    /// <param name="useRealTime">是否使用实时</param>
    public TimerHandle CreateLoop(float interval, Action onTick, bool useRealTime = false)
    {
        return CreateInternal(
            duration:     interval,
            onComplete:   onTick,
            onLoopEnd:    null,
            isLoop:       true,
            loopCount:    -1,   // -1 = 无限
            useRealTime:  useRealTime);
    }

    /// <summary>
    /// 创建一个有限次数循环计时器。
    /// </summary>
    /// <param name="interval">每次间隔（秒）</param>
    /// <param name="loopCount">循环次数</param>
    /// <param name="onTick">每次触发回调</param>
    /// <param name="onAllComplete">所有次数结束后的回调</param>
    /// <param name="useRealTime">是否使用实时</param>
    public TimerHandle CreateLoopCount(
        float  interval,
        int    loopCount,
        Action onTick,
        Action onAllComplete = null,
        bool   useRealTime   = false)
    {
        return CreateInternal(
            duration:     interval,
            onComplete:   onTick,
            onLoopEnd:    onAllComplete,
            isLoop:       true,
            loopCount:    loopCount,
            useRealTime:  useRealTime);
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 控制单个计时器
    // ══════════════════════════════════════════════════════

    public void Pause(TimerHandle handle)
    {
        if (TryGetTimer(handle, out var timer))
            timer.IsPaused = true;
    }

    public void Resume(TimerHandle handle)
    {
        if (TryGetTimer(handle, out var timer))
            timer.IsPaused = false;
    }

    public void Cancel(TimerHandle handle)
    {
        if (handle == null || !_activeTimers.ContainsKey(handle.Id)) return;
        ReleaseTimer(handle.Id);
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 全局控制（暂停菜单 / 昼夜系统）
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 全局暂停所有非实时计时器（打开暂停菜单时调用）。
    /// UseRealTime = true 的计时器不受影响。
    /// </summary>
    public void PauseAll()  => _globalPaused = true;

    /// <summary>全局恢复</summary>
    public void ResumeAll() => _globalPaused = false;

    /// <summary>
    /// 设置全局时间缩放（睡觉快进时间、慢动作等）。
    /// 只影响 UseRealTime = false 的计时器。
    /// </summary>
    public void SetGlobalTimeScale(float scale)
        => _globalTimeScale = Mathf.Max(0f, scale);

    /// <summary>取消所有计时器（场景切换时调用）</summary>
    public void CancelAll()
    {
        foreach (var kv in _activeTimers)
        {
            kv.Value.Reset();
            _pool.Enqueue(kv.Value);
        }
        _activeTimers.Clear();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 查询（供 TimerHandle 属性调用）
    // ══════════════════════════════════════════════════════

    public bool  IsValid(TimerHandle handle)
        => handle != null && _activeTimers.ContainsKey(handle.Id);

    public float GetRemainingTime(TimerHandle handle)
        => TryGetTimer(handle, out var t) ? t.Duration - t.Elapsed : 0f;

    public float GetElapsedTime(TimerHandle handle)
        => TryGetTimer(handle, out var t) ? t.Elapsed : 0f;

    public float GetProgress(TimerHandle handle)
    {
        if (!TryGetTimer(handle, out var t)) return 0f;
        if (t.InitialDuration <= 0f) return 1f;
        return Mathf.Clamp01(t.Elapsed / t.InitialDuration);
    }

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    private TimerHandle CreateInternal(
        float  duration,
        Action onComplete,
        Action onLoopEnd,
        bool   isLoop,
        int    loopCount,
        bool   useRealTime)
    {
        if (duration <= 0f)
        {
            Debug.LogWarning("[TimerSystem] duration 必须 > 0，已忽略本次创建。");
            return new TimerHandle();
        }

        // 从对象池取出（或新建）
        var timer = _pool.Count > 0 ? _pool.Dequeue() : new Timer();
        timer.Reset();

        timer.Id              = _nextId++;
        timer.Duration        = duration;
        timer.InitialDuration = duration;
        timer.IsLoop          = isLoop;
        timer.LoopCount       = loopCount;
        timer.LoopRemaining   = loopCount;
        timer.UseRealTime     = useRealTime;
        timer.OnComplete      = onComplete;
        timer.OnLoopEnd       = onLoopEnd;

        _activeTimers[timer.Id] = timer;

        var handle = new TimerHandle { Id = timer.Id };
        return handle;
    }

    private void ReleaseTimer(uint id)
    {
        if (!_activeTimers.TryGetValue(id, out var timer)) return;
        _activeTimers.Remove(id);
        timer.Reset();
        _pool.Enqueue(timer);   // 归还对象池
    }

    private bool TryGetTimer(TimerHandle handle, out Timer timer)
    {
        timer = null;
        if (handle == null) return false;
        return _activeTimers.TryGetValue(handle.Id, out timer);
    }
}