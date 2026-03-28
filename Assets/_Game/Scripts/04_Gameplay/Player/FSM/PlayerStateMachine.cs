// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/04_Gameplay/Player/FSM/PlayerStateMachine.cs
// 玩家状态机。封装 StateMachine<PlayerState>，注册所有玩家行为状态。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// 玩家状态机管理器。
///
/// 职责：
///   · 创建并注册所有玩家状态
///   · 提供状态切换的便捷 API
///   · 由 PlayerController 驱动 Update/FixedUpdate
/// </summary>
public class PlayerStateMachine
{
    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    private readonly StateMachine<PlayerState> _fsm = new StateMachine<PlayerState>();
    private readonly PlayerController _player;

    // ══════════════════════════════════════════════════════
    // 属性
    // ══════════════════════════════════════════════════════

    public PlayerState CurrentState => _fsm.CurrentStateKey;

    // ══════════════════════════════════════════════════════
    // 构造
    // ══════════════════════════════════════════════════════

    public PlayerStateMachine(PlayerController player)
    {
        _player = player;

        // 注册基础状态
        _fsm.AddState(PlayerState.Idle,   new PlayerIdleState(player, this));
        _fsm.AddState(PlayerState.Walk,   new PlayerMoveState(player, this));
        _fsm.AddState(PlayerState.Run,    new PlayerRunState(player, this));
        _fsm.AddState(PlayerState.Jump,   new PlayerJumpState(player, this));
        _fsm.AddState(PlayerState.Fall,   new PlayerFallState(player, this));
        _fsm.AddState(PlayerState.Attack, new PlayerAttackState(player, this));
        _fsm.AddState(PlayerState.Dodge,  new PlayerDodgeState(player, this));
        _fsm.AddState(PlayerState.Dead,   new PlayerDeadState(player, this));

        // 监听状态变更
        _fsm.OnStateChanged += OnStateChanged;
    }

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>切换到指定状态</summary>
    public void ChangeState(PlayerState state)
    {
        _fsm.ChangeState(state);
    }

    /// <summary>由 PlayerController.Update 驱动</summary>
    public void Update(float deltaTime)
    {
        _fsm.Update(deltaTime);
    }

    /// <summary>由 PlayerController.FixedUpdate 驱动</summary>
    public void FixedUpdate(float fixedDeltaTime)
    {
        _fsm.FixedUpdate(fixedDeltaTime);
    }

    /// <summary>设置初始状态</summary>
    public void Initialize(PlayerState initialState = PlayerState.Idle)
    {
        _fsm.ChangeState(initialState);
    }

    // ══════════════════════════════════════════════════════
    // 内部
    // ══════════════════════════════════════════════════════

    private void OnStateChanged(PlayerState from, PlayerState to)
    {
        Debug.Log($"[PlayerFSM] {from} → {to}");
    }
}
