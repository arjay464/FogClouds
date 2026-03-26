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
    {
        // ── Starting deck ──────────────────────────────────────────
        _effects["knife_strike_base"] = new DealDamageEffect(3);
        _effects["knife_strike_upcast"] = new DealDamageEffect(3, lifesteal: true);
        _effects["cultellara"] = new AddDaggerEffect();
        _effects["brace"] = new GainShieldEffect(2);
        _effects["cha_cha_card"] = new SpawnPermanentEffect("cha_cha", "Cha Cha - Loyal Chupacabra", turnsRemaining: 2);
        _effects["cultivita"] = new HealEffect(3);

        // ── Power pool ─────────────────────────────────────────────
        _effects["blood_hex"] = new BloodHexEffect();
        _effects["twin_strike_base"] = new MultiHitEffect(hitCount: 2, damagePerHit: 2);
        _effects["twin_strike_upcast"] = new MultiHitEffect(hitCount: 2, damagePerHit: 2, lifesteal: true);
        _effects["ferravallum"] = new GainShieldEffect(5);
        _effects["cursed_goblet_card"] = new SpawnPermanentEffect("cursed_goblet", "Cursed Goblet", turnsRemaining: -1);
        _effects["hemorrhage_base"] = new HemorrhageEffect();
        _effects["hemorrhage_upcast"] = new HemorrhageEffect();
        _effects["bloodrush_base"] = new BloodrushEffect();
        _effects["bloodrush_upcast"] = new BloodrushEffect(); // upcast version doubles via IsAttack flag
        _effects["arterial_cut"] = new ArterialCutEffect();
        _effects["hidden_daggers"] = new HiddenDaggersEffect();
        _effects["totem_of_sharpness"] = new TotemOfSharpnessEffect();
        _effects["daggers_in_the_dark"] = new DaggersInTheDarkEffect();

        // ── Strategy pool ──────────────────────────────────────────
        _effects["through_bloodshed"] = new ThroughBloodshedEffect();
        _effects["multiculta"] = new MulticultaEffect();
        _effects["sanguine_pact"] = new SanguinePactEffect();
        _effects["recover"] = new RecoverEffect();
        _effects["mirror_of_moonlight"] = new MirrorOfMoonlightEffect();
        _effects["totem_of_sacrifice"] = new TotemOfSacrificeEffect();
        _effects["ferricidium"] = new FerricidiumEffect();
        _effects["lex_noctis"] = new LexNoctisEffect();
        _effects["banishment"] = new BanishmentEffect();
        _effects["totem_of_progress"] = new TotemOfProgressEffect();

        // ── Spawned cards ──────────────────────────────────────────
        _effects["thrown_dagger"] = new ThrownDaggerEffect();
        _effects["blood_hex_curse"] = new NoOpEffect();

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