// 📁 Assets/_Game/04_Gameplay/Player/FSM/States/PlayerIdleState.cs
// 待机状态：无输入时的默认状态
using UnityEngine;

public class PlayerIdleState : PlayerStateBase
{
    public PlayerIdleState(PlayerController player, PlayerStateMachine fsm) : base(player, fsm) { }

    public override void OnEnter()
    {
        Player.SetAnimationState("Idle");
        Player.SetVelocityX(0f);
    }

    public override void OnUpdate(float deltaTime)
    {
        // 状态转移检测
        if (Player.IsDead) { FSM.ChangeState(PlayerState.Dead); return; }
        if (Player.AttackRequested) { FSM.ChangeState(PlayerState.Attack); return; }
        if (Player.DodgeRequested) { FSM.ChangeState(PlayerState.Dodge); return; }
        if (Player.JumpRequested && Player.IsGrounded) { FSM.ChangeState(PlayerState.Jump); return; }
        if (!Player.IsGrounded) { FSM.ChangeState(PlayerState.Fall); return; }

        float moveInput = Player.MoveInput.x;
        if (Mathf.Abs(moveInput) > 0.01f)
        {
            FSM.ChangeState(Player.IsRunning ? PlayerState.Run : PlayerState.Walk);
        }
    }
}
