using UnityEngine;
using Mirror;

public class MirrorCleanUp : MonoBehaviour
{
    void OnDestroy(){
        if(NetworkClient.isConnected){
            
            NetworkClient.Disconnect();
        }
    }
}
