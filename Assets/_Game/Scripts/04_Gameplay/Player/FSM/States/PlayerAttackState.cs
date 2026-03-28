// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/04_Gameplay/Player/FSM/States/PlayerAttackState.cs
// 攻击状态：执行攻击动作，通过 CombatSystem 对范围内敌人造成伤害
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

public class PlayerAttackState : PlayerStateBase
{
    private float _attackTimer;
    private float _attackDuration = 0.4f;
    private bool _damageApplied;

    // 攻击参数（后续可移到 SO 配置）
    private const float ATTACK_DAMAGE = 15f;
    private const float ATTACK_RANGE = 1.5f;
    private const float DAMAGE_APPLY_TIME = 0.15f; // 动画中伤害判定的时间点

    public PlayerAttackState(PlayerController player, PlayerStateMachine fsm) : base(player, fsm) { }

    public override void OnEnter()
    {
        Player.SetAnimationState("Attack");
        Player.SetVelocityX(0f);
        _attackTimer = 0f;
        _damageApplied = false;
    }

    public override void OnUpdate(float deltaTime)
    {
        if (Player.IsDead) { FSM.ChangeState(PlayerState.Dead); return; }

        _attackTimer += deltaTime;

        // 在伤害判定时间点施加伤害
        if (!_damageApplied && _attackTimer >= DAMAGE_APPLY_TIME)
        {
            _damageApplied = true;
            ApplyDamage();
        }

        // 攻击动画结束后回到待机
        if (_attackTimer >= _attackDuration)
        {
            if (!Player.IsGrounded)
                FSM.ChangeState(PlayerState.Fall);
            else if (Mathf.Abs(Player.MoveInput.x) > 0.01f)
                FSM.ChangeState(Player.IsRunning ? PlayerState.Run : PlayerState.Walk);
            else
                FSM.ChangeState(PlayerState.Idle);
        }
    }

    private void ApplyDamage()
    {
        if (!ServiceLocator.TryGet<CombatSystem>(out var combat)) return;

        // 攻击方向：面朝方向的前方
        float dir = Player.FacingRight ? 1f : -1f;
        Vector2 attackCenter = (Vector2)Player.Transform.position + new Vector2(dir * ATTACK_RANGE * 0.5f, 0f);

        combat.DealDamageInArea(
            Player.gameObject,
            attackCenter,
            ATTACK_RANGE,
            ATTACK_DAMAGE,
            DamageType.Physical,
            LayerMask.GetMask("Enemy"));
    }
}
