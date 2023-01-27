using System;
using System.Collections;
using UnityEngine;
using unityutilities;
using VelNet;

public class VelNetMan : MonoBehaviour
{
	public GameObject playerPrefab;
	private NetworkObject playerPrefabReference;
	
	public static Action<NetworkObject> OnPlayerPrefabInstantiated;

	// Start is called before the first frame update
	private void OnEnable()
	{
		VelNetManager.OnJoinedRoom += OnJoinedRoom;
		VelNetManager.OnLeftRoom += OnLeftRoom;
	}

	private void OnDisable()
	{
		VelNetManager.OnJoinedRoom -= OnJoinedRoom;
		VelNetManager.OnLeftRoom -= OnLeftRoom;
	}

	private void OnLeftRoom(string roomId)
	{
		Debug.Log("Left VelNet Room: " + roomId);
		if (playerPrefabReference != null) VelNetManager.NetworkDestroy(playerPrefabReference);
	}

	private void OnJoinedRoom(string roomId)
	{
		Debug.Log("Joined VelNet Room: " + roomId);
		playerPrefabReference = VelNetManager.NetworkInstantiate(playerPrefab.name);

		playerPrefabReference.GetComponent<CopyTransform>().SetTarget(Camera.main.transform, false);
		if (GameManager.instance.usingVR)
		{
			playerPrefabReference.transform.GetChild(1).GetComponent<CopyTransform>().SetTarget(GameManager.instance.vrRig.leftHand, false);
			playerPrefabReference.transform.GetChild(2).GetComponent<CopyTransform>().SetTarget(GameManager.instance.vrRig.rightHand, false);
		}

		// hide renderers locally. This is a dumb way of doing this
		playerPrefabReference.transform.GetChild(0).gameObject.SetActive(false);
		playerPrefabReference.transform.GetChild(1).GetChild(0).gameObject.SetActive(false);
		playerPrefabReference.transform.GetChild(2).GetChild(0).gameObject.SetActive(false);


		StartCoroutine(WaitOneFrame(() =>
		{
			try
			{
				OnPlayerPrefabInstantiated?.Invoke(playerPrefabReference);
			}
			catch (Exception e)
			{
				Debug.LogError(e);
			}
		}));
	}

	private IEnumerator WaitOneFrame(Action action)
	{
		yield return null;
		action();
	}

	private IEnumerator WaitForSeconds(float seconds, Action action)
	{
		yield return new WaitForSeconds(seconds);
		action();
	}
}