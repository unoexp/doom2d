// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/04_Gameplay/Player/FSM/States/PlayerAttackState.cs
// 攻击状态：执行攻击动作，通过 CombatSystem 对范围内敌人造成伤害
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

public class PlayerAttackState : PlayerStateBase
{
    private float _attackTimer;
    private float _attackDuration;
    private float _damageApplyTime;
    private bool _damageApplied;

    private const float ATTACK_RANGE = 1.5f;
    private const float BASE_DAMAGE = 15f;

    public PlayerAttackState(PlayerController player, PlayerStateMachine fsm) : base(player, fsm) { }

    public override void OnEnter()
    {
        var atkType = Player.CurrentAttackType;
        if (atkType == AttackType.Heavy)
        {
            _attackDuration = 0.7f;
            _damageApplyTime = 0.35f;
        }
        else
        {
            _attackDuration = 0.4f;
            _damageApplyTime = 0.15f;
        }

        Player.SetAnimationState(atkType == AttackType.Heavy ? "HeavyAttack" : "Attack");
        Player.SetVelocityX(0f);
        _attackTimer = 0f;
        _damageApplied = false;
    }

    public override void OnUpdate(float deltaTime)
    {
        if (Player.IsDead) { FSM.ChangeState(PlayerState.Dead); return; }

        _attackTimer += deltaTime;

        if (!_damageApplied && _attackTimer >= _damageApplyTime)
        {
            _damageApplied = true;
            ApplyDamage();
        }

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

        float dir = Player.FacingRight ? 1f : -1f;
        Vector2 attackCenter = (Vector2)Player.Transform.position + new Vector2(dir * ATTACK_RANGE * 0.5f, 0f);

        float finalDamage = BASE_DAMAGE * Player.CurrentDamageMultiplier;

        combat.DealDamageInArea(
            Player.gameObject,
            attackCenter,
            ATTACK_RANGE,
            finalDamage,
            DamageType.Physical,
            LayerMask.GetMask("Enemy"));
    }
}
