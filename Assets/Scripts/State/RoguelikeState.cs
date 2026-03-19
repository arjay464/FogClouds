using System;
using UnityEngine;
using System.Collections.Generic;

namespace FogClouds
{
    [Serializable]
    public class RoguelikeState
    {
        public List<string> Player1ChosenUpgrades;
        public List<string> Player2ChosenUpgrades;
        public RoguelikeOffers Player1UpcomingOffers;
        public RoguelikeOffers Player2UpcomingOffers;

        public RoguelikeState()
        {
            Player1ChosenUpgrades = new List<string>();
            Player2ChosenUpgrades = new List<string>();
            Player1UpcomingOffers = new RoguelikeOffers();
            Player2UpcomingOffers = new RoguelikeOffers();
        }
    }

    [Serializable]
    public class RoguelikeOffers
    {
        public List<string> PowerOffers;    // 3 CardIds
        public List<int> PowerPrices;       // parallel list — price per card
        public List<string> StrategyOffers; // 3 CardIds
        public List<int> StrategyPrices;

        public RoguelikeOffers()
        {
            PowerOffers = new List<string>();
            PowerPrices = new List<int>();
            StrategyOffers = new List<string>();
            StrategyPrices = new List<int>();
        }

        public RoguelikeOffers Clone() //deep copy
        {
            return new RoguelikeOffers
            {
                PowerOffers = new List<string>(this.PowerOffers),
                PowerPrices = new List<int>(this.PowerPrices),
                StrategyOffers = new List<string>(this.StrategyOffers),
                StrategyPrices = new List<int>(this.StrategyPrices),
            };
        }
    }

    [Serializable]
    public class ShopOffer
    {
        public List<string> PowerCards;
        public List<int> PowerPrices;
        public List<string> StrategyCards;
        public List<int> StrategyPrices;
        public List<string> ColorlessCards;
        public List<int> ColorlessPrices;
        public List<string> Passives;
        public List<int> PassivePrices;
        public int HpRegenSmallCost;
        public int HpRegenLargeCost;
        public int SightSmallCost;
        public int SightLargeCost;
        public int PersistentResourceCost;

        public ShopOffer()
        {
            PowerCards = new List<string>();
            PowerPrices = new List<int>();
            StrategyCards = new List<string>();
            StrategyPrices = new List<int>();
            ColorlessCards = new List<string>();
            ColorlessPrices = new List<int>();
            Passives = new List<string>();
            PassivePrices = new List<int>();
        }
    }
}