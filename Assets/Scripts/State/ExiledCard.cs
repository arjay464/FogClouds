using System;
using FogClouds;

namespace FogClouds
{
    [Serializable]
    public class ExiledCard
    {
        public CardInstance Card;
        public int TurnsRemaining;

        public ExiledCard(CardInstance card, int turnsRemaining)
        {
            Card = card;
            TurnsRemaining = turnsRemaining;
        }
    }
}