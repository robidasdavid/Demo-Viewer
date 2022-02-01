using System;
using System.Collections;
using UnityEngine;
using VelNet;

public class VelNetMan : MonoBehaviour
{
	public GameObject playerPrefab;
	private NetworkObject playerPrefabReference;
	
	public static Action<NetworkObject> OnPlayerPrefabInstantiated;

	// Start is called before the first frame update
	private void Start()
	{
		VelNetManager.OnConnectedToServer += () => { VelNetManager.Login(SystemInfo.deviceUniqueIdentifier, "nopass"); };
		VelNetManager.OnLoggedIn += () =>
		{
			// VelNetManager.Join("default");
		};

		VelNetManager.OnJoinedRoom += roomId =>
		{
			Debug.Log("Joined VelNet Room: " + roomId);
			playerPrefabReference = VelNetManager.InstantiateNetworkObject(playerPrefab.name);


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
		};
		VelNetManager.OnLeftRoom += roomId =>
		{
			Debug.Log("Left VelNet Room: " + roomId);
			if (playerPrefabReference != null) VelNetManager.NetworkDestroy(playerPrefabReference);
		};
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