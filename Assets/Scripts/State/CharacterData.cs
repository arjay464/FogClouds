using UnityEngine;

namespace FogClouds
{
    [CreateAssetMenu(menuName = "FogClouds/CharacterData")]
    public class CharacterData : ScriptableObject
    {
        [field: SerializeField] public string CharacterId { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public int BaseHP { get; private set; }
        [field: SerializeField] public int BasePerTurnResourceMax { get; private set; }
        [field: SerializeField] public string PerTurnResourceName { get; private set; }
        [field: SerializeField] public string PersistentResourceName { get; private set; }
        [field: SerializeField] public string PassiveName { get; private set; }
        [field: SerializeField] public string PassiveDescription { get; private set; }
    }
}