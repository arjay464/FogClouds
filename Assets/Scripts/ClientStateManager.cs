using System;
using UnityEngine;
using FogClouds;

public class ClientStateManager : MonoBehaviour
{
    public static ClientStateManager Instance { get; private set; }

    // Current state — always reflects the last broadcast received
    public ClientGameStateView CurrentState { get; private set; }

    // UI components subscribe to this to know when to re-render
    public event Action<ClientGameStateView> OnStateUpdated;

    // Fired specifically when GameOver becomes true
    public event Action<int> OnGameOver; // int = WinnerPlayerId

    // Fired when phase changes
    public event Action<TurnPhase> OnPhaseChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ReceiveState(ClientGameStateView view)
    {
        TurnPhase previousPhase = CurrentState?.CurrentPhase ?? TurnPhase.TurnStart;

        CurrentState = view;

        OnStateUpdated?.Invoke(view);

        if (view.CurrentPhase != previousPhase)
            OnPhaseChanged?.Invoke(view.CurrentPhase);

        if (view.GameOver)
            OnGameOver?.Invoke(view.WinnerPlayerId);
    }
}