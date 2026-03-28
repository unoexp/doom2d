// 📁 Assets/_Game/04_Gameplay/Player/FSM/States/PlayerMoveState.cs
// 行走状态
using UnityEngine;

public class PlayerMoveState : PlayerStateBase
{
    public PlayerMoveState(PlayerController player, PlayerStateMachine fsm) : base(player, fsm) { }

    public override void OnEnter()
    {
        Player.SetAnimationState("Walk");
    }

    public override void OnUpdate(float deltaTime)
    {
        if (Player.IsDead) { FSM.ChangeState(PlayerState.Dead); return; }
        if (Player.AttackRequested) { FSM.ChangeState(PlayerState.Attack); return; }
        if (Player.DodgeRequested) { FSM.ChangeState(PlayerState.Dodge); return; }
        if (Player.JumpRequested && Player.IsGrounded) { FSM.ChangeState(PlayerState.Jump); return; }
        if (!Player.IsGrounded) { FSM.ChangeState(PlayerState.Fall); return; }

        float moveInput = Player.MoveInput.x;

        if (Mathf.Abs(moveInput) < 0.01f)
        {
            FSM.ChangeState(PlayerState.Idle);
            return;
        }

        if (Player.IsRunning)
        {
            FSM.ChangeState(PlayerState.Run);
            return;
        }

        Player.SetVelocityX(moveInput * Player.WalkSpeed);
        Player.UpdateFacing(moveInput);
    }
}
