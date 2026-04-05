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
        _effects["ferravallum"] = new GainShieldEffect(5);
        _effects["cursed_goblet_card"] = new SpawnPermanentEffect("cursed_goblet", "Cursed Goblet", turnsRemaining: -1);
        _effects["hemorrhage_base"] = new HemorrhageEffect();
        _effects["hemorrhage_upcast"] = new HemorrhageEffect();
        _effects["bloodrush_base"] = new BloodrushEffect();
        _effects["bloodrush_upcast"] = new BloodrushEffect();
        _effects["arterial_cut"] = new ArterialCutEffect();
        _effects["hidden_daggers"] = new HiddenDaggersEffect();
        _effects["totem_of_sharpness"] = new TotemOfSharpnessEffect();
        _effects["daggers_in_the_dark"] = new DaggersInTheDarkEffect();
        _effects["sanguimortis_base"] = new SanguimortisEffect();
        _effects["sanguimortis_upcast"] = new SanguimortisEffect(lifesteal: true);
        _effects["frenzy_base"] = new FrenzyEffect();
        _effects["frenzy_upcast"] = new FrenzyEffect(lifesteal: true);
        _effects["vulnifera_base"] = new VulniferaEffect();
        _effects["vulnifera_upcast"] = new VulniferaEffect(lifesteal: true);
        _effects["bloodthirst_base"] = new BloodthirstEffect();
        _effects["bloodthirst_upcast"] = new BloodthirstEffect(lifesteal: true);
        _effects["weakpoint_strike_base"] = new WeakpointStrikeEffect();
        _effects["weakpoint_strike_upcast"] = new WeakpointStrikeEffect(lifesteal: true);
        _effects["shadowstep_base"] = new ShadowstepEffect();
        _effects["shadowstep_upcast"] = new ShadowstepEffect(lifesteal: true);
        _effects["brand_of_fragility_base"] = new BrandOfFragilityEffect();
        _effects["brand_of_fragility_upcast"] = new BrandOfFragilityEffect(lifesteal: true);
        _effects["inanivora"] = new InanivoraEffect();
        _effects["inanivora_upcast"] = new InanivoraEffect(lifesteal: true);
        _effects["deaths_call_base"] = new DeathsCallEffect();
        _effects["deaths_call_upcast"] = new DeathsCallEffect(lifesteal: true);
        _effects["exsanguinate_base"] = new ExsanguinateEffect();
        _effects["exsanguinate_upcast"] = new ExsanguinateEffect(lifesteal: true);
        _effects["lacerate"] = new LacerateEffect();

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
        _effects["parry"] = new ParryEffect();
        _effects["blood_mirror"] = new BloodMirrorEffect();
        _effects["accelerated_cut"] = new AcceleratedCutEffect();
        _effects["consume"] = new ConsumeEffect();
        _effects["clarity"] = new ClarityEffect();
        _effects["devovita"] = new DevovitaEffect();
        _effects["lunge"] = new LungeEffect();
        _effects["totem_of_warding"] = new TotemOfWardingEffect();
        _effects["one_for_one"] = new OneForOneEffect();
        _effects["vampiric_foresight"] = new VampiricForesightEffect();

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