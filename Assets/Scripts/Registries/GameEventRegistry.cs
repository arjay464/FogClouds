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
        _effects["chopping_block"] = new ChoppingBlockEffect();
        _effects["blessing_in_disguise"] = new BlessingInDisguiseEffect();
        _effects["calm_before_the_storm"] = new CalmBeforeTheStormEffect();
        _effects["too_good_to_be_true"] = new TooGoodToBeTrueEffect();
        _effects["deal_with_the_devil"] = new DealWithTheDevilEffect();
        _effects["writing_on_the_wall"] = new WritingOnTheWallEffect();
        _effects["fortune_favors_the_bold"] = new FortuneFavorsTheBoldEffect();
        Debug.Log($"[GameEventRegistry] Registered {_effects.Count} events.");

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
