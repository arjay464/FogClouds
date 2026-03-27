using UnityEngine;
using UnityEngine.UIElements;
using Mirror;
using kcp2k;

public class LobbyMenuController : MonoBehaviour
{
    private UIDocument _doc;
    private Button _hostButton;
    private Button _localHostButton;
    private TextField _localIpField;
    private Button _localJoinButton;
    private Label _statusLabel;

    private void Awake()
    {
        _doc = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        var root = _doc.rootVisualElement;
        _hostButton = root.Q<Button>("host-button");
        _localHostButton = root.Q<Button>("local-host-button");
        _statusLabel = root.Q<Label>("status-label");
        _localJoinButton = root.Q<Button>("local-join-button");
        _localIpField = root.Q<TextField>("local-ip-field");

        _hostButton.clicked += OnHostClicked;
        _localHostButton.clicked += OnLocalHostClicked;
        _localJoinButton.clicked += OnLocalJoinClicked;

        // Disable Steam button immediately if Steam isn't up
        bool steamReady = SteamManager.Initialized && SteamLobbyManager.Instance != null;
        _hostButton.SetEnabled(steamReady);
        if (!steamReady)
            _statusLabel.text = "Steam unavailable — use Local Host.";
    }

    private void OnDisable()
    {
        _hostButton.clicked -= OnHostClicked;
        _localHostButton.clicked -= OnLocalHostClicked;
        _localJoinButton.clicked -= OnLocalJoinClicked;
    }

    private void OnHostClicked()
    {
        if (!SteamManager.Initialized)
        {
            _statusLabel.text = "Steam not initialized.";
            return;
        }

        _hostButton.SetEnabled(false);
        _statusLabel.text = "Creating lobby...";
        SteamLobbyManager.Instance.HostLobby();
    }
    private void OnLocalHostClicked()
    {
        _localHostButton.SetEnabled(false);
        _statusLabel.text = "Starting local host...";

        var nm = (FogCloudsNetworkManager)NetworkManager.singleton;

        var kcp = nm.GetComponent<KcpTransport>();
        if (kcp == null)
        {
            _statusLabel.text = "KcpTransport not found on NetworkManager.";
            _localHostButton.SetEnabled(true);
            return;
        }

        nm.transport = kcp;
        Transport.active = kcp;
        nm.networkAddress = "localhost";
        nm.StartHost();
        nm.ServerChangeScene("SampleScene");
        Debug.Log("[LobbyMenuController] Local host started on localhost:7777.");
    }

    private void OnLocalJoinClicked()
    {
        _localJoinButton.SetEnabled(false);
        _statusLabel.text = "Joining local host...";

        var nm = (FogCloudsNetworkManager)NetworkManager.singleton;

        var kcp = nm.GetComponent<KcpTransport>();
        if (kcp == null)
        {
            _statusLabel.text = "KcpTransport not found on NetworkManager.";
            _localJoinButton.SetEnabled(true);
            return;
        }

        nm.transport = kcp;
        Transport.active = kcp;
        nm.networkAddress = string.IsNullOrWhiteSpace(_localIpField.value) ? "localhost" : _localIpField.value.Trim();
        nm.StartClient();
        Debug.Log($"[LobbyMenuController] Local client started, connecting to {nm.networkAddress}:7777.");
    }

    public void OnConnected()
    {
        gameObject.SetActive(false);
    }
}