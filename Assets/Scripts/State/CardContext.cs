using System.Collections.Generic;
using System.Linq;

namespace FogClouds
{
    // Rich context object populated by GameManager immediately before each card effect Apply().
    // Pre-computes all commonly needed derivations so effect authors don't have to.
    // Read-only — mutate game state through GameState and PlayerState directly as before.
    // DO NOT USE TO CALCULATE INTERMEDIARY VALUES. THIS DATA IS ONLY COMPUTED BEFORE A CARD EFFECT SO MULTI-HIT OR MULTI-EFFECT CARDS WILL GET STALE DATA AFTER THE FIRST EFFECT. 
    public class CardContext
    {
        // ── Core references ───────────────────────────────────────
        public readonly GameState State;
        public readonly QueueEntry Entry;

        // ── Player shortcuts ──────────────────────────────────────
        public readonly PlayerState Caster;
        public readonly PlayerState Opponent;

        // ── Card facts ────────────────────────────────────────────
        public readonly CardInstance Card;
        public readonly bool WasUpcast;
        public readonly bool IsAttack;
        public readonly int BonusDamage;
        public readonly int TargetInstanceId;
        public readonly int CurrentSpeed;

        // ── Caster state snapshots (live, not snapshot) ───────────
        public readonly int CasterHP;
        public readonly int CasterShield;
        public readonly int CasterSigils;
        public readonly int CasterSigilsMax;
        public readonly int CasterMistforce;
        public readonly int CasterSilver;
        public readonly int CasterBloodSpent;
        public readonly int CasterDaggersSpent;
        public readonly int CasterSigilsSpent;
        public readonly int CasterHandSize;
        public readonly int CasterDeckSize;
        public readonly int CasterDiscardSize;
        public readonly int CasterBoardSize;
        public readonly bool CasterMirrorActive;
        public readonly bool CasterParryActive;
        public readonly bool CasterConcealmentUsed;
        public readonly List<BoardPermanent> CasterBoard;
        public readonly List<Passive> CasterPassives;
        public readonly List<StatusEffect> CasterStatusEffects;

        // ── Opponent state snapshots (live) ───────────────────────
        public readonly int OpponentHP;
        public readonly int OpponentShield;
        public readonly int OpponentSigils;
        public readonly int OpponentSigilsMax;
        public readonly int OpponentMistforce;
        public readonly int OpponentSilver;
        public readonly int OpponentHandSize;
        public readonly int OpponentDeckSize;
        public readonly int OpponentDiscardSize;
        public readonly int OpponentBoardSize;
        public readonly List<BoardPermanent> OpponentBoard;
        public readonly List<Passive> OpponentPassives;
        public readonly List<StatusEffect> OpponentStatusEffects;

        // ── Queue facts (state of merged queue at resolution time) ─
        public readonly int CardsRemainingInQueue;
        public readonly int OpponentCardsRemaining;
        public readonly int OwnCardsRemaining;
        public readonly bool IsFirstToResolve;

        // ── Bleed / Poison helpers ────────────────────────────────
        public readonly int OpponentBleedStacks;
        public readonly int OpponentPoisonStacks;
        public readonly int CasterBleedStacks;
        public readonly int CasterPoisonStacks;

        // ── Turn facts ────────────────────────────────────────────
        public readonly int TurnNumber;
        public readonly TurnPhase CurrentPhase;
        public readonly bool CasterBelowHalfHP;
        public readonly bool OpponentBelowHalfHP;
        public readonly bool OpponentHasNoPermanents;
        public readonly bool OpponentHasNoSigils;

        // ── Character identity ────────────────────────────────────
        public readonly string CasterCharacterId;
        public readonly string OpponentCharacterId;
        public readonly bool CasterIsThessa;
        public readonly bool CasterIsChizu;

        public CardContext(QueueEntry entry, GameState state)
        {
            State = state;
            Entry = entry;
            Card = entry.Card;
            WasUpcast = entry.WasUpcast;
            IsAttack = entry.Card.IsAttack;
            BonusDamage = entry.BonusDamage;
            TargetInstanceId = entry.TargetInstanceId;
            CurrentSpeed = entry.CurrentSpeed;

            Caster = state.GetPlayer(entry.OwnerId);
            Opponent = state.GetOpponent(entry.OwnerId);

            // Caster
            CasterHP = Caster.HP;
            CasterShield = Caster.Shield;
            CasterSigils = Caster.Resources.PerTurnResource;
            CasterSigilsMax = Caster.Resources.PerTurnResourceMax + Caster.Resources.BonusPerTurnResource;
            CasterMistforce = Caster.Resources.PersistentResource;
            CasterSilver = Caster.Silver;
            CasterBloodSpent = Caster.BloodSpentThisTurn;
            CasterDaggersSpent = Caster.DaggersSpentThisTurn;
            CasterSigilsSpent = Caster.SigilsSpentThisTurn;
            CasterHandSize = Caster.Hand.Count;
            CasterDeckSize = Caster.Deck.Count;
            CasterDiscardSize = Caster.Discard.Count;
            CasterBoardSize = Caster.Board.Count;
            CasterMirrorActive = Caster.MirrorActive;
            CasterParryActive = Caster.ParryActive;
            CasterConcealmentUsed = Caster.ConcealmentUsedThisTurn;
            CasterBoard = Caster.Board;
            CasterPassives = Caster.Passives;
            CasterStatusEffects = Caster.StatusEffects;
            CasterBleedStacks = Caster.StatusEffects.Find(e => e.EffectId == "bleed")?.Value ?? 0;
            CasterPoisonStacks = Caster.StatusEffects.Find(e => e.EffectId == "poison")?.Value ?? 0;
            CasterBelowHalfHP = Caster.HP < Caster.Character.BaseHP / 2f;
            CasterCharacterId = Caster.Character.CharacterId;
            CasterIsThessa = CasterCharacterId == "thessa";
            CasterIsChizu = CasterCharacterId == "chizu";

            // Opponent
            OpponentHP = Opponent.HP;
            OpponentShield = Opponent.Shield;
            OpponentSigils = Opponent.Resources.PerTurnResource;
            OpponentSigilsMax = Opponent.Resources.PerTurnResourceMax + Opponent.Resources.BonusPerTurnResource;
            OpponentMistforce = Opponent.Resources.PersistentResource;
            OpponentSilver = Opponent.Silver;
            OpponentHandSize = Opponent.Hand.Count;
            OpponentDeckSize = Opponent.Deck.Count;
            OpponentDiscardSize = Opponent.Discard.Count;
            OpponentBoardSize = Opponent.Board.Count;
            OpponentBoard = Opponent.Board;
            OpponentPassives = Opponent.Passives;
            OpponentStatusEffects = Opponent.StatusEffects;
            OpponentBleedStacks = Opponent.StatusEffects.Find(e => e.EffectId == "bleed")?.Value ?? 0;
            OpponentPoisonStacks = Opponent.StatusEffects.Find(e => e.EffectId == "poison")?.Value ?? 0;
            OpponentBelowHalfHP = Opponent.HP < Opponent.Character.BaseHP / 2f;
            OpponentHasNoPermanents = Opponent.Board.Count == 0;
            OpponentHasNoSigils = Opponent.Resources.PerTurnResource == 0;
            OpponentCharacterId = Opponent.Character.CharacterId;

            // Queue
            CardsRemainingInQueue = state.MergedQueue.Count;
            OpponentCardsRemaining = state.MergedQueue.Count(e => e.OwnerId != entry.OwnerId);
            OwnCardsRemaining = state.MergedQueue.Count(e => e.OwnerId == entry.OwnerId);
            IsFirstToResolve = state.MergedQueue.All(e => e.OwnerId == entry.OwnerId);

            // Turn
            TurnNumber = state.TurnNumber;
            CurrentPhase = state.CurrentPhase;
        }

        // ── Convenience helpers ───────────────────────────────────

        public bool CasterHasPassive(string passiveId) =>
            Caster.Passives.Exists(p => p.PassiveId == passiveId);

        public bool OpponentHasPassive(string passiveId) =>
            Opponent.Passives.Exists(p => p.PassiveId == passiveId);

        public bool CasterHasStatusEffect(string effectId) =>
            Caster.StatusEffects.Exists(e => e.EffectId == effectId);

        public bool OpponentHasStatusEffect(string effectId) =>
            Opponent.StatusEffects.Exists(e => e.EffectId == effectId);

        public bool CasterHasPermanent(string permanentId) =>
            Caster.Board.Exists(p => p.PermanentId == permanentId);

        public bool OpponentHasPermanent(string permanentId) =>
            Opponent.Board.Exists(p => p.PermanentId == permanentId);

        public Passive CasterGetPassive(string passiveId) =>
            Caster.Passives.Find(p => p.PassiveId == passiveId);

        public StatusEffect OpponentGetStatus(string effectId) =>
            Opponent.StatusEffects.Find(e => e.EffectId == effectId);

        public StatusEffect CasterGetStatus(string effectId) =>
            Caster.StatusEffects.Find(e => e.EffectId == effectId);
    }
}