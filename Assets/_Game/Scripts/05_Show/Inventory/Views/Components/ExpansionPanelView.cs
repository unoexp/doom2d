// 📁 05_Show/Inventory/Views/Components/ExpansionPanelView.cs
// 扩展面板主UI组件
// 🏗️ 架构层级：05_Show - 表现层UI组件
// 🔧 职责：显示可用扩展列表、扩展详情和交互反馈
// ⚠️ 无业务逻辑，仅处理UI显示和用户交互

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SurvivalGame.Show.Inventory.Views.Components
{
    /// <summary>
    /// 扩展面板主UI组件
    /// 🖼️ 负责显示可用扩展列表和详情
    /// 🔄 通过ViewModel更新，不直接访问业务层
    /// </summary>
    public class ExpansionPanelView : MonoBehaviour
    {
        // ============ 序列化字段 ============
        [Header("核心组件")]
        [SerializeField] private GameObject _panelObject;          // 面板根对象
        [SerializeField] private Button _closeButton;              // 关闭按钮
        [SerializeField] private Button _expandAllButton;          // 一键扩展按钮
        [SerializeField] private Button _showRequirementsButton;   // 显示条件按钮

        [Header("信息显示")]
        [SerializeField] private TMP_Text _titleText;              // 面板标题
        [SerializeField] private TMP_Text _descriptionText;        // 面板描述
        [SerializeField] private TMP_Text _statsText;              // 统计信息

        [Header("扩展列表")]
        [SerializeField] private Transform _expansionListContainer; // 扩展项容器
        [SerializeField] private GameObject _expansionItemPrefab;   // 扩展项预制体

        [Header("详情面板")]
        [SerializeField] private GameObject _detailPanel;           // 详情面板
        [SerializeField] private TMP_Text _detailTitleText;        // 详情标题
        [SerializeField] private TMP_Text _detailDescriptionText;  // 详情描述
        [SerializeField] private Transform _requirementContainer;  // 条件项容器
        [SerializeField] private GameObject _requirementItemPrefab; // 条件项预制体
        [SerializeField] private Button _detailExpandButton;       // 详情中的扩展按钮

        [Header("状态显示")]
        [SerializeField] private GameObject _progressPanel;        // 进度面板
        [SerializeField] private Slider _progressSlider;           // 进度条
        [SerializeField] private TMP_Text _progressText;           // 进度文本
        [SerializeField] private TMP_Text _timeRemainingText;      // 剩余时间文本
        [SerializeField] private GameObject _activeStatusPanel;    // 活动状态面板
        [SerializeField] private TMP_Text _activeStatusText;       // 活动状态文本

        // ============ 内部状态 ============
        private Dictionary<string, ExpansionItemView> _expansionItems = new Dictionary<string, ExpansionItemView>();
        private Dictionary<string, ExpansionRequirementView> _requirementViews = new Dictionary<string, ExpansionRequirementView>();
        private string _selectedExpansionId;
        private bool _isPanelOpen = false;

        // ============ ViewModel引用 ============
        private ExpansionConfigViewModel _currentViewModel;

        // ============ 事件 ============
        public event Action<string> OnExpansionSelected;           // 扩展项被选中
        public event Action<string> OnExpandButtonClicked;         // 扩展按钮被点击
        public event Action<bool> OnPanelVisibilityChanged;        // 面板可见性变化

        // ============ 生命周期 ============

        private void Awake()
        {
            // 初始化UI状态
            _panelObject.SetActive(false);
            _detailPanel.SetActive(false);
            _progressPanel.SetActive(false);
            _activeStatusPanel.SetActive(false);
        }

        private void Start()
        {
            // 绑定按钮事件
            if (_closeButton != null)
                _closeButton.onClick.AddListener(ClosePanel);

            if (_expandAllButton != null)
                _expandAllButton.onClick.AddListener(OnExpandAllClicked);

            if (_showRequirementsButton != null)
                _showRequirementsButton.onClick.AddListener(OnShowRequirementsClicked);

            if (_detailExpandButton != null)
                _detailExpandButton.onClick.AddListener(OnDetailExpandClicked);

            // 订阅ViewModel事件
            SubscribeToViewModelEvents();
        }

        private void OnDestroy()
        {
            // 清理按钮事件
            if (_closeButton != null)
                _closeButton.onClick.RemoveAllListeners();

            if (_expandAllButton != null)
                _expandAllButton.onClick.RemoveAllListeners();

            if (_showRequirementsButton != null)
                _showRequirementsButton.onClick.RemoveAllListeners();

            if (_detailExpandButton != null)
                _detailExpandButton.onClick.RemoveAllListeners();

            // 清理UI项
            ClearAllExpansionItems();
        }

        // ============ 公共API ============

        /// <summary>
        /// 打开扩展面板
        /// </summary>
        public void OpenPanel(ExpansionConfigViewModel viewModel)
        {
            if (viewModel == null) return;

            _currentViewModel = viewModel;
            _panelObject.SetActive(true);
            _isPanelOpen = true;

            // 更新UI
            UpdatePanelContent(viewModel);
            OnPanelVisibilityChanged?.Invoke(true);
        }

        /// <summary>
        /// 关闭扩展面板
        /// </summary>
        public void ClosePanel()
        {
            _panelObject.SetActive(false);
            _isPanelOpen = false;
            _selectedExpansionId = null;
            _detailPanel.SetActive(false);

            OnPanelVisibilityChanged?.Invoke(false);
        }

        /// <summary>
        /// 切换面板显示状态
        /// </summary>
        public void TogglePanel(ExpansionConfigViewModel viewModel)
        {
            if (_isPanelOpen)
            {
                ClosePanel();
            }
            else
            {
                OpenPanel(viewModel);
            }
        }

        /// <summary>
        /// 更新面板内容
        /// </summary>
        public void UpdatePanelContent(ExpansionConfigViewModel viewModel)
        {
            if (viewModel == null || !_isPanelOpen) return;

            _currentViewModel = viewModel;

            // 更新标题和描述
            if (_titleText != null)
                _titleText.text = "背包扩展";

            if (_descriptionText != null)
                _descriptionText.text = "提升背包容量，优化存储空间";

            // 更新统计信息
            UpdateStatsDisplay(viewModel);

            // 更新扩展列表
            UpdateExpansionList(viewModel);

            // 如果有选中的扩展项，更新详情
            if (!string.IsNullOrEmpty(_selectedExpansionId))
            {
                UpdateDetailPanel(viewModel);
            }

            // 更新进度面板
            UpdateProgressPanel(viewModel);
        }

        /// <summary>
        /// 显示扩展进度
        /// </summary>
        public void ShowProgress(string expansionId, float progress, string statusText, string timeRemaining)
        {
            if (!_isPanelOpen) return;

            _progressPanel.SetActive(true);
            _progressSlider.value = progress;

            if (_progressText != null)
                _progressText.text = statusText;

            if (_timeRemainingText != null)
                _timeRemainingText.text = timeRemaining;

            // 更新对应的扩展项进度显示
            if (_expansionItems.TryGetValue(expansionId, out var itemView))
            {
                itemView.SetProgress(progress);
            }
        }

        /// <summary>
        /// 隐藏扩展进度
        /// </summary>
        public void HideProgress()
        {
            _progressPanel.SetActive(false);

            // 重置所有扩展项的进度显示
            foreach (var item in _expansionItems.Values)
            {
                item.SetProgress(0f);
            }
        }

        /// <summary>
        /// 显示活动状态（如：扩展中，回滚中等）
        /// </summary>
        public void ShowActiveStatus(string statusText, Color statusColor)
        {
            _activeStatusPanel.SetActive(true);
            _activeStatusText.text = statusText;
            _activeStatusText.color = statusColor;
        }

        /// <summary>
        /// 隐藏活动状态
        /// </summary>
        public void HideActiveStatus()
        {
            _activeStatusPanel.SetActive(false);
        }

        /// <summary>
        /// 显示扩展成功反馈
        /// </summary>
        public void ShowExpansionSuccessFeedback(string expansionName, int capacityIncrease)
        {
            // TODO: 实现视觉反馈（如粒子效果、动画等）
            Debug.Log($"[扩展成功] {expansionName} 容量增加 {capacityIncrease}格");
        }

        /// <summary>
        /// 显示扩展失败反馈
        /// </summary>
        public void ShowExpansionFailedFeedback(string reason)
        {
            // TODO: 实现视觉反馈（如错误提示、震动等）
            Debug.Log($"[扩展失败] {reason}");
        }

        // ============ 内部方法 ============

        /// <summary>
        /// 更新统计信息显示
        /// </summary>
        private void UpdateStatsDisplay(ExpansionConfigViewModel viewModel)
        {
            if (_statsText == null) return;

            var stats = viewModel.GetExpansionStats();
            _statsText.text = $"已完成: {viewModel.CompletedLevelsCount}/{viewModel.TotalLevelsCount}\n" +
                              $"可用扩展: {viewModel.AvailableLevelsCount}\n" +
                              $"总容量: {stats.CurrentMainSlots + stats.CurrentQuickSlots}格";
        }

        /// <summary>
        /// 更新扩展列表
        /// </summary>
        private void UpdateExpansionList(ExpansionConfigViewModel viewModel)
        {
            // 清除现有项
            ClearAllExpansionItems();

            if (_expansionListContainer == null || _expansionItemPrefab == null) return;

            // 创建新的扩展项
            foreach (var level in viewModel.ExpansionLevels)
            {
                var itemObj = Instantiate(_expansionItemPrefab, _expansionListContainer);
                var itemView = itemObj.GetComponent<ExpansionItemView>();

                if (itemView != null)
                {
                    // 配置扩展项
                    itemView.Initialize(
                        level.LevelId,
                        level.DisplayName,
                        level.Description,
                        !level.IsLocked,
                        level.CanStartExpansion(),
                        level.IsCompleted,
                        level.AllRequirementsMet
                    );

                    // 绑定点击事件
                    itemView.OnClicked += () => OnExpansionItemClicked(level.LevelId);

                    // 添加到字典
                    _expansionItems[level.LevelId] = itemView;
                }
            }
        }

        /// <summary>
        /// 更新详情面板
        /// </summary>
        private void UpdateDetailPanel(ExpansionConfigViewModel viewModel)
        {
            if (string.IsNullOrEmpty(_selectedExpansionId)) return;

            var level = viewModel.GetLevel(_selectedExpansionId);
            if (level == null)
            {
                _detailPanel.SetActive(false);
                return;
            }

            _detailPanel.SetActive(true);

            // 更新详情内容
            if (_detailTitleText != null)
                _detailTitleText.text = level.DisplayName;

            if (_detailDescriptionText != null)
                {
                string effectDesc = $"效果: {level.EffectDescription}\n" +
                                   $"主背包 +{level.AdditionalSlots}格";

                if (level.WeightLimitBoost > 0)
                    effectDesc += $", 负重 +{level.WeightLimitBoost}";

                if (level.UnlockSpecialSlots)
                    effectDesc += $", 特殊槽位解锁";

                _detailDescriptionText.text = $"{level.Description}\n\n{effectDesc}";
            }

            // 更新条件列表
            UpdateRequirementsList(level);

            // 更新扩展按钮状态
            if (_detailExpandButton != null)
            {
                _detailExpandButton.interactable = level.CanStartExpansion() && !level.IsCompleted;
                var buttonText = _detailExpandButton.GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                {
                    buttonText.text = level.IsCompleted ? "已完成" : "开始扩展";
                }
            }
        }

        /// <summary>
        /// 更新条件列表
        /// </summary>
        private void UpdateRequirementsList(ExpansionLevelViewModel level)
        {
            if (_requirementContainer == null || _requirementItemPrefab == null) return;

            // 清除现有条件项
            foreach (var view in _requirementViews.Values)
            {
                Destroy(view.gameObject);
            }
            _requirementViews.Clear();

            // 创建新的条件项
            foreach (var requirement in level.Requirements)
            {
                var itemObj = Instantiate(_requirementItemPrefab, _requirementContainer);
                var requirementView = itemObj.GetComponent<ExpansionRequirementView>();

                if (requirementView != null)
                {
                    requirementView.Initialize(
                        requirement.Type.ToString(),
                        requirement.DisplayText,
                        requirement.RequiredValue,
                        requirement.IsMet,
                        requirement.ProgressPercentage,
                        requirement.StatusText
                    );

                    _requirementViews[requirement.Type.ToString()] = requirementView;
                }
            }
        }

        /// <summary>
        /// 更新进度面板
        /// </summary>
        private void UpdateProgressPanel(ExpansionConfigViewModel viewModel)
        {
            // 检查是否有正在进行的扩展
            if (!string.IsNullOrEmpty(viewModel.CurrentExpansionLevelId))
            {
                var currentLevel = viewModel.GetLevel(viewModel.CurrentExpansionLevelId);
                if (currentLevel != null && currentLevel.IsInProgress)
                {
                    ShowProgress(
                        currentLevel.LevelId,
                        viewModel.CurrentExpansionProgress,
                        currentLevel.StateText,
                        FormatTimeRemaining(viewModel.CurrentExpansionRemainingTime)
                    );
                }
            }
            else
            {
                HideProgress();
            }
        }

        /// <summary>
        /// 订阅ViewModel事件
        /// </summary>
        private void SubscribeToViewModelEvents()
        {
            // 当ViewModel更新时，这里会收到通知
            // 实际项目中，需要通过Presenter连接ViewModel和View
        }

        /// <summary>
        /// 清除所有扩展项
        /// </summary>
        private void ClearAllExpansionItems()
        {
            foreach (var item in _expansionItems.Values)
            {
                if (item != null && item.gameObject != null)
                {
                    Destroy(item.gameObject);
                }
            }
            _expansionItems.Clear();
        }

        // ============ 事件处理方法 ============

        private void OnExpansionItemClicked(string expansionId)
        {
            _selectedExpansionId = expansionId;
            OnExpansionSelected?.Invoke(expansionId);

            // 更新详情面板
            if (_currentViewModel != null)
            {
                UpdateDetailPanel(_currentViewModel);
            }
        }

        private void OnExpandAllClicked()
        {
            // 尝试执行所有可用扩展
            Debug.Log("[扩展面板] 一键扩展按钮被点击");
            // 实际逻辑应该在Presenter中处理
        }

        private void OnShowRequirementsClicked()
        {
            // 显示/隐藏条件详细信息
            Debug.Log("[扩展面板] 显示条件按钮被点击");
            // 可以在这里切换条件列表的可见性
        }

        private void OnDetailExpandClicked()
        {
            if (!string.IsNullOrEmpty(_selectedExpansionId))
            {
                OnExpandButtonClicked?.Invoke(_selectedExpansionId);
            }
        }

        // ============ 工具方法 ============

        private string FormatTimeRemaining(float seconds)
        {
            if (seconds <= 0) return "已完成";

            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
            if (timeSpan.TotalHours >= 1)
            {
                return $"{timeSpan.Hours}小时{timeSpan.Minutes}分";
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                return $"{timeSpan.Minutes}分{timeSpan.Seconds}秒";
            }
            else
            {
                return $"{timeSpan.Seconds}秒";
            }
        }
    }
}