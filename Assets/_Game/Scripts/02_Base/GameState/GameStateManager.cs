// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Base/GameState/GameStateManager.cs
// 全局游戏状态管理器。驱动 StateMachine<GameState>，控制游戏生命周期。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// 全局游戏状态管理器。
///
/// 核心职责：
///   · 驱动 StateMachine&lt;GameState&gt; 的 Update/FixedUpdate
///   · 状态切换时通过 EventBus 广播 GameStateChangedEvent
///   · 支持 ReturnToPreviousState（暂停恢复）
///   · 通过 MonoSingleton + ServiceLocator 双重访问
/// </summary>
public sealed class GameStateManager : MonoSingleton<GameStateManager>
{
    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    private readonly StateMachine<GameState> _stateMachine = new StateMachine<GameState>();

    /// <summary>上一个状态（用于暂停恢复等场景）</summary>
    private GameState _previousState = GameState.None;

    // ══════════════════════════════════════════════════════
    // 属性
    // ══════════════════════════════════════════════════════

    /// <summary>当前游戏状态</summary>
    public GameState CurrentState => _stateMachine.CurrentStateKey;

    /// <summary>上一个游戏状态</summary>
    public GameState PreviousState => _previousState;

    // ══════════════════════════════════════════════════════
    // 初始化
    // ══════════════════════════════════════════════════════

    protected override void OnInitialize()
    {
        // 注册所有状态
        _stateMachine.AddState(GameState.Initializing, new InitializingState());
        _stateMachine.AddState(GameState.MainMenu,     new MainMenuState());
        _stateMachine.AddState(GameState.Loading,      new LoadingState());
        _stateMachine.AddState(GameState.GamePlay,     new GamePlayState());
        _stateMachine.AddState(GameState.Paused,       new PauseState());
        _stateMachine.AddState(GameState.GameOver,     new GameOverState());

        // 监听内部状态变更，广播到 EventBus
        _stateMachine.OnStateChanged += OnStateMachineChanged;

        ServiceLocator.Register<GameStateManager>(this);

        // 默认进入初始化状态
        _stateMachine.ChangeState(GameState.Initializing);
    }

    public override void Shutdown()
    {
        _stateMachine.OnStateChanged -= OnStateMachineChanged;
        ServiceLocator.Unregister<GameStateManager>();
        base.Shutdown();
    }

    // ══════════════════════════════════════════════════════
    // 驱动状态机
    // ══════════════════════════════════════════════════════

    private void Update()
    {
        _stateMachine.Update(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        _stateMachine.FixedUpdate(Time.fixedDeltaTime);
    }

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 切换到指定游戏状态。
    /// </summary>
    public void ChangeState(GameState newState)
    {
        _stateMachine.ChangeState(newState);
    }

    /// <summary>
    /// 返回上一个状态（典型场景：暂停 → 恢复游戏）。
    /// </summary>
    public void ReturnToPreviousState()
    {
        if (_previousState == GameState.None)
        {
            Debug.LogWarning("[GameStateManager] 没有可返回的上一个状态。");
            return;
        }

        _stateMachine.ChangeState(_previousState);
    }

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    /// <summary>状态机状态变更回调，广播事件并记录历史</summary>
    private void OnStateMachineChanged(GameState from, GameState to)
    {
        _previousState = from;

        EventBus.Publish(new GameStateChangedEvent
        {
            PreviousState = from,
            NewState = to
        });

        Debug.Log($"[GameStateManager] 状态切换：{from} → {to}");
    }
}
