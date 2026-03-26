// 📁 05_Show/Inventory/Managers/ExpansionUIManager.cs
// 扩展UI管理器
// 🏗️ 架构层级：05_Show - 表现层管理器
// 🔧 职责：协调扩展UI生命周期，管理UI状态同步
// ⚠️ 无业务逻辑，仅处理UI协调

using System;
using System.Collections.Generic;
using UnityEngine;
using SurvivalGame.Show.Inventory.ViewModels;
using SurvivalGame.Show.Inventory.Views.Components;

namespace SurvivalGame.Show.Inventory.Managers
{
    /// <summary>
    /// 扩展UI管理器
    /// 🎮 协调多个扩展UI组件
    /// 🔄 管理UI状态同步和生命周期
    /// </summary>
    public class ExpansionUIManager : MonoBehaviour
    {
        // ============ 序列化字段 ============
        [Header("UI组件引用")]
        [SerializeField] private ExpansionPanelView _expansionPanel;
        [SerializeField] private GameObject _expansionPanelPrefab; // 备用：动态创建

        [Header("配置")]
        [SerializeField] private bool _createPanelIfMissing = true; // 如果面板缺失则创建
        [SerializeField] private Canvas _targetCanvas;              // 目标画布

        [Header("动画")]
        [SerializeField] private float _panelOpenDuration = 0.3f;   // 面板打开动画时长
        [SerializeField] private float _panelCloseDuration = 0.2f;  // 面板关闭动画时长

        // ============ 内部状态 ============
        private Dictionary<string, ExpansionItemView> _expansionItemViews = new Dictionary<string, ExpansionItemView>();
        private ExpansionConfigViewModel _currentViewModel;
        private bool _isPanelOpen = false;
        private bool _isPanelAnimating = false;

        // ============ 事件 ============
        public event Action<string> OnExpansionRequested;      // 请求执行扩展
        public event Action<string> OnExpansionCancelled;      // 取消扩展
        public event Action OnPanelOpened;                     // 面板打开
        public event Action OnPanelClosed;                     // 面板关闭

        // ============ 生命周期 ============

        private void Awake()
        {
            // 确保有有效的面板引用
            EnsurePanelExists();
        }

        private void Start()
        {
            // 初始化面板状态
            if (_expansionPanel != null)
            {
                _expansionPanel.OnPanelVisibilityChanged += OnPanelVisibilityChanged;
                _expansionPanel.OnExpansionSelected += OnExpansionSelected;
                _expansionPanel.OnExpandButtonClicked += OnExpandButtonClicked;

                // 初始状态：关闭
                _expansionPanel.ClosePanel();
            }

            // 订阅ViewModel更新事件
            SubscribeToViewModelEvents();
        }

        private void OnDestroy()
        {
            // 清理事件订阅
            if (_expansionPanel != null)
            {
                _expansionPanel.OnPanelVisibilityChanged -= OnPanelVisibilityChanged;
                _expansionPanel.OnExpansionSelected -= OnExpansionSelected;
                _expansionPanel.OnExpandButtonClicked -= OnExpandButtonClicked;
            }
        }

        // ============ 公共API ============

        /// <summary>
        /// 打开扩展面板
        /// </summary>
        public void OpenPanel(ExpansionConfigViewModel viewModel)
        {
            if (_isPanelAnimating || _isPanelOpen) return;

            _currentViewModel = viewModel;
            _isPanelAnimating = true;

            if (_expansionPanel != null)
            {
                _expansionPanel.OpenPanel(viewModel);
                StartPanelOpenAnimation();
            }
            else
            {
                Debug.LogWarning("[ExpansionUIManager] 扩展面板不存在");
                _isPanelAnimating = false;
            }
        }

        /// <summary>
        /// 关闭扩展面板
        /// </summary>
        public void ClosePanel()
        {
            if (_isPanelAnimating || !_isPanelOpen) return;

            _isPanelAnimating = true;

            if (_expansionPanel != null)
            {
                _expansionPanel.ClosePanel();
                StartPanelCloseAnimation();
            }
        }

        /// <summary>
        /// 切换扩展面板
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
            if (_expansionPanel == null || !_isPanelOpen) return;

            _currentViewModel = viewModel;
            _expansionPanel.UpdatePanelContent(viewModel);
        }

        /// <summary>
        /// 显示扩展进度
        /// </summary>
        public void ShowExpansionProgress(string expansionId, float progress, string statusText, float remainingTime)
        {
            if (_expansionPanel == null) return;

            string timeRemaining = FormatTimeRemaining(remainingTime);
            _expansionPanel.ShowProgress(expansionId, progress, statusText, timeRemaining);

            // 同时更新对应的扩展项
            if (_expansionItemViews.TryGetValue(expansionId, out var itemView))
            {
                itemView.SetInProgress(true, statusText);
                itemView.SetProgress(progress);
            }
        }

        /// <summary>
        /// 隐藏扩展进度
        /// </summary>
        public void HideExpansionProgress()
        {
            if (_expansionPanel != null)
            {
                _expansionPanel.HideProgress();
            }

            // 重置所有扩展项的进行中状态
            foreach (var itemView in _expansionItemViews.Values)
            {
                itemView.SetInProgress(false);
                itemView.SetProgress(0f);
            }
        }

        /// <summary>
        /// 显示扩展成功反馈
        /// </summary>
        public void ShowExpansionSuccess(string expansionId, string expansionName, int capacityIncrease)
        {
            if (_expansionPanel != null)
            {
                _expansionPanel.ShowExpansionSuccessFeedback(expansionName, capacityIncrease);
            }

            if (_expansionItemViews.TryGetValue(expansionId, out var itemView))
            {
                itemView.ShowSuccessFeedback();
                itemView.SetInProgress(false);
                itemView.SetProgress(1f);
            }
        }

        /// <summary>
        /// 显示扩展失败反馈
        /// </summary>
        public void ShowExpansionFailed(string expansionId, string reason)
        {
            if (_expansionPanel != null)
            {
                _expansionPanel.ShowExpansionFailedFeedback(reason);
            }

            if (_expansionItemViews.TryGetValue(expansionId, out var itemView))
            {
                itemView.ShowFailedFeedback();
                itemView.SetInProgress(false);
                itemView.SetProgress(0f);
            }
        }

        /// <summary>
        /// 显示条件验证结果
        /// </summary>
        public void ShowValidationResult(string expansionId, bool allMet, List<string> failedConditions)
        {
            if (!_isPanelOpen) return;

            if (allMet)
            {
                ShowNotification($"扩展条件验证通过");
            }
            else
            {
                string failedText = string.Join("\n", failedConditions);
                ShowNotification($"条件不满足:\n{failedText}");
            }
        }

        /// <summary>
        /// 显示资源消耗结果
        /// </summary>
        public void ShowConsumptionResult(string expansionId, bool allConsumed, List<string> failedResources)
        {
            if (!_isPanelOpen) return;

            if (allConsumed)
            {
                ShowNotification($"资源消耗完成");
            }
            else
            {
                string failedText = string.Join("\n", failedResources);
                ShowNotification($"资源不足:\n{failedText}");
            }
        }

        /// <summary>
        /// 显示活动状态
        /// </summary>
        public void ShowActiveStatus(string statusText, Color statusColor)
        {
            if (_expansionPanel != null)
            {
                _expansionPanel.ShowActiveStatus(statusText, statusColor);
            }
        }

        /// <summary>
        /// 隐藏活动状态
        /// </summary>
        public void HideActiveStatus()
        {
            if (_expansionPanel != null)
            {
                _expansionPanel.HideActiveStatus();
            }
        }

        /// <summary>
        /// 获取当前选中的扩展ID
        /// </summary>
        public string GetSelectedExpansionId()
        {
            // 如果面板有当前选中的扩展项，返回其ID
            // 实际实现可能需要从面板获取
            return null;
        }

        /// <summary>
        /// 检查面板是否打开
        /// </summary>
        public bool IsPanelOpen()
        {
            return _isPanelOpen;
        }

        // ============ 内部方法 ============

        /// <summary>
        /// 确保面板存在
        /// </summary>
        private void EnsurePanelExists()
        {
            if (_expansionPanel == null && _createPanelIfMissing)
            {
                if (_expansionPanelPrefab != null && _targetCanvas != null)
                {
                    var panelObj = Instantiate(_expansionPanelPrefab, _targetCanvas.transform);
                    _expansionPanel = panelObj.GetComponent<ExpansionPanelView>();

                    if (_expansionPanel == null)
                    {
                        Debug.LogError("[ExpansionUIManager] 预制体缺少ExpansionPanelView组件");
                        Destroy(panelObj);
                    }
                }
                else
                {
                    Debug.LogWarning("[ExpansionUIManager] 无法创建扩展面板：缺少预制体或画布");
                }
            }
        }

        /// <summary>
        /// 订阅ViewModel事件
        /// </summary>
        private void SubscribeToViewModelEvents()
        {
            // 这里应该订阅ViewModel的事件通知
            // 实际实现中，ViewModel会通过Presenter通知UI更新
        }

        /// <summary>
        /// 开始面板打开动画
        /// </summary>
        private void StartPanelOpenAnimation()
        {
            // TODO: 实现面板打开动画（如渐入、滑动等）
            // 这里暂时用简单的延迟模拟
            Invoke(nameof(FinishPanelOpenAnimation), _panelOpenDuration);
        }

        /// <summary>
        /// 完成面板打开动画
        /// </summary>
        private void FinishPanelOpenAnimation()
        {
            _isPanelOpen = true;
            _isPanelAnimating = false;
            OnPanelOpened?.Invoke();
        }

        /// <summary>
        /// 开始面板关闭动画
        /// </summary>
        private void StartPanelCloseAnimation()
        {
            // TODO: 实现面板关闭动画
            Invoke(nameof(FinishPanelCloseAnimation), _panelCloseDuration);
        }

        /// <summary>
        /// 完成面板关闭动画
        /// </summary>
        private void FinishPanelCloseAnimation()
        {
            _isPanelOpen = false;
            _isPanelAnimating = false;
            OnPanelClosed?.Invoke();
        }

        /// <summary>
        /// 格式化剩余时间
        /// </summary>
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

        /// <summary>
        /// 显示通知
        /// </summary>
        private void ShowNotification(string message)
        {
            // 通过EventBus发布通知事件
            // 实际项目中应该使用项目的事件系统
            Debug.Log($"[扩展UI] {message}");
        }

        // ============ 事件处理方法 ============

        private void OnPanelVisibilityChanged(bool isVisible)
        {
            // 面板可见性变化事件
            Debug.Log($"[ExpansionUIManager] 面板可见性: {isVisible}");
        }

        private void OnExpansionSelected(string expansionId)
        {
            // 扩展项被选中
            Debug.Log($"[ExpansionUIManager] 扩展项选中: {expansionId}");
        }

        private void OnExpandButtonClicked(string expansionId)
        {
            // 扩展按钮被点击，通知外部系统
            Debug.Log($"[ExpansionUIManager] 请求执行扩展: {expansionId}");
            OnExpansionRequested?.Invoke(expansionId);
        }

        /// <summary>
        /// 注册扩展项视图
        /// </summary>
        public void RegisterExpansionItemView(string expansionId, ExpansionItemView itemView)
        {
            if (!_expansionItemViews.ContainsKey(expansionId))
            {
                _expansionItemViews[expansionId] = itemView;
            }
        }

        /// <summary>
        /// 取消注册扩展项视图
        /// </summary>
        public void UnregisterExpansionItemView(string expansionId)
        {
            if (_expansionItemViews.ContainsKey(expansionId))
            {
                _expansionItemViews.Remove(expansionId);
            }
        }

        /// <summary>
        /// 更新扩展项状态
        /// </summary>
        public void UpdateExpansionItemStatus(string expansionId, bool isAvailable, bool isCompleted, bool isInProgress)
        {
            if (_expansionItemViews.TryGetValue(expansionId, out var itemView))
            {
                // 这里需要更详细的状态信息来调用itemView的更新方法
                // 实际实现中应该从ViewModel获取完整状态
                itemView.SetInProgress(isInProgress);
            }
        }

        /// <summary>
        /// 清除所有注册的扩展项视图
        /// </summary>
        public void ClearAllExpansionItemViews()
        {
            _expansionItemViews.Clear();
        }
    }
}