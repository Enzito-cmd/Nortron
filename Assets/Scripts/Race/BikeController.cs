using Fusion;
using UnityEngine;
using UnityEngine.Serialization;

public class BikeController : NetworkBehaviour
{
    private const float AscendingThreshold = 0.1f;
    private const float MinMoveDistance = 0.0001f;
    private const float CeilingNormalY = -0.3f;
    private const float RespawnHeight = 0.5f;
    private const int MaxSlideIterations = 4;
    private const int MaxDepenetrationPasses = 3;
    private const float MetersPerSecondToKmh = 3.6f;
    private const float LaunchThreshold = 1f;
    private const float SurfaceProbeBack = 0.1f;
    private const float SurfaceProbeHeight = 0.2f;

    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float reverseAcceleration = 4f;
    [SerializeField] private float brakeDeceleration = 12f;
    [SerializeField] private float naturalDeceleration = 4f;
    [SerializeField] private float maxForwardSpeed = 14f;
    [SerializeField] private float maxReverseSpeed = 5f;
    [SerializeField] private float turnSpeed = 90f;
    [SerializeField] private float minSpeedToTurn = 1f;

    [Header("Physics")]
    [SerializeField] private LayerMask groundMask = ~0;
    [FormerlySerializedAs("gravity")]
    [SerializeField] private float jumpGravity = -9f;
    [SerializeField] private float dropGravity = -30f;
    [SerializeField] private float maxFallSpeed = 40f;
    [SerializeField] private float maxGroundAngle = 50f;
    [SerializeField] private float groundSnapDistance = 0.3f;
    [SerializeField] private float descentSnapDistance = 0.7f;
    [SerializeField] private float launchMultiplier = 1.2f;
    [SerializeField] private float skinWidth = 0.03f;
    [SerializeField] private float airTurnFactor = 0.35f;
    [SerializeField] private float respawnFallDistance = 15f;

    [Header("Boost")]
    [SerializeField] private float boostDuration = 2.5f;
    [SerializeField] private float boostSpeedBonus = 10f;
    [SerializeField] private float boostAccelerationMultiplier = 3f;

    [Header("Visual")]
    [SerializeField] private Transform visual;
    [SerializeField] private float visualTiltSpeed = 12f;

    [Header("Audio")]
    [SerializeField] private AudioSource engineAudioSource;
    [SerializeField] private AudioClip engineAccelerateClip;

    private readonly Collider[] overlaps = new Collider[16];

    private CapsuleCollider hull;
    private NetworkTransform networkTransform;
    private PlayerIdentity identity;
    private Quaternion visualBaseRotation = Quaternion.identity;
    private Quaternion visualTilt = Quaternion.identity;
    private float minGroundNormalY;

    [Networked] private float ForwardSpeed { get; set; }
    [Networked] private float VerticalSpeed { get; set; }
    [Networked] private NetworkBool IsGrounded { get; set; }
    [Networked] private NetworkBool IsAccelerating { get; set; }
    [Networked] private NetworkBool Launched { get; set; }
    [Networked] public int CurrentLap { get; private set; }
    [Networked] private int NextCheckpointIndex { get; set; }
    [Networked] public NetworkBool HasBoost { get; private set; }
    [Networked] private TickTimer BoostTimer { get; set; }
    [Networked] private NetworkButtons PreviousButtons { get; set; }

    public static BikeController Local { get; private set; }

    public int Progress
    {
        get
        {
            int count = RaceManager.Instance != null ? RaceManager.Instance.CheckpointCount : 0;
            int stage = NextCheckpointIndex == 0 ? count : NextCheckpointIndex;
            return CurrentLap * count + stage;
        }
    }

    public float DistanceToNextCheckpoint
    {
        get
        {
            RaceManager raceManager = RaceManager.Instance;

            if (raceManager == null || raceManager.CheckpointCount == 0)
            {
                return 0f;
            }

            return Vector3.Distance(transform.position, raceManager.GetCheckpoint(NextCheckpointIndex).Center);
        }
    }

    public float SpeedKmh => Mathf.Abs(ForwardSpeed) * MetersPerSecondToKmh;
    public string PlayerName => identity != null ? identity.DisplayName : "Jugador " + Object.InputAuthority.PlayerId;
    public CapsuleCollider Hull => hull;
    public Vector3 HorizontalVelocity => transform.forward * ForwardSpeed;
    public bool IsBoosting => BoostTimer.IsRunning && BoostTimer.Expired(Runner) == false;
    public float BoostFill => IsBoosting ? (BoostTimer.RemainingTime(Runner) ?? 0f) / boostDuration : 0f;

    public override void Spawned()
    {
        hull = GetComponent<CapsuleCollider>();
        networkTransform = GetComponent<NetworkTransform>();
        identity = GetComponent<PlayerIdentity>();
        minGroundNormalY = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);

        if (hull == null)
        {
            hull = CreateDefaultHull();
            Debug.LogWarning("Bike has no CapsuleCollider, a default one was created");
        }

        if (visual == null && transform.childCount > 0)
        {
            visual = transform.GetChild(0);
        }

        if (visual != null)
        {
            visualBaseRotation = visual.localRotation;
        }

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

        if (GetInput(out NetworkInputData input))
        {
            if (canDrive)
            {
                throttle = ClampInput(input.Forward);
                turn = ClampInput(input.Turn);

                if (HasBoost && IsBoosting == false && input.buttons.WasPressed(PreviousButtons, Buttons.Boost))
                {
                    HasBoost = false;
                    BoostTimer = TickTimer.CreateFromSeconds(Runner, boostDuration);
                }
            }

            PreviousButtons = input.buttons;
        }

        UpdateRotation(turn);
        Depenetrate();

        RaycastHit groundHit;
        bool grounded = ProbeGround(IsGrounded, VerticalSpeed > AscendingThreshold, out groundHit);
        IsGrounded = grounded;

        UpdateSpeed(throttle);
        UpdateMovement(grounded, groundHit);

        if (Object.HasStateAuthority)
        {
            CheckCheckpoint();
            CheckBoostPickup();
            CheckFall();
        }
    }

    public override void Render()
    {
        UpdateEngineAudio();
        UpdateVisualTilt();
    }

    public void ApplyPush(Vector3 push)
    {
        MoveWithCollision(push);
    }

    public void SetHorizontalVelocity(Vector3 velocity)
    {
        ForwardSpeed = Vector3.Dot(velocity, transform.forward);
    }

    private static float ClampInput(float value)
    {
        return float.IsNaN(value) ? 0f : Mathf.Clamp(value, -1f, 1f);
    }

    private CapsuleCollider CreateDefaultHull()
    {
        CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
        capsule.isTrigger = true;
        capsule.direction = 2;
        capsule.radius = 0.23f;
        capsule.height = 0.9f;
        capsule.center = new Vector3(0f, 0.14f, 0f);
        return capsule;
    }

    private void UpdateEngineAudio()
    {
        if (engineAudioSource == null || engineAccelerateClip == null)
        {
            return;
        }

        bool canDrive = RaceManager.Instance != null && RaceManager.Instance.State == RaceState.Racing;
        bool shouldPlay = IsAccelerating && canDrive;

        if (shouldPlay)
        {
            if (!engineAudioSource.isPlaying)
            {
                engineAudioSource.clip = engineAccelerateClip;
                engineAudioSource.loop = true;
                engineAudioSource.Play();
            }
        }
        else
        {
            if (engineAudioSource.isPlaying)
            {
                engineAudioSource.Stop();
            }
        }
    }

    private void UpdateVisualTilt()
    {
        if (visual == null)
        {
            return;
        }

        Quaternion target = Quaternion.identity;

        if (IsGrounded)
        {
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 1.5f, groundMask, QueryTriggerInteraction.Ignore))
            {
                target = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
        }
        else
        {
            Vector3 velocity = transform.forward * ForwardSpeed + Vector3.up * VerticalSpeed;

            if (velocity.sqrMagnitude > 0.01f)
            {
                target = Quaternion.FromToRotation(transform.forward, velocity.normalized);
            }
        }

        visualTilt = Quaternion.Slerp(visualTilt, target, 1f - Mathf.Exp(-visualTiltSpeed * Time.deltaTime));
        visual.rotation = visualTilt * transform.rotation * visualBaseRotation;
    }

    private void UpdateSpeed(float throttle)
    {
        bool boosting = IsBoosting;
        bool accelerating = throttle > 0.01f || (boosting && throttle > -0.01f);
        IsAccelerating = accelerating;

        if (IsGrounded == false)
        {
            return;
        }

        float speed = ForwardSpeed;
        float dt = Runner.DeltaTime;

        if (accelerating)
        {
            float topSpeed = boosting ? maxForwardSpeed + boostSpeedBonus : maxForwardSpeed;
            float power = boosting ? acceleration * boostAccelerationMultiplier : acceleration;
            speed = Mathf.MoveTowards(speed, topSpeed, power * dt);
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
        float factor = IsGrounded ? 1f : airTurnFactor;
        transform.Rotate(0f, turn * direction * turnSpeed * factor * Runner.DeltaTime, 0f);
    }

    private void UpdateMovement(bool grounded, RaycastHit groundHit)
    {
        float dt = Runner.DeltaTime;
        Vector3 forward = transform.forward;
        Vector3 velocity = forward * ForwardSpeed;
        float slopeVerticalSpeed = 0f;

        if (grounded)
        {
            Vector3 normal = groundHit.normal;
            slopeVerticalSpeed = -(normal.x * velocity.x + normal.z * velocity.z) / normal.y;
            velocity.y = slopeVerticalSpeed;
        }
        else
        {
            float airGravity = Launched ? jumpGravity : dropGravity;
            VerticalSpeed = Mathf.Max(VerticalSpeed + airGravity * dt, -maxFallSpeed);
            velocity.y = VerticalSpeed;
        }

        Vector3 startPosition = transform.position;
        bool hitCeiling = MoveWithCollision(velocity * dt);

        Vector3 achieved = transform.position - startPosition;
        achieved.y = 0f;
        ForwardSpeed = Vector3.Dot(achieved, forward) / dt;

        RaycastHit snapHit;
        bool landed = ProbeGround(grounded, slopeVerticalSpeed > AscendingThreshold, out snapHit);

        if (landed)
        {
            float gap = snapHit.distance - skinWidth;

            if (Mathf.Abs(gap) > MinMoveDistance)
            {
                transform.position += Vector3.down * gap;
            }

            VerticalSpeed = slopeVerticalSpeed;
            Launched = false;
        }
        else if (grounded)
        {
            bool launching = slopeVerticalSpeed > LaunchThreshold;

            VerticalSpeed = launching ? slopeVerticalSpeed * launchMultiplier : slopeVerticalSpeed;
            Launched = launching;
        }
        else if (hitCeiling && VerticalSpeed > 0f)
        {
            VerticalSpeed = 0f;
        }

        IsGrounded = landed;
    }

    private bool ProbeGround(bool wasGrounded, bool ascending, out RaycastHit hit)
    {
        hit = default;

        if (wasGrounded == false && VerticalSpeed > AscendingThreshold)
        {
            return false;
        }

        float snapDistance = wasGrounded && ascending == false ? descentSnapDistance : groundSnapDistance;

        GetHullSegment(out Vector3 start, out Vector3 end, out float radius);

        bool found = Physics.CapsuleCast(start, end, radius, Vector3.down, out hit, snapDistance + skinWidth, groundMask, QueryTriggerInteraction.Ignore);

        if (found)
        {
            hit.normal = SurfaceNormal(hit);
        }

        return found && hit.normal.y >= minGroundNormalY;
    }

    private Vector3 SurfaceNormal(RaycastHit contact)
    {
        Vector3 behind = transform.forward * -Mathf.Sign(ForwardSpeed);
        Vector3 origin = contact.point + behind * SurfaceProbeBack + Vector3.up * SurfaceProbeHeight;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit surface, SurfaceProbeHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
        {
            return surface.normal;
        }

        return contact.normal;
    }

    private bool MoveWithCollision(Vector3 displacement)
    {
        bool hitCeiling = false;
        Vector3 remaining = displacement;

        for (int i = 0; i < MaxSlideIterations; i++)
        {
            float distance = remaining.magnitude;

            if (distance < MinMoveDistance)
            {
                break;
            }

            Vector3 direction = remaining / distance;
            GetHullSegment(out Vector3 start, out Vector3 end, out float radius);

            if (Physics.CapsuleCast(start, end, radius, direction, out RaycastHit hit, distance + skinWidth, groundMask, QueryTriggerInteraction.Ignore) == false)
            {
                transform.position += remaining;
                break;
            }

            float travel = Mathf.Clamp(hit.distance - skinWidth, 0f, distance);
            transform.position += direction * travel;
            remaining = SlideAlong(direction * (distance - travel), hit.normal);

            if (hit.normal.y < CeilingNormalY)
            {
                hitCeiling = true;
            }
        }

        return hitCeiling;
    }

    private Vector3 SlideAlong(Vector3 remaining, Vector3 normal)
    {
        if (normal.sqrMagnitude < 0.5f)
        {
            return Vector3.zero;
        }

        Vector3 slid = Vector3.ProjectOnPlane(remaining, normal);

        if (normal.y >= minGroundNormalY)
        {
            return slid;
        }

        if (slid.y > 0f)
        {
            slid.y = 0f;
        }

        Vector3 flat = new Vector3(normal.x, 0f, normal.z);

        if (flat.sqrMagnitude > 0.0001f)
        {
            flat.Normalize();
            slid -= flat * Mathf.Min(0f, Vector3.Dot(slid, flat));
        }

        return slid;
    }

    private void Depenetrate()
    {
        for (int pass = 0; pass < MaxDepenetrationPasses; pass++)
        {
            GetHullSegment(out Vector3 start, out Vector3 end, out float radius);
            int count = Physics.OverlapCapsuleNonAlloc(start, end, radius, overlaps, groundMask, QueryTriggerInteraction.Ignore);
            bool moved = false;

            for (int i = 0; i < count; i++)
            {
                Collider other = overlaps[i];
                Transform otherTransform = other.transform;

                if (otherTransform.IsChildOf(transform))
                {
                    continue;
                }

                if (Physics.ComputePenetration(hull, hull.transform.position, hull.transform.rotation, other, otherTransform.position, otherTransform.rotation, out Vector3 direction, out float distance))
                {
                    transform.position += direction * (distance + MinMoveDistance * 10f);
                    moved = true;
                }
            }

            if (moved == false)
            {
                return;
            }
        }
    }

    private void GetHullSegment(out Vector3 start, out Vector3 end, out float radius)
    {
        Transform hullTransform = hull.transform;
        Vector3 scale = hullTransform.lossyScale;
        float scaleFactor = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

        Vector3 axis = hullTransform.forward;

        if (hull.direction == 0)
        {
            axis = hullTransform.right;
        }
        else if (hull.direction == 1)
        {
            axis = hullTransform.up;
        }

        Vector3 center = hullTransform.TransformPoint(hull.center);
        radius = hull.radius * scaleFactor;
        float half = Mathf.Max(hull.height * 0.5f * scaleFactor - radius, 0f);

        start = center + axis * half;
        end = center - axis * half;
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

    private void CheckBoostPickup()
    {
        RaceManager raceManager = RaceManager.Instance;

        if (HasBoost || raceManager == null)
        {
            return;
        }

        if (raceManager.TryTakeBoost(transform.position))
        {
            HasBoost = true;
        }
    }

    private void CheckFall()
    {
        RaceManager raceManager = RaceManager.Instance;

        if (raceManager == null || raceManager.CheckpointCount < 2)
        {
            return;
        }

        int count = raceManager.CheckpointCount;
        int previousIndex = (NextCheckpointIndex + count - 1) % count;
        Vector3 previousCenter = raceManager.GetCheckpoint(previousIndex).Center;

        if (transform.position.y > previousCenter.y - respawnFallDistance)
        {
            return;
        }

        Vector3 forward = raceManager.GetCheckpoint(NextCheckpointIndex).Center - previousCenter;
        forward.y = 0f;

        Vector3 position = previousCenter;

        if (Physics.Raycast(previousCenter + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 30f, groundMask, QueryTriggerInteraction.Ignore))
        {
            position = hit.point + Vector3.up * RespawnHeight;
        }

        Quaternion rotation = forward.sqrMagnitude > 0.01f ? Quaternion.LookRotation(forward) : transform.rotation;

        ForwardSpeed = 0f;
        VerticalSpeed = 0f;
        IsGrounded = false;
        Launched = false;

        if (networkTransform != null)
        {
            networkTransform.Teleport(position, rotation);
        }
        else
        {
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
