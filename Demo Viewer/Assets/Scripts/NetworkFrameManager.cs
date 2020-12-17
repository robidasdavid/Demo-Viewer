using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class NetworkFrameManager : MonoBehaviourPunCallbacks, IPunObservable 
{
    public int networkFrameIndex;   // TODO
    public string networkJsonData;  // TODO
    private bool isServer;  // TODO
    public bool IsLocalOrServer => !gameObject.activeSelf || isServer;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(networkJsonData);   
        }

        if (stream.IsReading)
        {
            networkJsonData = (string)stream.ReceiveNext();
        }
    }
}
