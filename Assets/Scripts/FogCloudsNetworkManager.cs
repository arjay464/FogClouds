using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class FogCloudsNetworkManager : NetworkManager
{
    private readonly HashSet<NetworkConnectionToClient> _addedPlayers = new();

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (_addedPlayers.Contains(conn)) return;

        _addedPlayers.Add(conn);
        base.OnServerAddPlayer(conn);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        _addedPlayers.Remove(conn);
        base.OnServerDisconnect(conn);
    }
    public override void OnStartHost()
    {
        base.OnStartHost();
        Debug.Log("[FogCloudsNetworkManager] Host started successfully.");
    }
}