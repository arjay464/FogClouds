using System.Collections.Generic;
using UnityEngine;
using FogClouds;

public class CardEffectRegistry : MonoBehaviour
{
    public static CardEffectRegistry Instance { get; private set; }

    private Dictionary<string, ICardEffect> _effects = new();

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
    {   //Knife Strike Base - damage
        _effects["knife_strike_base"] = new DealDamageEffect(3, lifesteal: false);
        // Knife Strike Upcast — damage + lifesteal
        _effects["knife_strike_upcast"] = new DealDamageEffect(3, lifesteal: true);

        // Cultellara — add 1 Dagger
        _effects["cultellara"] = new AddDaggerEffect();

        // Brace — gain 2 shield
        _effects["brace"] = new GainShieldEffect(2);

        // Cha Cha — spawn permanent (handled by SpawnPermanentEffect)
        _effects["cha_cha_card"] = new SpawnPermanentEffect("cha_cha", "Cha Cha - Loyal Chupacabra", turnsRemaining: 2);

        // Cursed Goblet — spawn permanent
        _effects["cursed_goblet_card"] = new SpawnPermanentEffect("cursed_goblet", "Cursed Goblet", turnsRemaining: -1);

        // Ferravallum — gain 5 shield
        _effects["ferravallum"] = new GainShieldEffect(5);

        // Cultivita — heal 3 HP
        _effects["cultivita"] = new HealEffect(3);

        // Twin Slash Base — 2 hits of 2 damage
        _effects["twin_slash_base"] = new MultiHitEffect(hitCount: 2, damagePerHit: 2, lifesteal: false);

        //Twin Slash Upcast - 2 hits of 2 damage + lifesteal
        _effects["twin_slash_upgraded"] = new MultiHitEffect(hitCount: 2, damagePerHit: 2, lifesteal: true);

        Debug.Log($"[CardEffectRegistry] Registered {_effects.Count} effects.");
    }

    public ICardEffect GetEffect(string effectId)
    {
        if (_effects.TryGetValue(effectId, out var effect))
            return effect;

        Debug.LogWarning($"[CardEffectRegistry] No effect found for id: {effectId}");
        return null;
    }
}