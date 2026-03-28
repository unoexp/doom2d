// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/04_Gameplay/World/TemperatureSystem.cs
// 温度系统。连接环境温度→玩家体温，影响生存属性衰减。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// 温度影响系统。
///
/// 核心职责：
///   · 监听 AmbientTemperatureChangedEvent（由 WeatherSystem 发布）
///   · 根据环境温度计算玩家体温变化速率
///   · 通过 SurvivalStatusSystem.ModifyAttribute 影响体温属性
///   · 在庇护所内减缓或停止体温衰减（通过区域检测）
///
/// 设计说明：
///   · 体温舒适区间为 [ComfortMin, ComfortMax]，在此区间内体温自动回归正常
///   · 低于舒适区间：体温持续下降（失温风险）
///   · 高于舒适区间：体温持续上升（中暑风险）
///   · 暴风雪时体温衰减 ×1.8（对应 GDD 4.6 天气系统设计）
/// </summary>
public class TemperatureSystem : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("舒适温度区间（°C）")]
    [Tooltip("低于此温度开始体温下降")]
    [SerializeField] private float _comfortMin = 15f;

    [Tooltip("高于此温度开始体温上升")]
    [SerializeField] private float _comfortMax = 35f;

    [Header("体温变化速率")]
    [Tooltip("每秒体温变化的基础速率（点/°C偏差）")]
    [SerializeField] private float _tempChangeRate = 0.05f;

    [Tooltip("舒适区间内体温自动恢复速率（点/秒）")]
    [SerializeField] private float _recoveryRate = 0.5f;

    [Tooltip("体温正常值")]
    [SerializeField] private float _normalBodyTemp = 50f;

    [Header("庇护所修正")]
    [Tooltip("庇护所内体温衰减倍率（0=完全不衰减）")]
    [SerializeField] private float _shelterMultiplier = 0f;

    // ══════════════════════════════════════════════════════
    // 运行时状态
    // ══════════════════════════════════════════════════════

    private float _currentAmbientTemp;
    private float _feelsLikeTemp;
    private bool _isInShelter;
    private SurvivalStatusSystem _survivalSystem;

    // ══════════════════════════════════════════════════════
    // 属性
    // ══════════════════════════════════════════════════════

    public float CurrentAmbientTemperature => _currentAmbientTemp;
    public float FeelsLikeTemperature => _feelsLikeTemp;
    public bool IsInShelter => _isInShelter;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        ServiceLocator.Register<TemperatureSystem>(this);
    }

    private void Start()
    {
        _survivalSystem = ServiceLocator.Get<SurvivalStatusSystem>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<AmbientTemperatureChangedEvent>(OnAmbientTempChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<AmbientTemperatureChangedEvent>(OnAmbientTempChanged);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<TemperatureSystem>();
    }

    private void Update()
    {
        if (_survivalSystem == null) return;
        ApplyTemperatureEffect(Time.deltaTime);
    }

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>设置玩家是否在庇护所内</summary>
    public void SetInShelter(bool inShelter)
    {
        _isInShelter = inShelter;
    }

    // ══════════════════════════════════════════════════════
    // 事件处理
    // ══════════════════════════════════════════════════════

    private void OnAmbientTempChanged(AmbientTemperatureChangedEvent evt)
    {
        _currentAmbientTemp = evt.Temperature;
        _feelsLikeTemp = evt.FeelsLikeTemperature;
    }

    // ══════════════════════════════════════════════════════
    // 温度效果计算
    // ══════════════════════════════════════════════════════

    /// <summary>每帧计算并施加体温变化</summary>
    private void ApplyTemperatureEffect(float deltaTime)
    {
        float effectiveTemp = _feelsLikeTemp;
        float tempDelta;

        if (effectiveTemp < _comfortMin)
        {
            // 环境过冷：体温下降
            float deviation = _comfortMin - effectiveTemp;
            tempDelta = -deviation * _tempChangeRate * deltaTime;
        }
        else if (effectiveTemp > _comfortMax)
        {
            // 环境过热：体温上升（超出正常值也有危险）
            float deviation = effectiveTemp - _comfortMax;
            tempDelta = deviation * _tempChangeRate * deltaTime;
        }
        else
        {
            // 舒适区间：体温自动回归正常值
            float currentBodyTemp = _survivalSystem.GetValue(SurvivalAttributeType.Temperature);
            if (currentBodyTemp < _normalBodyTemp)
            {
                tempDelta = _recoveryRate * deltaTime;
            }
            else if (currentBodyTemp > _normalBodyTemp + 1f)
            {
                tempDelta = -_recoveryRate * deltaTime;
            }
            else
            {
                return; // 体温正常，无需调整
            }
        }

        // 庇护所内修正
        if (_isInShelter)
        {
            // 如果在庇护所内且有供暖，体温不衰减，只恢复
            if (tempDelta < 0f)
            {
                tempDelta *= _shelterMultiplier;
            }
            // 庇护所内额外恢复
            float currentTemp = _survivalSystem.GetValue(SurvivalAttributeType.Temperature);
            if (currentTemp < _normalBodyTemp)
            {
                tempDelta += _recoveryRate * 0.5f * deltaTime;
            }
        }

        if (!Mathf.Approximately(tempDelta, 0f))
        {
            _survivalSystem.ModifyAttribute(SurvivalAttributeType.Temperature, tempDelta);
        }
    }
}
