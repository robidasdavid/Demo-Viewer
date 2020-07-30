using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using unityutilities;

public class GameManager : MonoBehaviour
{
	public Transform[] vrOnlyThings;
	public Transform[] flatOnlyThings;
	[ReadOnly]
	public bool lastFrameUserPresent;

	private void Awake()
	{
		if (gameObject.activeSelf)
		{
			RefreshVRObjectsVisibility(GetPresence());
		}
	}



	// Update is called once per frame
	void Update()
	{
		bool isPresent = GetPresence();

		if (lastFrameUserPresent != isPresent)
		{
			RefreshVRObjectsVisibility(isPresent);
		}
		lastFrameUserPresent = isPresent;
	}

	private static bool GetPresence()
	{
		InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
		bool isPresent = false;
		headDevice.TryGetFeatureValue(CommonUsages.userPresence, out isPresent);
		return isPresent;
	}

	private void RefreshVRObjectsVisibility(bool present)
	{
		Debug.Log("RefreshVRObjectsVisibility");
		if (present)
		{
			foreach (var thing in vrOnlyThings)
			{
				thing.gameObject.SetActive(true);
			}
			foreach (var thing in flatOnlyThings)
			{
				thing.gameObject.SetActive(false);
			}
		}
		else
		{
			foreach (var thing in vrOnlyThings)
			{
				thing.gameObject.SetActive(false);
			}
			foreach (var thing in flatOnlyThings)
			{
				thing.gameObject.SetActive(true);
			}
		}
	}
}
