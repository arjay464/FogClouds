using UnityEngine;

namespace FogClouds
{
    [CreateAssetMenu(menuName = "FogClouds/CardPoolRegistry")]
    public class CardPoolRegistry : ScriptableObject
    {
        [field: SerializeField] public CardDefinition[] ThessaPower { get; private set; }
        [field: SerializeField] public CardDefinition[] ThessaStrategy { get; private set; }
        [field: SerializeField] public CardDefinition[] ChizuPower { get; private set; }
        [field: SerializeField] public CardDefinition[] ChizuStrategy { get; private set; }
        [field: SerializeField] public CardDefinition[] Colorless { get; private set; }
        [field: SerializeField] public CardDefinition[] Auction { get; private set; }
        [field: SerializeField] public PassiveDefinition[] Passives { get; private set; }

        public CardDefinition[] GetPowerPool(string characterId)
        {
            return characterId == "chizu" ? ChizuPower : ThessaPower;
        }

        public CardDefinition[] GetStrategyPool(string characterId)
        {
            return characterId == "chizu" ? ChizuStrategy : ThessaStrategy;
        }
    }
}