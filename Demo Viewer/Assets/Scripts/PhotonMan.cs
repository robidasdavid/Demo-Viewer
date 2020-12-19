using Photon.Pun;
using UnityEngine;
using unityutilities;

public class PhotonMan : MonoBehaviourPunCallbacks
{
	public GameObject avatarPrefab;

	// Start is called before the first frame update
	private void Start()
	{
		PhotonNetwork.ConnectUsingSettings();
	}


	public override void OnJoinedRoom()
	{
		GameObject obj = PhotonNetwork.Instantiate(avatarPrefab.name, Vector3.zero, Quaternion.identity);
		obj.GetComponent<CopyTransform>().SetTarget(GameManager.instance.camera.transform, false);
		obj.transform.GetChild(0).gameObject.SetActive(false);
	}
}