using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum RaceState
{
    Waiting,
    Countdown,
    Racing,
    Finished
}

public class RaceManager : NetworkBehaviour
{
    private const int MaxPickups = 8;
    private const float KmhToMetersPerSecond = 1f / 3.6f;

    private static readonly System.Comparison<BikeController> StandingsOrder = CompareProgress;

    [SerializeField] private int minPlayersToStart = 2;
    [SerializeField] private float countdownSeconds = 3f;
    [SerializeField] private int lapsToWin = 3;
    [SerializeField] private float finishDelaySeconds = 5f;
    [SerializeField] private float boostBonusKmh = 36f;
    [SerializeField] private float collisionBounce = 0.3f;
    [SerializeField] private Checkpoint[] checkpoints;

    [Header("Catch Up")]
    [SerializeField] private float minSpeedKmh = 35f;
    [SerializeField] private float baseSpeedKmh = 50f;
    [SerializeField] private float maxSpeedKmh = 65f;
    [SerializeField] private float fullEffectSeconds = 3f;

    [Networked] public RaceState State { get; private set; }
    [Networked] private TickTimer CountdownTimer { get; set; }
    [Networked] private TickTimer FinishTimer { get; set; }
    [Networked] public PlayerRef WinnerPlayer { get; private set; }
    [Networked] public float WinnerSeconds { get; private set; }
    [Networked] public int LapsToWin { get; private set; }
    [Networked] public float BoostBonus { get; private set; }
    [Networked] private int RacingStartTick { get; set; }
    [Networked, Capacity(MaxPickups)] private NetworkArray<TickTimer> PickupTimers => default;

    private readonly List<BikeController> bikes = new List<BikeController>();
    private readonly List<BikeController> standings = new List<BikeController>();
    private ChangeDetector _changeDetector;
    private BoostPickup[] pickups = new BoostPickup[0];
    private readonly List<float> distances = new List<float>();
    private float[] segmentStarts = new float[0];
    private float[] segmentLengths = new float[0];
    private float lapLength;
    private bool catchUpEnabled = true;

    public static RaceManager Instance { get; private set; }

    public int CheckpointCount => checkpoints.Length;
    public int BikeCount => bikes.Count;

    public Checkpoint GetCheckpoint(int index)
    {
        return checkpoints[index];
    }

    public float GetTrackDistance(int lap, int nextCheckpointIndex, float distanceToNext)
    {
        int count = checkpoints.Length;

        if (count < 2)
        {
            return 0f;
        }

        int previous = (nextCheckpointIndex + count - 1) % count;
        float within = Mathf.Clamp(segmentLengths[previous] - distanceToNext, 0f, segmentLengths[previous]);

        return lap * lapLength + segmentStarts[previous] + within;
    }

    public List<BikeController> GetStandings()
    {
        standings.Clear();
        standings.AddRange(bikes);
        standings.Sort(StandingsOrder);
        return standings;
    }

    private static int CompareProgress(BikeController a, BikeController b)
    {
        int byProgress = b.Progress.CompareTo(a.Progress);

        if (byProgress != 0)
        {
            return byProgress;
        }

        return a.DistanceToNextCheckpoint.CompareTo(b.DistanceToNextCheckpoint);
    }

    public override void Spawned()
    {
        Instance = this;
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        BuildTrackPath();

        foreach (BikeController bike in FindObjectsByType<BikeController>(FindObjectsSortMode.None))
        {
            if (bike.Object != null && bike.Object.IsValid)
            {
                RegisterBike(bike);
            }
        }

        if (Object.HasStateAuthority)
        {
            ApplyConfig();
        }

        pickups =FindObjectsByType<BoostPickup>(FindObjectsSortMode.None)
            .OrderBy(pickup => pickup.transform.position.x)
            .ThenBy(pickup => pickup.transform.position.z)
            .ThenBy(pickup => pickup.transform.position.y)
            .Take(MaxPickups)
            .ToArray();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RegisterBike(BikeController bike)
    {
        if (bikes.Contains(bike) == false)
        {
            bikes.Add(bike);
        }
    }

    public void UnregisterBike(BikeController bike)
    {
        bikes.Remove(bike);
    }

    public bool TryTakeBoost(Vector3 position)
    {
        for (int i = 0; i < pickups.Length; i++)
        {
            if (pickups[i].IsNear(position) && PickupTimers[i].ExpiredOrNotRunning(Runner))
            {
                PickupTimers.Set(i, TickTimer.CreateFromSeconds(Runner, pickups[i].RespawnSeconds));
                return true;
            }
        }

        return false;
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority == false)
        {
            return;
        }

        ResolveBikeCollisions();

        switch (State)
        {
            case RaceState.Waiting:
                if (bikes.Count >= minPlayersToStart)
                {
                    State = RaceState.Countdown;
                    CountdownTimer = TickTimer.CreateFromSeconds(Runner, countdownSeconds);
                }
                break;

            case RaceState.Countdown:
                if (CountdownTimer.Expired(Runner))
                {
                    State = RaceState.Racing;
                    RacingStartTick = Runner.Tick.Raw;
                }
                break;

            case RaceState.Racing:
                UpdateSpeedLimits();
                break;

            case RaceState.Finished:
                if (FinishTimer.Expired(Runner))
                {
                    Runner.LoadScene(SceneRef.FromIndex(NetworkManager.LobbySceneIndex), LoadSceneMode.Single);
                }
                break;
        }
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(State))
            {
                if (State == RaceState.Countdown)
                {
                    SoundManager.Instance?.PlayCountdownSound();
                }
                else if (State == RaceState.Finished)
                {
                    OnRaceFinished();
                }
            }
        }

        for (int i = 0; i < pickups.Length; i++)
        {
            pickups[i].SetAvailable(PickupTimers[i].ExpiredOrNotRunning(Runner));
        }
    }

    public void ReportLapCompleted(BikeController bike, int lap)
    {
        if (Object.HasStateAuthority == false || State != RaceState.Racing || lap < LapsToWin)
        {
            return;
        }

        State = RaceState.Finished;
        WinnerPlayer = bike.Object.InputAuthority;
        WinnerSeconds = Mathf.Max(0f, (Runner.Tick.Raw - RacingStartTick) * Runner.DeltaTime);
        FinishTimer = TickTimer.CreateFromSeconds(Runner, finishDelaySeconds);
    }

    public float GetCountdownSecondsRemaining()
    {
        return CountdownTimer.RemainingTime(Runner) ?? 0f;
    }

    public bool IsLocalPlayerWinner()
    {
        return WinnerPlayer == Runner.LocalPlayer;
    }

    private void OnRaceFinished()
    {
        bool won = IsLocalPlayerWinner();

        if (won)
        {
            SoundManager.Instance?.PlayVictorySound();
        }

        if (BikeController.Local != null)
        {
            PlayerStats.RecordRace(won);
        }
    }

    private void ApplyConfig()
    {
        catchUpEnabled = GameConfig.GetBool(GameConfig.CatchUpEnabledKey, catchUpEnabled);
        minSpeedKmh = GameConfig.GetFloat(GameConfig.CatchUpMinKey, minSpeedKmh, 10f, 200f);
        baseSpeedKmh = Mathf.Max(minSpeedKmh, GameConfig.GetFloat(GameConfig.CatchUpBaseKey, baseSpeedKmh, 10f, 200f));
        maxSpeedKmh = Mathf.Max(baseSpeedKmh, GameConfig.GetFloat(GameConfig.CatchUpMaxKey, maxSpeedKmh, 10f, 200f));
        fullEffectSeconds = GameConfig.GetFloat(GameConfig.CatchUpSecondsKey, fullEffectSeconds, 0.5f, 15f);

        LapsToWin = GameConfig.GetInt(GameConfig.LapsKey, lapsToWin, 1, 10);
        BoostBonus = GameConfig.GetFloat(GameConfig.BoostBonusKey, boostBonusKmh, 0f, 60f) * KmhToMetersPerSecond;
    }

    private void BuildTrackPath()
    {
        int count = checkpoints.Length;

        segmentStarts = new float[count];
        segmentLengths = new float[count];
        lapLength = 0f;

        for (int i = 0; i < count; i++)
        {
            Vector3 from = checkpoints[i].Center;
            Vector3 to = checkpoints[(i + 1) % count].Center;

            segmentStarts[i] = lapLength;
            segmentLengths[i] = Vector3.Distance(from, to);
            lapLength += segmentLengths[i];
        }
    }

    private void UpdateSpeedLimits()
    {
        if (catchUpEnabled == false)
        {
            for (int i = 0; i < bikes.Count; i++)
            {
                bikes[i].SetSpeedLimit(baseSpeedKmh * KmhToMetersPerSecond);
            }

            return;
        }

        float fullGap = baseSpeedKmh * KmhToMetersPerSecond * fullEffectSeconds;

        distances.Clear();

        for (int i = 0; i < bikes.Count; i++)
        {
            distances.Add(bikes[i].RaceDistance);
        }

        for (int i = 0; i < bikes.Count; i++)
        {
            float gapAhead = float.MaxValue;
            float gapBehind = float.MaxValue;

            for (int j = 0; j < bikes.Count; j++)
            {
                if (j == i)
                {
                    continue;
                }

                float difference = distances[j] - distances[i];

                if (difference > 0f)
                {
                    gapAhead = Mathf.Min(gapAhead, difference);
                }
                else
                {
                    gapBehind = Mathf.Min(gapBehind, -difference);
                }
            }

            float catchUp = gapAhead == float.MaxValue ? 0f : Mathf.Clamp01(gapAhead / fullGap);
            float hold = gapBehind == float.MaxValue ? 0f : Mathf.Clamp01(gapBehind / fullGap);
            float limit = baseSpeedKmh + (maxSpeedKmh - baseSpeedKmh) * catchUp - (baseSpeedKmh - minSpeedKmh) * hold;

            bikes[i].SetSpeedLimit(limit * KmhToMetersPerSecond);
        }
    }

    private void ResolveBikeCollisions()
    {
        for (int i = 0; i < bikes.Count; i++)
        {
            for (int j = i + 1; j < bikes.Count; j++)
            {
                BikeController a = bikes[i];
                BikeController b = bikes[j];

                if (a.Hull == null || b.Hull == null)
                {
                    continue;
                }

                Transform hullA = a.Hull.transform;
                Transform hullB = b.Hull.transform;

                if (Physics.ComputePenetration(a.Hull, hullA.position, hullA.rotation, b.Hull, hullB.position, hullB.rotation, out Vector3 direction, out float distance) == false)
                {
                    continue;
                }

                Vector3 push = direction * (distance * 0.5f);

                a.ApplyPush(push);
                b.ApplyPush(-push);

                Vector3 normal = -direction;
                normal.y = 0f;

                if (normal.sqrMagnitude < 0.0001f)
                {
                    continue;
                }

                normal.Normalize();

                Vector3 velocityA = a.HorizontalVelocity;
                Vector3 velocityB = b.HorizontalVelocity;
                float approach = Vector3.Dot(velocityB - velocityA, normal);

                if (approach >= 0f)
                {
                    continue;
                }

                float impulse = -(1f + collisionBounce) * approach * 0.5f;

                a.SetHorizontalVelocity(velocityA - normal * impulse);
                b.SetHorizontalVelocity(velocityB + normal * impulse);
            }
        }
    }
}