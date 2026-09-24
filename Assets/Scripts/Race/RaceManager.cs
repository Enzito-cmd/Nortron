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
    [SerializeField] private int minPlayersToStart = 2;
    [SerializeField] private float countdownSeconds = 5f;
    [SerializeField] private int lapsToWin = 3;
    [SerializeField] private float finishDelaySeconds = 5f;
    [SerializeField] private float bikeCollisionDistance = 1.6f;
    [SerializeField] private float collisionSpeedLoss = 0.6f;
    [SerializeField] private Checkpoint[] checkpoints;

    [Networked] public RaceState State { get; private set; }
    [Networked] private TickTimer CountdownTimer { get; set; }
    [Networked] private TickTimer FinishTimer { get; set; }
    [Networked] public PlayerRef WinnerPlayer { get; private set; }

    private readonly List<BikeController> bikes = new List<BikeController>();

    public static RaceManager Instance { get; private set; }

    public int LapsToWin => lapsToWin;
    public int CheckpointCount => checkpoints.Length;
    public int BikeCount => bikes.Count;

    public Checkpoint GetCheckpoint(int index)
    {
        return checkpoints[index];
    }

    public int GetPosition(BikeController bike)
    {
        int position = 1;

        for (int i = 0; i < bikes.Count; i++)
        {
            if (bikes[i] != bike && bikes[i].Progress > bike.Progress)
            {
                position++;
            }
        }

        return position;
    }

    public override void Spawned()
    {
        Instance = this;
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

                Vector3 offset = b.transform.position - a.transform.position;
                offset.y = 0f;

                float distance = offset.magnitude;

                if (distance >= bikeCollisionDistance || distance <= 0.0001f)
                {
                    continue;
                }

                Vector3 pushDirection = offset / distance;
                Vector3 push = pushDirection * ((bikeCollisionDistance - distance) * 0.5f);

                a.ApplyCollisionPush(-push, collisionSpeedLoss);
                b.ApplyCollisionPush(push, collisionSpeedLoss);
            }
        }
    }
}
