using Fusion;
using UnityEngine;

public class LobbyPlayer : NetworkBehaviour
{
    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private static readonly int GroundedParameter = Animator.StringToHash("Grounded");
    private static readonly int JumpParameter = Animator.StringToHash("Jump");

    private const float JumpStartVelocity = 2f;

    [SerializeField] private Animator animator;
    [SerializeField] private float turnSpeed = 120f;

    private NetworkCharacterController characterController;
    private bool wasGrounded = true;

    public static LobbyPlayer Local { get; private set; }

    public override void Spawned()
    {
        characterController = GetComponent<NetworkCharacterController>();

        characterController.rotationSpeed = 0f;

        if (Object.HasInputAuthority)
        {
            Local = this;
            LocalAvatar.Target = transform;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Local == this)
        {
            Local = null;
        }

        if (LocalAvatar.Target == transform)
        {
            LocalAvatar.Target = null;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData input))
        {
            transform.Rotate(0f, input.Turn * turnSpeed * Runner.DeltaTime, 0f);
            characterController.Move(transform.forward * input.Forward);

            if (input.buttons.IsSet(Buttons.Jump))
            {
                characterController.Jump();
            }
        }
    }

    public override void Render()
    {
        if (animator == null)
        {
            return;
        }

        Vector3 velocity = characterController.Velocity;
        bool isGrounded = characterController.Grounded;
        float horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;

        animator.SetFloat(SpeedParameter, horizontalSpeed, 0.1f, Time.deltaTime);
        animator.SetBool(GroundedParameter, isGrounded);

        if (wasGrounded && isGrounded == false && velocity.y > JumpStartVelocity)
        {
            animator.SetTrigger(JumpParameter);
        }

        if (isGrounded && wasGrounded == false)
        {
            animator.ResetTrigger(JumpParameter);
        }

        wasGrounded = isGrounded;
    }
}
