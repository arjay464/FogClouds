using Mirror;
using UnityEngine;
using FogClouds;

public class PlayerNetworkAgent : NetworkBehaviour
{
    public static PlayerNetworkAgent LocalAgent { get; private set; }

    public bool _registered = false;

    public override void OnStartLocalPlayer()
    {
        LocalAgent = this;
    }

    public override void OnStartServer()
    {
        if (_registered) return;
        _registered = true;

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

    [Command]
    public void CmdQueueCardById(int cardInstanceId, bool upcast)
    {
        GameManager.Instance.HandleQueueCard(this, cardInstanceId, upcast);
    }
    [Command]
    public void CmdCommitPower()
    {
        GameManager.Instance.HandleCommitPower(this);
    }

    [Command]
    public void CmdCommitStrategy()
    {
        GameManager.Instance.HandleCommitStrategy(this);
    }

    [Command]
    public void CmdMarketCrash()
    {
        GameManager.Instance.HandleMarketCrash(this);
    }

    [Command]
    public void CmdSlightOfHand(int cardInstanceId)
    {
        GameManager.Instance.HandleSlightOfHand(this, cardInstanceId);
    }

    [Command]
    public void CmdBlessedDiaryTarget(string permanentId)
    {
        GameManager.Instance.HandleBlessedDiaryTarget(this, permanentId);
    }

    [Command]
    public void CmdAncientTelescope(string nodeId)
    {
        GameManager.Instance.HandleAncientTelescope(this, nodeId);
    }

    [Command]
    public void CmdPlayInstantWithTarget(int cardInstanceId, int targetInstanceId)
    {
        GameManager.Instance.HandlePlayInstantWithTarget(this, cardInstanceId, targetInstanceId);
    }
    [Command]
    public void CmdQueueCardWithTarget(int cardInstanceId, bool upcast, int targetInstanceId)
    {
        GameManager.Instance.HandleQueueCardWithTarget(this, cardInstanceId, upcast, targetInstanceId);
    }
}