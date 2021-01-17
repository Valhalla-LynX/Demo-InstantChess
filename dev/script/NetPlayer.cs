using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetPlayer : NetworkBehaviour
{
    // 双放吃子数
    [HideInInspector]
    [SyncVar]
    public int eaten;

    private void Start()
    {
        Transform chessGroupTransform = GameObject.Find("chessGroup").transform;
        transform.parent = chessGroupTransform;
        foreach (Transform chessGroupTrans in chessGroupTransform)
        {
            Debug.Log(chessGroupTrans != transform);
            if (chessGroupTrans != transform)
            {
                foreach(Transform chessTrans in chessGroupTrans)
                {
                    chessTrans.gameObject.GetComponent<Chess>().InitChess();
                }
            }
        }
    }

    [Command]
    public void CMDMove(string name, int toRow, int toCol)
    {
        RPCMove(name, toRow, toCol);
    }

    [ClientRpc]
    public void RPCMove(string name, int toRow, int toCol)
    {
        GameObject go = GameObject.Find(name);
        if (go!=null)
        {
            Chess chess = go.GetComponent<Chess>();
            if (chess != null)
            {
                chess.ReciveCMDMove(new Coord(toRow, toCol));
            }
        }
    }
}
