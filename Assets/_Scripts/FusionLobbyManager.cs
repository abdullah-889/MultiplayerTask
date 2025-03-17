using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FusionLobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static FusionLobbyManager Instance;
    private NetworkRunner _runner;
    [SerializeField] private Transform sessionListContainer;
    [SerializeField] private GameObject sessionListItemPrefab;
    [SerializeField] private string _gameplayScene = "RaceTrackScene";

    [SerializeField] private GameObject lobbyView;
    [SerializeField] private GameObject sessionListView;

    [SerializeField] private Button StartGameButton;

    [SerializeField] private GameObject loading;

    [SerializeField] private Transform playerListContainer; // Parent UI element
    [SerializeField] private TextMeshProUGUI playerNamePrefab; // UI prefab for player name
    private Dictionary<PlayerRef, TextMeshProUGUI> _playerEntries = new Dictionary<PlayerRef, TextMeshProUGUI>(); // Track players

    private async void Awake()
    {
        HideEverything();
        ShowLoading();
     
       
        await InitializeRunner(); // Ensure session list updates work

         if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep this manager and the runner alive
        }
    else
        {
            Destroy(gameObject); // Prevent duplicates
        }
    }

    public async System.Threading.Tasks.Task InitializeRunner()
    {
        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this); // Ensure callbacks are registered
        }

        // **Join the session lobby to receive session list updates**
        var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);
        if (result.Ok)
        {
            sessionListView.SetActive(true);
            HideLoading();
            Debug.Log("Successfully joined session lobby.");
        }
        else
        {
            Debug.LogError($"Failed to join session lobby: {result.ErrorMessage}");
        }
    }

    private void HideLoading()
    {
        loading.gameObject.SetActive(false);
    }

    private void HideEverything()
    {
           StartGameButton.gameObject.SetActive(false);
         sessionListView.gameObject.SetActive(false);
        lobbyView.gameObject.SetActive(false);
         loading.gameObject.SetActive(false);
    }
    private void ShowLoading()
    {
        loading.gameObject.SetActive(true);
        sessionListView.gameObject.SetActive(false);
        lobbyView.gameObject.SetActive(false);
    }

    public async void CreateSession(string sessionName)
    {
        ShowLoading();
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = sessionName,
            PlayerCount = 4, // Max 4 players
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            HideLoading();
            lobbyView.SetActive(true);
            StartGameButton.gameObject.SetActive(true);
            StartGameButton.onClick.RemoveAllListeners();
            StartGameButton.onClick.AddListener(StartGame);
            Debug.Log($"Lobby '{sessionName}' Created!");
        }
        else
        {
            Debug.LogError($"Failed to create lobby: {result.ErrorMessage}");
        }
    }

    public async void JoinSession(string sessionName)
    {
        ShowLoading();
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = sessionName,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            HideLoading();
            lobbyView.SetActive(true);
            Debug.Log($"Joined session '{sessionName}' successfully!");
        }
        else
        {
            Debug.LogError($"Failed to join session: {result.ErrorMessage}");
        }
    }

    public void StartGame()
    {
        
        if (_runner.IsServer) // Only the host can start the game
        {
          
             HideEverything();
            _runner.LoadScene(SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath(_gameplayScene)), LoadSceneMode.Single);
             // SceneManager.UnloadSceneAsync("MainMenuScene");
        }
        else
        {
            Debug.Log("Only the host can start the game!");
        }
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"Session list updated! Found {sessionList.Count} sessions.");
        
        foreach (Transform child in sessionListContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var session in sessionList)
        {
            GameObject newSessionItem = Instantiate(sessionListItemPrefab, sessionListContainer);
            SessionListItem sessionUI = newSessionItem.GetComponent<SessionListItem>();

            sessionUI.roomName.text = session.Name;
            sessionUI.playerCount.text = $"{session.PlayerCount}/4"; // Max 4 players

            sessionUI.joinButton.onClick.RemoveAllListeners();
            sessionUI.joinButton.onClick.AddListener(() => JoinSession(session.Name));
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (lobbyView.activeSelf)
        {
            TextMeshProUGUI newPlayerItem = Instantiate(playerNamePrefab, playerListContainer);
            newPlayerItem.text = $"Player {player.PlayerId}";
            _playerEntries[player] = newPlayerItem;
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player.PlayerId} left!");

        if (_playerEntries.ContainsKey(player))
        {
            Destroy(_playerEntries[player]); // Remove from UI
            _playerEntries.Remove(player);
        }
    }


    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) {}
    public void OnConnectedToServer(NetworkRunner runner) {}
    public void OnDisconnectedFromServer(NetworkRunner runner) {}
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) {}
    public void OnInput(NetworkRunner runner, NetworkInput input) {}
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) {}
    public void OnSceneLoadDone(NetworkRunner runner) {}
    public void OnSceneLoadStart(NetworkRunner runner) {}
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) {}

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) {}
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) {}
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) {}
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) {}
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) {}
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) {}
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) {}
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) {}
}
