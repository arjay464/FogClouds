using System.Collections.Generic;
using UnityEngine;
using FogClouds;

public class GameEventRegistry : MonoBehaviour
{
    public static GameEventRegistry Instance { get; private set; }

    private Dictionary<string, IGameEventEffect> _effects = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        RegisterEffects();
    }

    private void RegisterEffects()
    {
        // Phase 5: register events here
        // _effects["blood_tithe"] = new BloodTitheEffect();

        Debug.Log($"[GameEventRegistry] Registered {_effects.Count} events.");
    }

    public IGameEventEffect GetEffect(string eventId)
    {
        if (_effects.TryGetValue(eventId, out var effect))
            return effect;

        Debug.LogWarning($"[GameEventRegistry] No effect found for id: {eventId}");
        return null;
    }
}