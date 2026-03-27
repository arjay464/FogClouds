using UnityEngine;
using UnityEngine.UIElements;
using Mirror;

public class LobbyMenuController : MonoBehaviour
{
    private UIDocument _doc;
    private Button _hostButton;
    private Label _statusLabel;

    private void Awake()
    {
        _doc = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        var root = _doc.rootVisualElement;
        _hostButton = root.Q<Button>("host-button");
        _statusLabel = root.Q<Label>("status-label");

        _hostButton.clicked += OnHostClicked;
    }

    private void OnDisable()
    {
        _hostButton.clicked -= OnHostClicked;
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

    public void OnConnected()
    {
        gameObject.SetActive(false);
    }
}