using Photon.Pun;
using UnityEngine;

public class PhotonMan : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
