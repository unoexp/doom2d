// 📁 05_Show/Inventory/Views/Components/ExpansionItemView.cs
// 扩展项UI组件
// 🏗️ 架构层级：05_Show - 表现层UI子组件
// 🔧 职责：显示单个扩展项的UI元素和交互
// ⚠️ 无业务逻辑，仅处理UI显示和用户交互

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SurvivalGame.Show.Inventory.Views.Components
{
    /// <summary>
    /// 扩展项UI组件
    /// 🔘 单个扩展项的UI表示
    /// 📊 显示扩展状态和基本信息
    /// </summary>
    public class ExpansionItemView : MonoBehaviour
    {
        // ============ 序列化字段 ============
        [Header("基础信息")]
        [SerializeField] private Image _iconImage;              // 扩展图标
        [SerializeField] private TMP_Text _nameText;            // 扩展名称
        [SerializeField] private TMP_Text _descriptionText;     // 扩展描述

        [Header("状态显示")]
        [SerializeField] private GameObject _completedIndicator; // 完成指示器
        [SerializeField] private GameObject _availableIndicator; // 可用指示器
        [SerializeField] private GameObject _lockedIndicator;    // 锁定指示器
        [SerializeField] private TMP_Text _statusText;          // 状态文本
        [SerializeField] private GameObject _inProgressIndicator; // 进行中指示器

        [Header("进度显示")]
        [SerializeField] private Slider _progressSlider;        // 进度条
        [SerializeField] private TMP_Text _progressText;        // 进度文本
        [SerializeField] private GameObject _progressPanel;     // 进度面板

        [Header("交互元素")]
        [SerializeField] private Button _mainButton;            // 主按钮
        [SerializeField] private Button _expandButton;          // 扩展按钮（如果分离）
        [SerializeField] private Image _backgroundImage;        // 背景图片（用于状态着色）

        [Header("视觉反馈")]
        [SerializeField] private Color _availableColor = Color.green;     // 可用状态颜色
        [SerializeField] private Color _lockedColor = Color.gray;         // 锁定状态颜色
        [SerializeField] private Color _completedColor = Color.blue;      // 完成状态颜色
        [SerializeField] private Color _inProgressColor = Color.yellow;   // 进行中颜色

        // ============ 内部状态 ============
        private string _expansionId;
        private bool _isSelected = false;

        // ============ 事件 ============
        public event System.Action OnClicked;          // 点击事件

        // ============ 公共API ============

        /// <summary>
        /// 初始化扩展项
        /// </summary>
        public void Initialize(
            string expansionId,
            string displayName,
            string description,
            bool isUnlocked,
            bool canStart,
            bool isCompleted,
            bool requirementsMet)
        {
            _expansionId = expansionId;

            // 更新UI元素
            UpdateName(displayName);
            UpdateDescription(description);
            UpdateStatus(isUnlocked, canStart, isCompleted, requirementsMet);

            // 设置按钮事件
            if (_mainButton != null)
            {
                _mainButton.onClick.RemoveAllListeners();
                _mainButton.onClick.AddListener(() => OnClicked?.Invoke());
            }

            if (_expandButton != null)
            {
                _expandButton.onClick.RemoveAllListeners();
                _expandButton.onClick.AddListener(() => OnClicked?.Invoke());
            }

            // 默认隐藏进度面板
            if (_progressPanel != null)
                _progressPanel.SetActive(false);
        }

        /// <summary>
        /// 设置扩展进度
        /// </summary>
        public void SetProgress(float progress)
        {
            if (_progressPanel != null)
                _progressPanel.SetActive(progress > 0f);

            if (_progressSlider != null)
                _progressSlider.value = progress;

            if (_progressText != null)
                _progressText.text = $"{(progress * 100):F0}%";
        }

        /// <summary>
        /// 更新名称
        /// </summary>
        public void UpdateName(string name)
        {
            if (_nameText != null)
                _nameText.text = name;
        }

        /// <summary>
        /// 更新描述
        /// </summary>
        public void UpdateDescription(string description)
        {
            if (_descriptionText != null)
                _descriptionText.text = description;
        }

        /// <summary>
        /// 更新状态
        /// </summary>
        public void UpdateStatus(bool isUnlocked, bool canStart, bool isCompleted, bool requirementsMet)
        {
            // 更新状态指示器
            if (_completedIndicator != null)
                _completedIndicator.SetActive(isCompleted);

            if (_availableIndicator != null)
                _availableIndicator.SetActive(!isCompleted && canStart && requirementsMet);

            if (_lockedIndicator != null)
                _lockedIndicator.SetActive(!isUnlocked || !requirementsMet);

            if (_inProgressIndicator != null)
                _inProgressIndicator.SetActive(false); // 将在SetInProgress中设置

            // 更新状态文本
            string statusText = GetStatusText(isUnlocked, canStart, isCompleted, requirementsMet);
            if (_statusText != null)
                _statusText.text = statusText;

            // 更新背景颜色
            UpdateBackgroundColor(isUnlocked, canStart, isCompleted);
        }

        /// <summary>
        /// 设置选中状态
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;

            // 更新选中状态的视觉反馈
            if (_backgroundImage != null)
            {
                var color = _backgroundImage.color;
                color.a = selected ? 0.8f : 0.4f;
                _backgroundImage.color = color;
            }
        }

        /// <summary>
        /// 设置进行中状态
        /// </summary>
        public void SetInProgress(bool inProgress, string progressText = null)
        {
            if (_inProgressIndicator != null)
                _inProgressIndicator.SetActive(inProgress);

            if (inProgress && !string.IsNullOrEmpty(progressText) && _statusText != null)
                _statusText.text = progressText;

            // 更新背景颜色
            if (inProgress && _backgroundImage != null)
            {
                _backgroundImage.color = _inProgressColor;
            }
        }

        /// <summary>
        /// 显示成功反馈
        /// </summary>
        public void ShowSuccessFeedback()
        {
            // TODO: 实现成功动画（如缩放、颜色变化等）
            Debug.Log($"[扩展项] {_expansionId} 成功反馈");
        }

        /// <summary>
        /// 显示失败反馈
        /// </summary>
        public void ShowFailedFeedback()
        {
            // TODO: 实现失败动画（如抖动、颜色闪烁等）
            Debug.Log($"[扩展项] {_expansionId} 失败反馈");
        }

        // ============ 内部方法 ============

        /// <summary>
        /// 获取状态文本
        /// </summary>
        private string GetStatusText(bool isUnlocked, bool canStart, bool isCompleted, bool requirementsMet)
        {
            if (isCompleted)
                return "已完成";

            if (!isUnlocked)
                return "未解锁";

            if (!requirementsMet)
                return "条件未满足";

            if (canStart)
                return "可开始";

            return "准备中";
        }

        /// <summary>
        /// 更新背景颜色
        /// </summary>
        private void UpdateBackgroundColor(bool isUnlocked, bool canStart, bool isCompleted)
        {
            if (_backgroundImage == null) return;

            if (isCompleted)
            {
                _backgroundImage.color = _completedColor;
            }
            else if (canStart && isUnlocked)
            {
                _backgroundImage.color = _availableColor;
            }
            else
            {
                _backgroundImage.color = _lockedColor;
            }
        }

        /// <summary>
        /// 设置图标
        /// </summary>
        public void SetIcon(Sprite icon)
        {
            if (_iconImage != null)
                _iconImage.sprite = icon;
        }

        /// <summary>
        /// 设置按钮交互性
        /// </summary>
        public void SetButtonInteractable(bool interactable)
        {
            if (_mainButton != null)
                _mainButton.interactable = interactable;

            if (_expandButton != null)
                _expandButton.interactable = interactable;
        }

        /// <summary>
        /// 获取扩展ID
        /// </summary>
        public string GetExpansionId()
        {
            return _expansionId;
        }

        /// <summary>
        /// 显示详细信息面板
        /// </summary>
        public void ShowDetails()
        {
            // 可以在这里实现详细信息的展开/折叠
            Debug.Log($"[扩展项] 显示详细信息: {_expansionId}");
        }

        /// <summary>
        /// 隐藏详细信息面板
        /// </summary>
        public void HideDetails()
        {
            Debug.Log($"[扩展项] 隐藏详细信息: {_expansionId}");
        }
    }
}