using UnityEngine;
using Mirror;

public class NetworkButtons : MonoBehaviour
{
    void OnGUI(){
        if(!NetworkClient.isConnected && !NetworkServer.active){
            if(GUI.Button(new Rect(10, 10, 100, 30), "Host")){
                NetworkManager.singleton.StartHost();
            }

            if(GUI.Button(new Rect(10, 50, 100, 30), "Join")){
                NetworkManager.singleton.StartClient();
            }
        }

        if(NetworkServer.active || NetworkClient.isConnected){
            if(GUI.Button(new Rect(10, 10, 100, 30), "Stop")){
                if(NetworkServer.active && NetworkClient.isConnected)
                    NetworkManager.singleton.StopHost();
                else if(NetworkClient.isConnected)
                    NetworkManager.singleton.StopClient();
                else if(NetworkServer.active)
                    NetworkManager.singleton.StopServer();
            }
            
        }
    }
}
