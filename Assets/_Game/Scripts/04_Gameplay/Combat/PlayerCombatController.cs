// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/04_Gameplay/Combat/PlayerCombatController.cs
// 玩家战斗输入控制器。管理轻击/重击/闪避/格挡的输入处理和体力消耗。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// 玩家战斗输入控制器。
///
/// 核心职责：
///   · 读取战斗相关输入（攻击/格挡/闪避）
///   · 管理攻击冷却和体力消耗
///   · 区分轻击/重击（按住时长）
///   · 管理格挡状态和减伤
///   · 通过 PlayerController 驱动状态机切换
///
/// 设计说明：
///   · 挂载在玩家 GameObject 上，与 PlayerController 配合
///   · 通过 SurvivalStatusSystem 消耗/检查体力
///   · 武器伤害从 EquipmentSystem 获取当前装备武器属性
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerCombatController : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("轻击")]
    [SerializeField] private float _lightAttackStaminaCost = 5f;
    [SerializeField] private float _lightAttackCooldown = 0.4f;

    [Header("重击")]
    [SerializeField] private float _heavyAttackStaminaCost = 15f;
    [SerializeField] private float _heavyAttackCooldown = 1.0f;
    [SerializeField] private float _heavyChargeTime = 0.5f;

    [Header("闪避")]
    [SerializeField] private float _dodgeStaminaCost = 20f;
    [SerializeField] private float _dodgeCooldown = 0.8f;
    [SerializeField] private float _dodgeDuration = 0.3f;
    [SerializeField] private float _dodgeSpeed = 12f;

    [Header("格挡")]
    [SerializeField] private float _blockStaminaCostPerHit = 8f;
    [SerializeField] private float _blockDamageReduction = 0.5f;

    // ══════════════════════════════════════════════════════
    // 组件引用
    // ══════════════════════════════════════════════════════

    private PlayerController _player;
    private SurvivalStatusSystem _survivalStatus;
    private SkillSystem _skillSystem;

    // ══════════════════════════════════════════════════════
    // 运行时状态
    // ══════════════════════════════════════════════════════

    private float _attackCooldownTimer;
    private float _dodgeCooldownTimer;
    private float _attackHoldTime;
    private bool _isBlocking;
    private bool _isDodging;
    private float _dodgeTimer;

    // ══════════════════════════════════════════════════════
    // 公有属性
    // ══════════════════════════════════════════════════════

    /// <summary>当前是否在格挡</summary>
    public bool IsBlocking => _isBlocking;

    /// <summary>格挡减伤比例</summary>
    public float BlockDamageReduction => _blockDamageReduction;

    /// <summary>当前是否在闪避（无敌帧）</summary>
    public bool IsDodging => _isDodging;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
    }

    private void Start()
    {
        _survivalStatus = ServiceLocator.Get<SurvivalStatusSystem>();
        ServiceLocator.TryGet<SkillSystem>(out _skillSystem);
    }

    private void Update()
    {
        // 冷却计时
        if (_attackCooldownTimer > 0f)
            _attackCooldownTimer -= Time.deltaTime;
        if (_dodgeCooldownTimer > 0f)
            _dodgeCooldownTimer -= Time.deltaTime;

        // 闪避中的移动
        if (_isDodging)
        {
            _dodgeTimer -= Time.deltaTime;
            if (_dodgeTimer <= 0f)
                _isDodging = false;
        }

        HandleInput();
    }

    // ══════════════════════════════════════════════════════
    // 输入处理
    // ══════════════════════════════════════════════════════

    private void HandleInput()
    {
        if (_player.IsDead) return;

        // 格挡（右键按住）
        _isBlocking = Input.GetMouseButton(1) && HasStamina(_blockStaminaCostPerHit);

        // 闪避（空格 + 方向键）
        if (Input.GetKeyDown(KeyCode.Space) && _dodgeCooldownTimer <= 0f)
        {
            TryDodge();
            return;
        }

        // 攻击（左键）
        if (Input.GetMouseButton(0))
        {
            _attackHoldTime += Time.deltaTime;
        }

        if (Input.GetMouseButtonUp(0) && _attackCooldownTimer <= 0f)
        {
            if (_attackHoldTime >= _heavyChargeTime)
                TryHeavyAttack();
            else
                TryLightAttack();

            _attackHoldTime = 0f;
        }

        if (!Input.GetMouseButton(0))
            _attackHoldTime = 0f;
    }

    // ══════════════════════════════════════════════════════
    // 战斗操作
    // ══════════════════════════════════════════════════════

    private void TryLightAttack()
    {
        if (!ConsumeStamina(_lightAttackStaminaCost)) return;

        _attackCooldownTimer = _lightAttackCooldown;

        // 技能加成
        float bonus = _skillSystem != null ? _skillSystem.GetPrimaryBonus(SkillType.Combat) : 0f;
        float damageMultiplier = 1f + bonus;

        // 通过 PlayerController 触发攻击状态
        // PlayerAttackState 会调用 CombatSystem
        _player.TriggerAttack(AttackType.Light, damageMultiplier);
    }

    private void TryHeavyAttack()
    {
        if (!ConsumeStamina(_heavyAttackStaminaCost)) return;

        _attackCooldownTimer = _heavyAttackCooldown;

        float bonus = _skillSystem != null ? _skillSystem.GetPrimaryBonus(SkillType.Combat) : 0f;
        float damageMultiplier = 2f + bonus;

        _player.TriggerAttack(AttackType.Heavy, damageMultiplier);
    }

    private void TryDodge()
    {
        if (!ConsumeStamina(_dodgeStaminaCost)) return;

        _dodgeCooldownTimer = _dodgeCooldown;
        _isDodging = true;
        _dodgeTimer = _dodgeDuration;

        // 闪避方向
        float dir = _player.FacingRight ? 1f : -1f;
        if (Mathf.Abs(_player.MoveInput.x) > 0.01f)
            dir = Mathf.Sign(_player.MoveInput.x);

        _player.SetVelocityX(dir * _dodgeSpeed);
    }

    /// <summary>当格挡成功时由外部调用，消耗体力</summary>
    public void OnBlockHit()
    {
        ConsumeStamina(_blockStaminaCostPerHit);
    }

    // ══════════════════════════════════════════════════════
    // 体力管理
    // ══════════════════════════════════════════════════════

    private bool HasStamina(float cost)
    {
        if (_survivalStatus == null) return true;
        return _survivalStatus.GetValue(SurvivalAttributeType.Stamina) >= cost;
    }

    private bool ConsumeStamina(float cost)
    {
        if (_survivalStatus == null) return true;

        float current = _survivalStatus.GetValue(SurvivalAttributeType.Stamina);
        if (current < cost) return false;

        _survivalStatus.ModifyAttribute(SurvivalAttributeType.Stamina, -cost);
        return true;
    }
}
