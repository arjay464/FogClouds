using UnityEngine;
using Steamworks;
using Mirror;

public class SteamLobbyManager : MonoBehaviour
{
    public static SteamLobbyManager Instance { get; private set; }

    private const string HostAddressKey = "HostAddress";
    private CSteamID _lobbyID;

    // Callbacks
    private Callback<LobbyCreated_t> _lobbyCreated;
    private Callback<GameLobbyJoinRequested_t> _joinRequested;
    private Callback<LobbyEnter_t> _lobbyEntered;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (!SteamManager.Initialized) return;

        _lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        _joinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
        _lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    // --- Host ---

    public void HostLobby()
    {
        if (!SteamManager.Initialized) return;
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 2);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("[SteamLobbyManager] Lobby creation failed.");
            return;
        }

        _lobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        SteamMatchmaking.SetLobbyData(_lobbyID, HostAddressKey,
            SteamUser.GetSteamID().ToString());

        NetworkManager.singleton.StartHost();
        NetworkManager.singleton.ServerChangeScene("SampleScene");
        Debug.Log("[SteamLobbyManager] Lobby created, loading game scene.");
    }

    // --- Client ---

    private void OnJoinRequested(GameLobbyJoinRequested_t callback)
    {
        // Fired when the player accepts a Steam invite
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (NetworkServer.active) return;

        string hostAddress = SteamMatchmaking.GetLobbyData(
            new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey);

        if (string.IsNullOrEmpty(hostAddress))
        {
            Debug.LogError("[SteamLobbyManager] No host address in lobby data.");
            return;
        }

        NetworkManager.singleton.networkAddress = hostAddress;
        NetworkManager.singleton.StartClient();
        Debug.Log($"[SteamLobbyManager] Connecting to host: {hostAddress}");
    }

    // --- Cleanup ---

    public void LeaveLobby()
    {
        if (_lobbyID.IsValid())
            SteamMatchmaking.LeaveLobby(_lobbyID);
    }
}