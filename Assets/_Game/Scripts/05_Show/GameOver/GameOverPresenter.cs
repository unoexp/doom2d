// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/GameOver/GameOverPresenter.cs
// 死亡结算Presenter。连接业务事件与ViewModel，处理用户操作。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// 死亡结算 Presenter。
///
/// 核心职责：
///   · 订阅 PlayerDeadEvent，触发结算面板显示
///   · 将死亡数据写入 ViewModel
///   · 处理用户按钮操作（读档/返回主菜单）
///   · 通过 EventBus 发布状态切换请求
///
/// 设计说明：
///   · 存活时间从 GameTimeSystem 获取（如可用）
///   · 读档操作通过 SaveEvents 通知 SaveLoadSystem
///   · 返回主菜单通过 GameStateChangedEvent 通知 GameStateManager
/// </summary>
public class GameOverPresenter : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // 引用
    // ══════════════════════════════════════════════════════

    [SerializeField] private GameOverPanelView _view;

    private GameOverViewModel _viewModel;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        _viewModel = new GameOverViewModel();
    }

    private void Start()
    {
        if (_view != null)
        {
            _view.Bind(_viewModel);

            // 注册到 UIManager
            var uiManager = ServiceLocator.Get<UIManager>();
            if (uiManager != null)
            {
                uiManager.RegisterPanel(_view);
            }

            // 绑定按钮事件
            _view.OnLoadSaveClicked += HandleLoadSave;
            _view.OnMainMenuClicked += HandleReturnToMainMenu;
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerDeadEvent>(OnPlayerDead);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerDeadEvent>(OnPlayerDead);
    }

    private void OnDestroy()
    {
        if (_view != null)
        {
            _view.OnLoadSaveClicked -= HandleLoadSave;
            _view.OnMainMenuClicked -= HandleReturnToMainMenu;

            var uiManager = ServiceLocator.Get<UIManager>();
            if (uiManager != null)
            {
                uiManager.UnregisterPanel(_view);
            }
        }
    }

    // ══════════════════════════════════════════════════════
    // 事件处理
    // ══════════════════════════════════════════════════════

    private void OnPlayerDead(PlayerDeadEvent evt)
    {
        // 获取存活时间
        float survivalTime = 0f;
        if (ServiceLocator.TryGet<GameTimeSystem>(out var timeSystem))
        {
            survivalTime = timeSystem.TotalPlayTime;
        }

        // 更新 ViewModel
        _viewModel.SetData(evt.Cause, survivalTime);

        // 打开结算面板
        var uiManager = ServiceLocator.Get<UIManager>();
        if (uiManager != null && _view != null)
        {
            uiManager.OpenPanel(_view);
        }

        Debug.Log($"[GameOver] 玩家死亡: {evt.Cause}，存活 {survivalTime:F0}秒");
    }

    // ══════════════════════════════════════════════════════
    // 按钮处理
    // ══════════════════════════════════════════════════════

    private void HandleLoadSave()
    {
        // 关闭结算面板
        var uiManager = ServiceLocator.Get<UIManager>();
        if (uiManager != null)
        {
            uiManager.ClosePanel(_view);
        }

        // 发布读档请求
        EventBus.Publish(new LoadGameRequestEvent());

        Debug.Log("[GameOver] 请求读取存档");
    }

    private void HandleReturnToMainMenu()
    {
        // 关闭结算面板
        var uiManager = ServiceLocator.Get<UIManager>();
        if (uiManager != null)
        {
            uiManager.CloseAllPanels();
        }

        // 请求切换到主菜单状态
        var gameStateManager = ServiceLocator.Get<GameStateManager>();
        if (gameStateManager != null)
        {
            gameStateManager.ChangeState(GameState.MainMenu);
        }

        Debug.Log("[GameOver] 返回主菜单");
    }
}
