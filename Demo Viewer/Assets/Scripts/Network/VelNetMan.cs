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
	private void Start()
	{
		VelNetManager.OnConnectedToServer += () => { VelNetManager.Login("Replay Viewer", Hash128.Compute(SystemInfo.deviceUniqueIdentifier).ToString()); };
		VelNetManager.OnLoggedIn += () =>
		{
			// VelNetManager.Join("default");
		};

		VelNetManager.OnJoinedRoom += roomId =>
		{
			Debug.Log("Joined VelNet Room: " + roomId);
			playerPrefabReference = VelNetManager.NetworkInstantiate(playerPrefab.name);
			
			playerPrefabReference.GetComponent<CopyTransform>().SetTarget(Camera.main.transform, false);
			playerPrefabReference.transform.GetChild(0).gameObject.SetActive(false);


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