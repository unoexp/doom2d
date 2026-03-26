// 📁 05_Show/Inventory/Views/Components/ExpansionRequirementView.cs
// 扩展条件项UI组件
// 🏗️ 架构层级：05_Show - 表现层UI子组件
// 🔧 职责：显示单个扩展条件的UI元素和状态
// ⚠️ 无业务逻辑，仅处理UI显示

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SurvivalGame.Show.Inventory.Views.Components
{
    /// <summary>
    /// 扩展条件项UI组件
    /// ✅ 显示单个扩展条件的满足状态
    /// 📊 支持进度条显示和文本描述
    /// </summary>
    public class ExpansionRequirementView : MonoBehaviour
    {
        // ============ 序列化字段 ============
        [Header("基本信息")]
        [SerializeField] private TMP_Text _typeText;            // 条件类型文本
        [SerializeField] private TMP_Text _descriptionText;     // 条件描述文本
        [SerializeField] private TMP_Text _targetText;          // 目标值文本

        [Header("状态显示")]
        [SerializeField] private GameObject _metIndicator;      // 满足条件指示器
        [SerializeField] private GameObject _unmetIndicator;    // 未满足条件指示器
        [SerializeField] private TMP_Text _statusText;          // 状态文本
        [SerializeField] private Image _statusIcon;             // 状态图标

        [Header("进度显示")]
        [SerializeField] private Slider _progressSlider;        // 进度条
        [SerializeField] private TMP_Text _progressText;        // 进度文本
        [SerializeField] private GameObject _progressPanel;     // 进度面板

        [Header("资源显示")]
        [SerializeField] private Image _resourceIcon;           // 资源图标
        [SerializeField] private TMP_Text _resourceAmountText;  // 资源数量文本
        [SerializeField] private GameObject _resourcePanel;     // 资源面板

        [Header("视觉反馈")]
        [SerializeField] private Color _metColor = Color.green;         // 满足状态颜色
        [SerializeField] private Color _unmetColor = Color.red;         // 未满足状态颜色
        [SerializeField] private Color _progressColor = Color.yellow;   // 进行中颜色
        [SerializeField] private Sprite _metIcon;              // 满足图标
        [SerializeField] private Sprite _unmetIcon;            // 未满足图标

        // ============ 内部状态 ============
        private string _requirementId;

        // ============ 公共API ============

        /// <summary>
        /// 初始化条件项
        /// </summary>
        public void Initialize(
            string requirementType,
            string displayText,
            int requiredValue,
            bool isMet,
            float progressPercentage,
            string statusText)
        {
            _requirementId = requirementType;

            // 更新UI元素
            UpdateTypeText(requirementType);
            UpdateDescription(displayText);
            UpdateTargetValue(requiredValue);
            UpdateStatus(isMet, progressPercentage, statusText);

            // 根据条件类型显示不同的面板
            UpdateDisplayType(requirementType);
        }

        /// <summary>
        /// 更新类型文本
        /// </summary>
        public void UpdateTypeText(string typeText)
        {
            if (_typeText != null)
                _typeText.text = GetTypeDisplayName(typeText);
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
        /// 更新目标值
        /// </summary>
        public void UpdateTargetValue(int requiredValue)
        {
            if (_targetText != null)
                _targetText.text = $"需要: {requiredValue}";
        }

        /// <summary>
        /// 更新状态
        /// </summary>
        public void UpdateStatus(bool isMet, float progressPercentage, string statusText)
        {
            // 更新状态指示器
            if (_metIndicator != null)
                _metIndicator.SetActive(isMet);

            if (_unmetIndicator != null)
                _unmetIndicator.SetActive(!isMet);

            // 更新状态文本
            if (_statusText != null)
            {
                if (!string.IsNullOrEmpty(statusText))
                    _statusText.text = statusText;
                else
                    _statusText.text = isMet ? "已满足" : "未满足";
            }

            // 更新状态图标
            if (_statusIcon != null)
            {
                _statusIcon.sprite = isMet ? _metIcon : _unmetIcon;
                _statusIcon.color = isMet ? _metColor : _unmetColor;
            }

            // 更新进度显示
            UpdateProgress(progressPercentage, isMet);
        }

        /// <summary>
        /// 更新进度
        /// </summary>
        public void UpdateProgress(float progressPercentage, bool isMet)
        {
            if (_progressPanel != null)
            {
                bool showProgress = progressPercentage > 0f && progressPercentage < 1f;
                _progressPanel.SetActive(showProgress);

                if (showProgress)
                {
                    if (_progressSlider != null)
                    {
                        _progressSlider.value = progressPercentage;
                        _progressSlider.fillRect.GetComponent<Image>().color =
                            isMet ? _metColor : _progressColor;
                    }

                    if (_progressText != null)
                        _progressText.text = $"{(progressPercentage * 100):F0}%";
                }
            }
        }

        /// <summary>
        /// 设置资源信息
        /// </summary>
        public void SetResourceInfo(Sprite icon, int currentAmount, int requiredAmount)
        {
            if (_resourcePanel != null)
                _resourcePanel.SetActive(true);

            if (_resourceIcon != null && icon != null)
                _resourceIcon.sprite = icon;

            if (_resourceAmountText != null)
                _resourceAmountText.text = $"{currentAmount}/{requiredAmount}";
        }

        /// <summary>
        /// 显示成功反馈
        /// </summary>
        public void ShowSuccessFeedback()
        {
            // TODO: 实现成功动画（如缩放、颜色变化等）
            Debug.Log($"[条件项] {_requirementId} 满足条件反馈");
        }

        /// <summary>
        /// 显示失败反馈
        /// </summary>
        public void ShowFailedFeedback(string reason)
        {
            // TODO: 实现失败动画（如抖动、颜色闪烁等）
            Debug.Log($"[条件项] {_requirementId} 未满足条件反馈: {reason}");
        }

        /// <summary>
        /// 设置目标ID（用于技能/任务等特定条件）
        /// </summary>
        public void SetTargetId(string targetId)
        {
            // 可以在这里添加目标ID的特定显示逻辑
            Debug.Log($"[条件项] 目标ID: {targetId}");
        }

        // ============ 内部方法 ============

        /// <summary>
        /// 获取类型显示名称
        /// </summary>
        private string GetTypeDisplayName(string type)
        {
            switch (type)
            {
                case "ResourceCost":
                    return "资源消耗";
                case "SkillLevel":
                    return "技能等级";
                case "PlayerLevel":
                    return "玩家等级";
                case "QuestCompletion":
                    return "任务完成";
                default:
                    return type;
            }
        }

        /// <summary>
        /// 更新显示类型
        /// </summary>
        private void UpdateDisplayType(string requirementType)
        {
            // 根据条件类型显示不同的UI元素
            bool isResource = requirementType == "ResourceCost";
            bool hasProgress = requirementType == "SkillLevel" || requirementType == "PlayerLevel";

            // 显示/隐藏资源面板
            if (_resourcePanel != null)
                _resourcePanel.SetActive(isResource);

            // 显示/隐藏进度面板
            if (_progressPanel != null)
            {
                // 进度面板的显示逻辑由UpdateProgress控制
                // 这里只是设置默认状态
                _progressPanel.SetActive(false);
            }

            // 更新目标文本位置
            if (_targetText != null)
            {
                // 资源类型显示在资源面板中，其他类型显示在常规位置
                _targetText.gameObject.SetActive(!isResource);
            }
        }

        /// <summary>
        /// 设置图标
        /// </summary>
        public void SetIcon(Sprite icon)
        {
            if (_statusIcon != null && icon != null)
                _statusIcon.sprite = icon;
        }

        /// <summary>
        /// 获取条件ID
        /// </summary>
        public string GetRequirementId()
        {
            return _requirementId;
        }

        /// <summary>
        /// 设置是否可见
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        /// <summary>
        /// 设置高亮状态
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            // TODO: 实现高亮效果（如边框发光、背景颜色变化等）
            if (highlighted)
            {
                Debug.Log($"[条件项] {_requirementId} 高亮显示");
            }
        }

        /// <summary>
        /// 设置互动性（对于可点击的条件项）
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            var button = GetComponent<Button>();
            if (button != null)
                button.interactable = interactable;
        }
    }
}