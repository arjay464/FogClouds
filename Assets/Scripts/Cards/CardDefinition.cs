using System;
using UnityEngine;

namespace FogClouds
{
    [CreateAssetMenu(fileName = "NewCard", menuName = "FogClouds/CardDefinition")]
    public class CardDefinition : ScriptableObject
    {
        [field: SerializeField] public string CardId { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public string FlavourText { get; private set; }
        [field: SerializeField] public CardType Type { get; private set; }
        [field: SerializeField] public int BaseSpeed { get; private set; }
        [field: SerializeField] public ResourceCost Cost { get; private set; }
        [field: SerializeField] public string OwnerCharacterId { get; private set; }
        [field: SerializeField] public string EffectId { get; private set; }
    }

    [Serializable]
    public struct ResourceCost
    {
        public int Daggers;
        public int Blood;

        public ResourceCost(int daggers = 0, int blood = 0)
        {
            Daggers = daggers;
            Blood = blood;
        }
    }
}