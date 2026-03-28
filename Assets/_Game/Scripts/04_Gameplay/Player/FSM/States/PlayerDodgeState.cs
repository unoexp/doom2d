// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/04_Gameplay/Player/FSM/States/PlayerDodgeState.cs
// 闪避状态：快速位移，期间无敌
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

public class PlayerDodgeState : PlayerStateBase
{
    private float _dodgeTimer;
    private float _dodgeDirection;

    private const float DODGE_DURATION = 0.35f;
    private const float DODGE_SPEED = 12f;

    public PlayerDodgeState(PlayerController player, PlayerStateMachine fsm) : base(player, fsm) { }

    public override void OnEnter()
    {
        Player.SetAnimationState("Dodge");
        _dodgeTimer = 0f;

        // 闪避方向：有输入用输入方向，否则用面朝方向
        float inputX = Player.MoveInput.x;
        _dodgeDirection = Mathf.Abs(inputX) > 0.01f
            ? Mathf.Sign(inputX)
            : (Player.FacingRight ? 1f : -1f);

        // TODO: 设置无敌帧（通过 CombatSystem 或 IDamageable 标记）
    }

    public override void OnUpdate(float deltaTime)
    {
        if (Player.IsDead) { FSM.ChangeState(PlayerState.Dead); return; }

        _dodgeTimer += deltaTime;

        Player.SetVelocityX(_dodgeDirection * DODGE_SPEED);

        if (_dodgeTimer >= DODGE_DURATION)
        {
            if (!Player.IsGrounded)
                FSM.ChangeState(PlayerState.Fall);
            else
                FSM.ChangeState(PlayerState.Idle);
        }
    }

    public override void OnExit()
    {
        // TODO: 取消无敌帧
    }
}
