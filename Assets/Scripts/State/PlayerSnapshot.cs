using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FogClouds
{
    // A frozen copy of a player's state, taken at the TurnStart → MainPhase transition
    // (after draw, after resource refresh, before any player input).
    // ALL fog-filtered views of the opponent read from this object — never from live PlayerState.
    // This is a hard architectural rule. It eliminates the incentive to delay main phase
    // actions to protect information.
    // Queues are intentionally excluded: queues are empty at snapshot time.
    [Serializable]
    public class PlayerSnapshot
    {
        public int PlayerID;
        public int HP;
        public int Shield;

        //Shallow copy is safe — CharacterData is immutable.
        public string CharacterId;

        public List<CardInstance> Hand;
        public int DeckCount;
        public List<CardInstance> Deck;
        public int DiscardCount;
        public List<CardInstance> Discard;

        public List<Passive> Passives;
        public int PassiveCount;

        public ResourceState Resources;
        public int Silver;

        public List<BoardPermanent> Board;

        //Pre-generated roguelike offers for the upcoming roguelike phase.
        public RoguelikeOffers UpcomingOffers;

        // Creates a PlayerSnapshot from a live PlayerState.
        // Deep copies all mutable collections so the snapshot is fully independent of live state.
        public static PlayerSnapshot From(PlayerState source)
        {
            return new PlayerSnapshot
            {
                PlayerID = source.PlayerId,
                HP = source.HP,
                Shield = source.Shield,
                CharacterId = source.Character.CharacterId,
                Hand = source.Hand.Select(c => c.Clone()).ToList(),
                DeckCount = source.Deck.Count,
                Deck = source.Deck.Select(c => c.Clone()).ToList(),
                DiscardCount = source.Discard.Count,
                Discard = source.Discard.Select(c => c.Clone()).ToList(),
                Passives = source.Passives.Select(p => p.Clone()).ToList(),
                PassiveCount = source.Passives.Count,
                Resources = source.Resources.Clone(),
                Silver = source.Silver,
                Board = source.Board.Select(b => b.Clone()).ToList(),
                UpcomingOffers = source.UpcomingOffers?.Clone()
            };
        }
    }
}
