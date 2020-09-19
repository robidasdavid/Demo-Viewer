using Mirror;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
	public GameObject renderers;

	private void Start()
	{
		if (isLocalPlayer)
		{
			renderers.SetActive(false);
		}
	}

	private void Update()
	{
		if (isLocalPlayer)
		{
			transform.position = GameManager.instance.camera.transform.position;
			transform.forward = GameManager.instance.camera.transform.forward;

		}
	}
}
