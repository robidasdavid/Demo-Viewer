using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkFrameManager : NetworkBehaviour
{
	[SyncVar]
	public string networkJsonData;
	[SyncVar]
	public int networkFrameIndex;

	public bool IsLocalOrServer {
		get => !gameObject.activeSelf || isServer;
	}
}
