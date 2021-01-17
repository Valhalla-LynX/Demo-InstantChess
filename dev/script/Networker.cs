using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("")]
public class Networker : NetworkManager
{

    public GameObject bpre;
    public GameObject rpre;

    public override void OnClientConnect(NetworkConnection conn)
    {
        if (!clientLoadedScene)
        {
            ClientScene.RegisterPrefab(bpre);
            ClientScene.RegisterPrefab(rpre);
            if (!ClientScene.ready) ClientScene.Ready(conn);
            ClientScene.AddPlayer(conn);
        }
    }

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        switch (numPlayers)
        {
            case 0:
                // NetBoard.FreshBoard();
                NetworkServer.AddPlayerForConnection(conn, Instantiate(bpre));
                break;
            case 1:
                NetworkServer.AddPlayerForConnection(conn, Instantiate(rpre));
                break;
            default:
                Debug.LogError("need prefab");
                return;
        }
    }
}
