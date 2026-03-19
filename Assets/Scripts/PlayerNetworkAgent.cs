using Mirror;
using UnityEngine;
using FogClouds;

public class PlayerNetworkAgent : NetworkBehaviour
{
    public static PlayerNetworkAgent LocalAgent { get; private set; }

    public override void OnStartLocalPlayer()
    {
        LocalAgent = this;
    }

    public override void OnStartServer()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[PlayerNetworkAgent] GameManager instance not found.");
            return;
        }

        GameManager.Instance.RegisterPlayer(this);
    }

    [Command]
    public void CmdQueueCard(bool upcast)
    {
        GameManager.Instance.HandleQueueFirstCard(this, upcast: false);
    }

    void OnGUI()
    {
        if (!isLocalPlayer) return;

        GUILayout.BeginArea(new Rect(10, 110, 200, 600));

        if (GUILayout.Button("Queue First Card"))
            CmdQueueCard(upcast: false);

        if (GUILayout.Button("Queue First Card (Upcast)"))
            CmdQueueCard(upcast: true);

        if (GUILayout.Button("Play First Instant"))
            CmdPlayFirstInstant();

        if (GUILayout.Button("End Turn"))
            CmdSubmitEndTurn();

        if (GUILayout.Button("Play First Permanent"))
            CmdPlayFirstPermanent();

        if (GUILayout.Button("Choose POWER (stub)"))
            CmdSubmitRoguelikeChoice(RoguelikeCategory.Power, "");

        if (GUILayout.Button("Choose INSIGHT"))
            CmdChooseInsight();

        if (GUILayout.Button("Unlock CharacterIdentity"))
            CmdUnlockNode("character_identity");

        if (GUILayout.Button("Submit Roguelike Choice"))
            CmdSubmitRoguelikeChoice();

        if (GUILayout.Button("Choose Power [0]"))
            CmdChoosePower(0);

        if (GUILayout.Button("Choose Strategy [0]"))
            CmdChooseStrategy(0);

        if (GUILayout.Button("Submit Auction Bids (1,1,1)"))
            CmdSubmitAuctionBids(1, 1, 1);

        GUILayout.EndArea();
    }

    [Command]
    public void CmdPlayFirstInstant()
    {
        GameManager.Instance.HandlePlayFirstInstant(this);
    }

    [Command]
    public void CmdSubmitEndTurn()
    {
        GameManager.Instance.HandleEndTurnIntent(this);
    }

    [Command]
    public void CmdPlayInstant(int cardInstanceId)
    {
        GameManager.Instance.HandlePlayInstant(this, cardInstanceId);
    }

    [Command]
    public void CmdPlayPermanent(int cardInstanceId)
    {
        GameManager.Instance.HandlePlayPermanent(this, cardInstanceId);
    }

    [Command]
    public void CmdPlayFirstPermanent()
    {
        GameManager.Instance.HandlePlayFirstPermanent(this);
    }

    [Command]
    public void CmdUnlockNode(string nodeId)
    {
        GameManager.Instance.HandleUnlockNode(this, nodeId);
    }

    [Command]
    public void CmdSubmitRoguelikeChoice(RoguelikeCategory category, string selectionId)
    {
        GameManager.Instance.HandleRoguelikeChoice(this, category, selectionId);
    }
    [Command]
    public void CmdChooseInsight()
    {
        GameManager.Instance.HandleChooseInsight(this);
    }

    [Command]
    public void CmdSubmitRoguelikeChoice()
    {
        GameManager.Instance.HandleSubmitRoguelikeChoice(this);
    }

    [Command]
    public void CmdChoosePower(int offerIndex)
    {
        GameManager.Instance.HandleChoosePower(this, offerIndex);
    }

    [Command]
    public void CmdChooseStrategy(int offerIndex)
    {
        GameManager.Instance.HandleChooseStrategy(this, offerIndex);
    }
    [Command]
    public void CmdShopPurchase(ShopPurchaseType purchaseType, int index)
    {
        GameManager.Instance.HandleShopPurchase(this, purchaseType, index);
    }

    [Command]
    public void CmdShopDone()
    {
        GameManager.Instance.HandleShopDone(this);
    }

    [Command]
    public void CmdSubmitAuctionBids(int bid0, int bid1, int bid2)
    {
        GameManager.Instance.HandleSubmitAuctionBids(this, bid0, bid1, bid2);
    }

    [Command]
    public void CmdSubmitEventChoice(string choice)
    {
        GameManager.Instance.HandleEventChoice(this, choice);
    }
}