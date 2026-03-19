using UnityEngine;
using System;
using System.Collections.Generic;

namespace FogClouds
{
    // Represents one card's slot in a player's queue (during MainPhase)
    // or in the merged queue (during QueueResolution).
    [Serializable]
    public class QueueEntry
    {
        //PlayerId of the player who queued this card (0 or 1).
        public int OwnerId;

        //The card instance that was queued.
        public CardInstance Card;

        // The card's current speed. Starts at Card.ModifiedSpeed when enqueued.
        // Can be modified by effects while in queue.
        public int CurrentSpeed;

        // Submission order within this player's queue for the current turn.
        // Used as a stable key for UI ordering during MainPhase.
        // NOT used as a tiebreaker at resolution — ties are random.
        public int QueuePosition;

        public int TieBreaker;

        public QueueEntry() { }

        public QueueEntry(int ownerId, CardInstance card, int queuePosition)
        {

            OwnerId = ownerId;
            Card = card;
            CurrentSpeed = card.ModifiedSpeed;
            QueuePosition = queuePosition;
        }

        public override string ToString()
        {
            return $"[P{OwnerId} pos:{QueuePosition}] {Card} spd:{CurrentSpeed}";
        }
    }

}
