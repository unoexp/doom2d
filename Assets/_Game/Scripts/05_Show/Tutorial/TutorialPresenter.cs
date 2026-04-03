// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/Tutorial/TutorialPresenter.cs
// 教学提示 Presenter。订阅教学事件，在屏幕上显示/隐藏提示。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;
using TMPro;

/// <summary>
/// 教学提示 Presenter。
///
/// 核心职责：
///   · 订阅 TutorialTriggerEvent 显示提示
///   · 管理提示的显示时长和淡出
///   · 不用弹窗，使用底部浮动文字
/// </summary>
public class TutorialPresenter : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("UI 引用")]
    [SerializeField] private GameObject _tutorialPanel;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("参数")]
    [SerializeField] private float _displayDuration = 4f;
    [SerializeField] private float _fadeOutDuration = 1f;

    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    private float _timer;
    private bool _isShowing;
    private bool _isFading;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Start()
    {
        if (_tutorialPanel != null)
            _tutorialPanel.SetActive(false);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<TutorialTriggerEvent>(OnTutorialTrigger);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<TutorialTriggerEvent>(OnTutorialTrigger);
    }

    private void Update()
    {
        if (!_isShowing) return;

        _timer -= Time.deltaTime;

        if (_timer <= 0f && !_isFading)
        {
            _isFading = true;
            _timer = _fadeOutDuration;
        }

        if (_isFading)
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = Mathf.Clamp01(_timer / _fadeOutDuration);

            if (_timer <= 0f)
            {
                _isShowing = false;
                _isFading = false;
                if (_tutorialPanel != null)
                    _tutorialPanel.SetActive(false);
            }
        }
    }

    // ══════════════════════════════════════════════════════
    // 事件处理
    // ══════════════════════════════════════════════════════

    private void OnTutorialTrigger(TutorialTriggerEvent evt)
    {
        ShowMessage(evt.Message);
    }

    private void ShowMessage(string message)
    {
        if (_tutorialPanel != null)
            _tutorialPanel.SetActive(true);

        if (_messageText != null)
            _messageText.text = message;

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        _timer = _displayDuration;
        _isShowing = true;
        _isFading = false;
    }
}
