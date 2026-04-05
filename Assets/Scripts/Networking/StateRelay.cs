using Mirror;
using UnityEngine;
using FogClouds;
using Newtonsoft.Json;
using System.Collections.Generic;

public class StateRelay : NetworkBehaviour
{
    public static StateRelay Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [Server]
    public void BroadcastToAll()
    {
        if (_gameState == null)
        {
            Debug.LogWarning("[StateRelay] BroadcastToAll called before GameState was set.");
            return;
        }


        foreach (var agent in _agents)
        {
            int playerId = _agents.IndexOf(agent);
            var view = FogFilter.GenerateView(_gameState, playerId);

            Debug.Log($"[StateRelay] P{playerId} view — OpponentCharacterId: {view.OpponentState?.CharacterId ?? "hidden"}, OpponentHP: {view.OpponentState?.HP}");

            string json = JsonConvert.SerializeObject(view);
            TargetReceiveState(agent.connectionToClient, json);
        }
    }

    [TargetRpc]
    private void TargetReceiveState(NetworkConnection target, string json)
    {
        var view = JsonConvert.DeserializeObject<ClientGameStateView>(json);
        Debug.Log($"[StateRelay] Received state. Phase: {view.CurrentPhase}, Turn: {view.TurnNumber}, OwnHP: {view.OwnState?.HP}");

        if (ClientStateManager.Instance != null)
            ClientStateManager.Instance.ReceiveState(view);
        else
            Debug.LogWarning("[StateRelay] ClientStateManager not found — state not delivered to UI.");
    }

    private GameState _gameState;
    private List<PlayerNetworkAgent> _agents;

    [Server]
    public void Initialize(GameState gameState, List<PlayerNetworkAgent> agents)
    {
        _gameState = gameState;
        _agents = agents;
    }
}