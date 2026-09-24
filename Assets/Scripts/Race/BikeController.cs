using Fusion;
using UnityEngine;

public class BikeController : NetworkBehaviour
{
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float reverseAcceleration = 4f;
    [SerializeField] private float brakeDeceleration = 12f;
    [SerializeField] private float naturalDeceleration = 4f;
    [SerializeField] private float maxForwardSpeed = 14f;
    [SerializeField] private float maxReverseSpeed = 5f;
    [SerializeField] private float turnSpeed = 90f;
    [SerializeField] private float minSpeedToTurn = 1f;
    [SerializeField] private float groundStickForce = 2f;

    private CharacterController controller;

    [Networked] private float ForwardSpeed { get; set; }
    [Networked] public int CurrentLap { get; private set; }
    [Networked] private int NextCheckpointIndex { get; set; }

    public static BikeController Local { get; private set; }

    public int Progress => CurrentLap * 1000 + NextCheckpointIndex;

    public override void Spawned()
    {
        controller = GetComponent<CharacterController>();

        if (Object.HasStateAuthority && RaceManager.Instance != null)
        {
            NextCheckpointIndex = RaceManager.Instance.CheckpointCount > 1 ? 1 : 0;
        }

        RaceManager.Instance?.RegisterBike(this);

        if (Object.HasInputAuthority)
        {
            Local = this;
            LocalAvatar.Target = transform;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        RaceManager.Instance?.UnregisterBike(this);

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
        bool canDrive = RaceManager.Instance != null && RaceManager.Instance.State == RaceState.Racing;

        float throttle = 0f;
        float turn = 0f;

        if (GetInput(out NetworkInputData input) && canDrive)
        {
            throttle = input.Forward;
            turn = input.Turn;
        }

        UpdateSpeed(throttle);
        UpdateRotation(turn);
        UpdateMovement();

        if (Object.HasStateAuthority)
        {
            CheckCheckpoint();
        }
    }

    private void UpdateSpeed(float throttle)
    {
        float speed = ForwardSpeed;
        float dt = Runner.DeltaTime;

        if (throttle > 0.01f)
        {
            speed = Mathf.MoveTowards(speed, maxForwardSpeed, acceleration * dt);
        }
        else if (throttle < -0.01f)
        {
            if (speed > 0.01f)
            {
                speed = Mathf.MoveTowards(speed, 0f, brakeDeceleration * dt);
            }
            else
            {
                speed = Mathf.MoveTowards(speed, -maxReverseSpeed, reverseAcceleration * dt);
            }
        }
        else
        {
            speed = Mathf.MoveTowards(speed, 0f, naturalDeceleration * dt);
        }

        ForwardSpeed = speed;
    }

    private void UpdateRotation(float turn)
    {
        if (Mathf.Abs(ForwardSpeed) < minSpeedToTurn)
        {
            return;
        }

        float direction = Mathf.Sign(ForwardSpeed);
        transform.Rotate(0f, turn * direction * turnSpeed * Runner.DeltaTime, 0f);
    }

    private void UpdateMovement()
    {
        float dt = Runner.DeltaTime;
        Vector3 previousPosition = transform.position;

        controller.Move(transform.forward * (ForwardSpeed * dt));

        Vector3 achievedMove = transform.position - previousPosition;
        ForwardSpeed = Vector3.Dot(achievedMove, transform.forward) / dt;

        controller.Move(Vector3.down * (groundStickForce * dt));
    }

    public void ApplyCollisionPush(Vector3 push, float speedLoss)
    {
        controller.Move(push);
        ForwardSpeed *= speedLoss;
    }

    private void CheckCheckpoint()
    {
        RaceManager raceManager = RaceManager.Instance;

        if (raceManager == null || raceManager.CheckpointCount == 0)
        {
            return;
        }

        Checkpoint target = raceManager.GetCheckpoint(NextCheckpointIndex);

        if (target.Contains(transform.position) == false)
        {
            return;
        }

        if (NextCheckpointIndex == 0)
        {
            CurrentLap++;
            raceManager.ReportLapCompleted(this, CurrentLap);
        }

        NextCheckpointIndex = (NextCheckpointIndex + 1) % raceManager.CheckpointCount;
    }
}
