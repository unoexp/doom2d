// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/04_Gameplay/Combat/HitBox.cs
// 攻击碰撞检测组件。挂载在武器/攻击特效上，检测命中目标。
// ══════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻击碰撞检测组件。
///
/// 核心职责：
///   · 通过 Trigger 碰撞检测命中 IDamageable 目标
///   · 支持单次命中和持续命中模式
///   · 防止同一次攻击对同一目标重复命中
///   · 命中时通过 CombatSystem 处理伤害
///
/// 使用方式：
///   · 挂载在带有 Collider2D (IsTrigger=true) 的攻击判定物体上
///   · 攻击开始时调用 Activate()，攻击结束时调用 Deactivate()
///   · 通常由 PlayerAttackState / EnemyAttackState 控制激活时机
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HitBox : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("伤害配置")]
    [Tooltip("基础伤害值（可被外部覆盖）")]
    [SerializeField] private float _baseDamage = 10f;

    [Tooltip("伤害类型")]
    [SerializeField] private DamageType _damageType = DamageType.Physical;

    [Tooltip("暴击率")]
    [SerializeField] private float _critChance = 0.1f;

    [Header("击退配置")]
    [Tooltip("击退力度")]
    [SerializeField] private float _knockbackForce = 5f;

    [Header("行为配置")]
    [Tooltip("是否允许对同一目标多次命中（如持续伤害区域）")]
    [SerializeField] private bool _allowMultiHit = false;

    [Tooltip("多次命中时的间隔（秒）")]
    [SerializeField] private float _multiHitInterval = 0.5f;

    // ══════════════════════════════════════════════════════
    // 运行时状态
    // ══════════════════════════════════════════════════════

    private bool _isActive;
    private GameObject _owner;
    private CombatSystem _combatSystem;

    /// <summary>本次攻击已命中的目标（防止重复命中）</summary>
    private readonly HashSet<int> _hitTargets = new HashSet<int>();

    /// <summary>多次命中模式下的命中时间记录</summary>
    private readonly Dictionary<int, float> _lastHitTime = new Dictionary<int, float>();

    // ══════════════════════════════════════════════════════
    // 属性
    // ══════════════════════════════════════════════════════

    public bool IsActive => _isActive;
    public float BaseDamage => _baseDamage;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        // 确保 Collider 为 Trigger
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        // 默认关闭
        Deactivate();
    }

    private void Start()
    {
        _combatSystem = ServiceLocator.Get<CombatSystem>();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>激活攻击判定</summary>
    /// <param name="owner">攻击者 GameObject</param>
    /// <param name="damage">覆盖伤害值（-1=使用默认）</param>
    public void Activate(GameObject owner, float damage = -1f)
    {
        _owner = owner;
        if (damage >= 0f) _baseDamage = damage;

        _isActive = true;
        _hitTargets.Clear();
        _lastHitTime.Clear();

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
    }

    /// <summary>关闭攻击判定</summary>
    public void Deactivate()
    {
        _isActive = false;
        _hitTargets.Clear();
        _lastHitTime.Clear();

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    /// <summary>设置伤害参数</summary>
    public void SetDamageParams(float damage, DamageType type, float critChance)
    {
        _baseDamage = damage;
        _damageType = type;
        _critChance = critChance;
    }

    // ══════════════════════════════════════════════════════
    // 碰撞检测
    // ══════════════════════════════════════════════════════

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isActive) return;
        TryHitTarget(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!_isActive || !_allowMultiHit) return;
        TryHitTarget(other);
    }

    /// <summary>尝试对碰撞目标造成伤害</summary>
    private void TryHitTarget(Collider2D other)
    {
        // 不攻击自己
        if (_owner != null && other.gameObject == _owner) return;

        // 查找 IDamageable 组件
        var damageable = other.GetComponent<IDamageable>();
        if (damageable == null || damageable.IsDead) return;

        int targetId = other.gameObject.GetInstanceID();

        // 非多次命中模式：每个目标只命中一次
        if (!_allowMultiHit)
        {
            if (_hitTargets.Contains(targetId)) return;
            _hitTargets.Add(targetId);
        }
        else
        {
            // 多次命中模式：检查间隔
            float currentTime = Time.time;
            if (_lastHitTime.TryGetValue(targetId, out float lastTime))
            {
                if (currentTime - lastTime < _multiHitInterval) return;
            }
            _lastHitTime[targetId] = currentTime;
        }

        // 通过 CombatSystem 处理伤害
        if (_combatSystem != null)
        {
            _combatSystem.Attack(_owner, damageable, _baseDamage, _damageType, _critChance);
        }
    }
}
