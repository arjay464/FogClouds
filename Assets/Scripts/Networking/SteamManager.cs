using UnityEngine;
using Steamworks;

public class SteamManager : MonoBehaviour
{
    private static SteamManager _instance;
    public static bool Initialized { get; private set; } = false;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (!SteamAPI.Init())
        {
            Debug.LogError("[SteamManager] SteamAPI.Init() failed. Is Steam running?");
            return;
        }

        Initialized = true;
        Debug.Log("[SteamManager] Steam initialized successfully.");
    }

    private void Update()
    {
        if (Initialized)
            SteamAPI.RunCallbacks();
    }

    private void OnApplicationQuit()
    {
        if (Initialized)
            SteamAPI.Shutdown();
    }
}