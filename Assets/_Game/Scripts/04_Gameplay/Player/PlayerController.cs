// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/04_Gameplay/Player/PlayerController.cs
// 玩家控制器。处理输入、物理移动、状态机驱动。
// 2D横版角色控制的核心组件。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// 玩家控制器（MonoBehaviour）。
///
/// 核心职责：
///   · 读取输入（移动、跳跃、奔跑、交互）
///   · 管理 Rigidbody2D 物理移动
///   · 驱动 PlayerStateMachine
///   · 提供地面检测和朝向管理
///   · 订阅 PlayerDeadEvent 处理死亡
///
/// 设计说明：
///   · 各状态通过 PlayerController 的公有方法/属性控制角色行为
///   · 状态类不直接操作 Rigidbody2D，统一通过 Controller 接口
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, IDamageable
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("生命值")]
    [SerializeField] private float _maxHealth = 100f;

    [Header("移动参数")]
    [SerializeField] private float _walkSpeed = 4f;
    [SerializeField] private float _runSpeed = 7f;
    [SerializeField] private float _airMoveSpeed = 3.5f;

    [Header("跳跃参数")]
    [SerializeField] private float _jumpForce = 10f;

    [Header("地面检测")]
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private float _groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask _groundLayer;

    // ══════════════════════════════════════════════════════
    // 组件引用
    // ══════════════════════════════════════════════════════

    private Rigidbody2D _rb;
    private Animator _animator;
    private PlayerStateMachine _fsm;

    // ══════════════════════════════════════════════════════
    // 输入状态
    // ══════════════════════════════════════════════════════

    private Vector2 _moveInput;
    private bool _jumpRequested;
    private bool _attackRequested;
    private bool _dodgeRequested;
    private bool _runHeld;
    private bool _inputEnabled = true;

    // ══════════════════════════════════════════════════════
    // 运行时状态
    // ══════════════════════════════════════════════════════

    private float _currentHealth;
    private bool _isGrounded;
    private bool _isDead;
    private bool _facingRight = true;

    // ══════════════════════════════════════════════════════
    // 公有属性（供 FSM 状态类读取）
    // ══════════════════════════════════════════════════════

    public Vector2 MoveInput => _moveInput;
    public bool JumpRequested => _jumpRequested;
    public bool AttackRequested => _attackRequested;
    public bool DodgeRequested => _dodgeRequested;
    public bool IsRunning => _runHeld;
    public Transform Transform => transform;
    public bool IsGrounded => _isGrounded;
    public bool IsDead => _isDead;
    public bool FacingRight => _facingRight;
    public float VerticalVelocity => _rb != null ? _rb.velocity.y : 0f;

    public float WalkSpeed => _walkSpeed;
    public float RunSpeed => _runSpeed;
    public float AirMoveSpeed => _airMoveSpeed;

    public PlayerState CurrentState => _fsm?.CurrentState ?? PlayerState.Idle;

    // ══════════════════════════════════════════════════════
    // IDamageable 实现
    // ══════════════════════════════════════════════════════

    float IDamageable.CurrentHealth => _currentHealth;
    float IDamageable.MaxHealth => _maxHealth;
    bool IDamageable.IsDead => _isDead;
    Transform IDamageable.Transform => transform;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponentInChildren<Animator>();

        // 冻结Z轴旋转（2D角色不应旋转）
        _rb.freezeRotation = true;

        _fsm = new PlayerStateMachine(this);
    }

    private void Start()
    {
        _currentHealth = _maxHealth;
        _fsm.Initialize(PlayerState.Idle);

        // 设置交互系统的交互者
        if (ServiceLocator.TryGet<InteractionSystem>(out var interactionSystem))
            interactionSystem.SetInteractor(transform);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerDeadEvent>(OnPlayerDead);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerDeadEvent>(OnPlayerDead);
    }

    private void Update()
    {
        ReadInput();
        _fsm.Update(Time.deltaTime);

        // 消费一次性输入
        _jumpRequested = false;
        _attackRequested = false;
        _dodgeRequested = false;
    }

    private void FixedUpdate()
    {
        CheckGrounded();
        _fsm.FixedUpdate(Time.fixedDeltaTime);
    }

    // ══════════════════════════════════════════════════════
    // 公有方法（供 FSM 状态类调用）
    // ══════════════════════════════════════════════════════

    /// <summary>设置水平速度（保持垂直速度不变）</summary>
    public void SetVelocityX(float velocityX)
    {
        _rb.velocity = new Vector2(velocityX, _rb.velocity.y);
    }

    /// <summary>执行跳跃</summary>
    public void PerformJump()
    {
        _rb.velocity = new Vector2(_rb.velocity.x, _jumpForce);
    }

    /// <summary>更新朝向</summary>
    public void UpdateFacing(float moveX)
    {
        if (moveX > 0.01f && !_facingRight) Flip();
        else if (moveX < -0.01f && _facingRight) Flip();
    }

    /// <summary>设置动画状态</summary>
    public void SetAnimationState(string stateName)
    {
        if (_animator != null)
            _animator.Play(stateName, 0, 0f);
    }

    /// <summary>禁用输入（死亡/过场/对话时）</summary>
    public void DisableInput()
    {
        _inputEnabled = false;
        _moveInput = Vector2.zero;
        _jumpRequested = false;
        _runHeld = false;
    }

    /// <summary>启用输入</summary>
    public void EnableInput()
    {
        _inputEnabled = true;
    }

    // ══════════════════════════════════════════════════════
    // IDamageable 方法
    // ══════════════════════════════════════════════════════

    public void TakeDamage(DamageInfo info)
    {
        if (_isDead) return;

        _currentHealth -= info.Damage;
        _currentHealth = Mathf.Max(0f, _currentHealth);

        // 击退
        if (info.KnockbackForce > 0f && _rb != null)
        {
            _rb.AddForce(info.KnockbackDirection * info.KnockbackForce, ForceMode2D.Impulse);
        }

        if (_currentHealth <= 0f)
        {
            _isDead = true;
            EventBus.Publish(new PlayerDeadEvent { Cause = DeathCause.Combat });
        }
    }

    public void Heal(float amount)
    {
        if (_isDead) return;
        _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
    }

    // ══════════════════════════════════════════════════════
    // 输入读取
    // ══════════════════════════════════════════════════════

    private void ReadInput()
    {
        if (!_inputEnabled)
        {
            _moveInput = Vector2.zero;
            return;
        }

        // 使用旧版 Input 系统（项目未安装 Input System 包）
        _moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        _runHeld = Input.GetKey(KeyCode.LeftShift);

        if (Input.GetKeyDown(KeyCode.Space))
            _jumpRequested = true;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J))
            _attackRequested = true;

        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.K))
            _dodgeRequested = true;

        // 交互键
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (ServiceLocator.TryGet<InteractionSystem>(out var interactionSystem))
                interactionSystem.TryInteract();
        }
    }

    // ══════════════════════════════════════════════════════
    // 地面检测
    // ══════════════════════════════════════════════════════

    private void CheckGrounded()
    {
        if (_groundCheck == null)
        {
            // 没有配置 GroundCheck 时使用角色脚底位置
            _isGrounded = Physics2D.OverlapCircle(
                (Vector2)transform.position + Vector2.down * 0.5f,
                _groundCheckRadius, _groundLayer);
        }
        else
        {
            _isGrounded = Physics2D.OverlapCircle(
                _groundCheck.position, _groundCheckRadius, _groundLayer);
        }
    }

    // ══════════════════════════════════════════════════════
    // 翻转朝向
    // ══════════════════════════════════════════════════════

    private void Flip()
    {
        _facingRight = !_facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    // ══════════════════════════════════════════════════════
    // 事件处理
    // ══════════════════════════════════════════════════════

    private void OnPlayerDead(PlayerDeadEvent evt)
    {
        _isDead = true;
    }

    // ══════════════════════════════════════════════════════
    // Gizmos（编辑器调试）
    // ══════════════════════════════════════════════════════

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 绘制地面检测范围
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Vector3 checkPos = _groundCheck != null
            ? _groundCheck.position
            : transform.position + Vector3.down * 0.5f;
        Gizmos.DrawWireSphere(checkPos, _groundCheckRadius);
    }
#endif
}
