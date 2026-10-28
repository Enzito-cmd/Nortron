using System;
using System.Collections.Generic;
using System.Text;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public const int MenuSceneIndex = 0;
    public const int LobbySceneIndex = 1;
    public const int RaceSceneIndex = 2;
    public const int NewRaceSceneIndex = 3;
    public const int MaxNameLength = 16;

    private const int MaxPlayers = 4;
    private const string MenuSceneName = "MainMenu";

    [SerializeField] private NetworkRunner runner;
    [SerializeField] private NetworkSceneManagerDefault sceneManager;
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [SerializeField] private NetworkPrefabRef racePlayerPrefab;

    private readonly Dictionary<PlayerRef, NetworkObject> spawnedAvatars = new Dictionary<PlayerRef, NetworkObject>();
    private int loadedSceneIndex = -1;

    public static NetworkManager Instance { get; private set; }

    public string PlayerName { get; set; } = string.Empty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);
        runner.AddCallbacks(this);
    }

    public void LeaveSession()
    {
        runner.Shutdown();
    }

    public async void StartGameHost(string sessionName)
    {
        runner.ProvideInput = true;

        StartGameResult result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = sessionName,
            PlayerCount = MaxPlayers,
            SceneManager = sceneManager
        });

        if (result.Ok)
        {
            await runner.LoadScene(SceneRef.FromIndex(LobbySceneIndex), LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("Could not create the session: " + result.ShutdownReason);
        }
    }

    public async void StartGameClient(string sessionName)
    {
        runner.ProvideInput = true;

        StartGameResult result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = sessionName,
            ConnectionToken = Encoding.UTF8.GetBytes(PlayerName.Trim()),
            SceneManager = sceneManager
        });

        if (result.Ok == false)
        {
            Debug.LogError("Could not join the session: " + result.ShutdownReason);
        }
    }

    private void SpawnAvatar(NetworkRunner runner, PlayerRef player)
    {
        if (spawnedAvatars.ContainsKey(player))
        {
            return;
        }

        NetworkPrefabRef prefabToSpawn;
        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;

        if (loadedSceneIndex == LobbySceneIndex)
        {
            prefabToSpawn = playerPrefab;
        }
        else if (loadedSceneIndex == NewRaceSceneIndex)
        {
            prefabToSpawn = racePlayerPrefab;
        }
        else
        {
            return;
        }

        SpawnPoints spawnPoints = FindFirstObjectByType<SpawnPoints>();

        if (spawnPoints != null)
        {
            spawnPosition = spawnPoints.GetPosition(player.AsIndex);
            spawnRotation = spawnPoints.GetRotation(player.AsIndex);
        }
        else
        {
            Debug.LogWarning("No spawnpoints");
        }

        string nickname = BuildNickname(runner, player);

        NetworkObject avatar = runner.Spawn(prefabToSpawn, spawnPosition, spawnRotation, player, (spawnRunner, spawned) =>
        {
            PlayerIdentity identity = spawned.GetComponent<PlayerIdentity>();

            if (identity != null)
            {
                identity.Nickname = nickname;
            }
        });

        runner.SetPlayerObject(player, avatar);
        spawnedAvatars.Add(player, avatar);
    }

    private string BuildNickname(NetworkRunner runner, PlayerRef player)
    {
        string nickname = string.Empty;

        if (player == runner.LocalPlayer)
        {
            nickname = PlayerName;
        }
        else
        {
            byte[] token = runner.GetPlayerConnectionToken(player);

            if (token != null)
            {
                nickname = Encoding.UTF8.GetString(token);
            }
        }

        nickname = nickname.Trim();

        if (nickname.Length > MaxNameLength)
        {
            nickname = nickname.Substring(0, MaxNameLength);
        }

        if (nickname.Length == 0)
        {
            nickname = "Jugador " + player.PlayerId;
        }

        return nickname;
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            SpawnAvatar(runner, player);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer == false)
        {
            return;
        }

        if (spawnedAvatars.TryGetValue(player, out NetworkObject avatar))
        {
            runner.Despawn(avatar);
            spawnedAvatars.Remove(player);
        }
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        if (runner.IsServer)
        {
            foreach (NetworkObject avatar in spawnedAvatars.Values)
            {
                runner.Despawn(avatar);
            }
        }

        spawnedAvatars.Clear();
        loadedSceneIndex = -1;
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        loadedSceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (runner.IsServer)
        {
            foreach (PlayerRef player in runner.ActivePlayers)
            {
                SpawnAvatar(runner, player);
            }
        }
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        spawnedAvatars.Clear();
        SceneManager.LoadScene(MenuSceneName);
        ReplaceRunner();
    }

    private void ReplaceRunner()
    {
        GameObject oldRunnerObject = runner.gameObject;

        GameObject runnerObject = new GameObject("NetworkRunner");
        DontDestroyOnLoad(runnerObject);

        runner = runnerObject.AddComponent<NetworkRunner>();
        runner.AddCallbacks(this);

        sceneManager = runnerObject.AddComponent<NetworkSceneManagerDefault>();

        Destroy(oldRunnerObject);
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        if (runner.IsShutdown == false)
        {
            runner.Shutdown();
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (ChatBox.IsTyping)
        {
            input.Set(new NetworkInputData());
            return;
        }

        float forward = 0f;
        float turn = 0f;

        if (Input.GetKey(KeyCode.W))
        {
            forward += 1f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            forward -= 1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            turn += 1f;
        }

        if (Input.GetKey(KeyCode.A))
        {
            turn -= 1f;
        }

        NetworkInputData data = new NetworkInputData();
        data.Forward = forward;
        data.Turn = turn;
        data.buttons.Set((int)Buttons.Jump, Input.GetKey(KeyCode.Space));
        data.buttons.Set((int)Buttons.Boost, Input.GetKey(KeyCode.Space));

        input.Set(data);
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }
}
