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

    private static readonly System.Comparison<BikeController> StandingsOrder = CompareProgress;

    [SerializeField] private int minPlayersToStart = 2;
    [SerializeField] private float countdownSeconds = 3f;
    [SerializeField] private int lapsToWin = 3;
    [SerializeField] private float finishDelaySeconds = 5f;
    [SerializeField] private float collisionBounce = 0.3f;
    [SerializeField] private Checkpoint[] checkpoints;

    [Networked] public RaceState State { get; private set; }
    [Networked] private TickTimer CountdownTimer { get; set; }
    [Networked] private TickTimer FinishTimer { get; set; }
    [Networked] public PlayerRef WinnerPlayer { get; private set; }
    [Networked, Capacity(MaxPickups)] private NetworkArray<TickTimer> PickupTimers => default;

    private readonly List<BikeController> bikes = new List<BikeController>();
    private readonly List<BikeController> standings = new List<BikeController>();
    private ChangeDetector _changeDetector;
    private BoostPickup[] pickups = new BoostPickup[0];

    public static RaceManager Instance { get; private set; }

    public int LapsToWin => lapsToWin;
    public int CheckpointCount => checkpoints.Length;
    public int BikeCount => bikes.Count;

    public Checkpoint GetCheckpoint(int index)
    {
        return checkpoints[index];
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

        pickups = FindObjectsByType<BoostPickup>(FindObjectsSortMode.None)
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
                }
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
                else if (State == RaceState.Finished && IsLocalPlayerWinner())
                {
                    if (IsLocalPlayerWinner())
                    {
                        SoundManager.Instance?.PlayVictorySound();
                    }
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
        if (Object.HasStateAuthority == false || State != RaceState.Racing || lap < lapsToWin)
        {
            return;
        }

        State = RaceState.Finished;
        WinnerPlayer = bike.Object.InputAuthority;
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