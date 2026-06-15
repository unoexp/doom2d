// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/PauseMenu/PauseMenuPresenter.cs
// 暂停菜单Presenter。管理暂停/恢复、存档、返回主菜单。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 暂停菜单 Presenter。
/// 监听 ESC 键打开/关闭，管理时间暂停。
/// </summary>
public class PauseMenuPresenter : MonoBehaviour
{
    [SerializeField] private PauseMenuView _menuView;

    [Header("场景配置")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    private bool _isPaused;

    private void Start()
    {
        if (_menuView != null)
        {
            _menuView.OnResumeClicked += HandleResume;
            _menuView.OnSaveClicked += HandleSave;
            _menuView.OnSettingsClicked += HandleSettings;
            _menuView.OnMainMenuClicked += HandleMainMenu;

            var uiManager = ServiceLocator.Get<UIManager>();
            if (uiManager != null)
                uiManager.RegisterPanel(_menuView);
        }
    }

    private void Update()
    {
        // ESC 键切换暂停
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused)
                HandleResume();
            else
                Pause();
        }
    }

    private void OnDestroy()
    {
        if (_menuView != null)
        {
            _menuView.OnResumeClicked -= HandleResume;
            _menuView.OnSaveClicked -= HandleSave;
            _menuView.OnSettingsClicked -= HandleSettings;
            _menuView.OnMainMenuClicked -= HandleMainMenu;
        }

        // 确保退出时恢复时间
        if (_isPaused)
            Time.timeScale = 1f;
    }

    // ══════════════════════════════════════════════════════
    // 暂停控制
    // ══════════════════════════════════════════════════════

    private void Pause()
    {
        _isPaused = true;
        Time.timeScale = 0f;

        // 检查庇护所内存档（骨架版本：无 PlayerFacade，始终允许存档）
        bool canSave = true;

        _menuView.SetSaveAvailable(canSave);

        var uiManager = ServiceLocator.Get<UIManager>();
        if (uiManager != null)
            uiManager.OpenPanel(_menuView);

        EventBus.Publish(new GameStateChangedEvent
        {
            PreviousState = GameState.GamePlay,
            NewState = GameState.Paused
        });
    }

    private void HandleResume()
    {
        _isPaused = false;
        Time.timeScale = 1f;

        var uiManager = ServiceLocator.Get<UIManager>();
        if (uiManager != null)
            uiManager.ClosePanel(_menuView);

        EventBus.Publish(new GameStateChangedEvent
        {
            PreviousState = GameState.Paused,
            NewState = GameState.GamePlay
        });
    }

    private void HandleSave()
    {
        if (ServiceLocator.TryGet<SaveLoadSystem>(out var saveSystem))
        {
            saveSystem.Save();
            _menuView.ShowSaveStatus("存档成功", Color.green);
        }
        else
        {
            _menuView.ShowSaveStatus("存档失败", Color.red);
        }
    }

    private void HandleSettings()
    {
        Debug.Log("[PauseMenu] 打开设置（待实现）");
    }

    private void HandleMainMenu()
    {
        _isPaused = false;
        Time.timeScale = 1f;

        EventBus.Clear();
        SceneManager.LoadScene(_mainMenuSceneName);
    }
}
