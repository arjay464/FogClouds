using UnityEngine;
using Mirror;

public class PhaseManager : NetworkBehaviour
{

    private DeckManager deckManager;

    void Start()
    {
        if(isLocalPlayer){
            deckManager = FindFirstObjectByType<DeckManager>();
        }
    }
    public void OnTurnEnd()
    {
        Debug.Log("Turn Ended.");
        if(deckManager == null){
            Debug.LogError("Still null.");
            return;
        }
        deckManager.OnTurnEnd();
    }
}